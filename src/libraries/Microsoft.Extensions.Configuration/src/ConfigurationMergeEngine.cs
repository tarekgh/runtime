// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Builds a single flattened, cross-source merged view of a set of configuration providers, applying the
    /// selected array and object merge behaviors. The view is only produced when at least one provider
    /// implements <see cref="IConfigurationMergeMetadata"/>; otherwise the configuration root keeps its
    /// original per-provider read path and behavior.
    /// </summary>
    internal static class ConfigurationMergeEngine
    {
        private const char KeyDelimiter = ':';

        internal static IConfigurationProvider? TryBuildMergedProvider(
            IList<IConfigurationProvider> providers,
            ConfigurationMergeBehavior arrayBehavior,
            ConfigurationMergeBehavior objectBehavior)
        {
            bool anyMetadata = false;
            foreach (IConfigurationProvider provider in providers)
            {
                if (provider is IConfigurationMergeMetadata)
                {
                    anyMetadata = true;
                    break;
                }
            }

            if (!anyMetadata)
            {
                return null;
            }

            var merged = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var arrayCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (IConfigurationProvider provider in providers)
            {
                MergeProvider(provider, merged, arrayCounts, arrayBehavior, objectBehavior);
            }

            return new MergedConfigurationProvider(merged);
        }

        private static void MergeProvider(
            IConfigurationProvider provider,
            Dictionary<string, string?> merged,
            Dictionary<string, int> arrayCounts,
            ConfigurationMergeBehavior arrayBehavior,
            ConfigurationMergeBehavior objectBehavior)
        {
            var entries = new List<KeyValuePair<string, string?>>();
            EnumerateProvider(provider, entries);

            if (provider is not IConfigurationMergeMetadata metadata)
            {
                // A provider that does not report structure contributes flat keys that override by exact key,
                // preserving the historical behavior for command-line, environment variable, and custom providers.
                foreach (KeyValuePair<string, string?> entry in entries)
                {
                    merged[entry.Key] = entry.Value;
                }

                return;
            }

            Dictionary<string, ConfigurationNodeInfo> declared = DiscoverDeclaredNodes(metadata, entries);

            // Offsets are captured before this provider is applied so its own elements do not offset each other.
            var offsets = new Dictionary<string, int>(arrayCounts, StringComparer.OrdinalIgnoreCase);

            ApplyReplaceRoots(declared, merged, arrayCounts, offsets, arrayBehavior, objectBehavior);

            foreach (KeyValuePair<string, string?> entry in entries)
            {
                string mergedKey = RewritePath(entry.Key, declared, offsets);
                merged[mergedKey] = entry.Value;
            }

            foreach (KeyValuePair<string, ConfigurationNodeInfo> node in declared)
            {
                if (node.Value.Kind != ConfigurationNodeKind.Positional)
                {
                    continue;
                }

                string mergedPath = RewritePath(node.Key, declared, offsets);
                arrayCounts.TryGetValue(mergedPath, out int existing);
                arrayCounts[mergedPath] = existing + node.Value.ElementCount;
            }
        }

        private static void ApplyReplaceRoots(
            Dictionary<string, ConfigurationNodeInfo> declared,
            Dictionary<string, string?> merged,
            Dictionary<string, int> arrayCounts,
            Dictionary<string, int> offsets,
            ConfigurationMergeBehavior arrayBehavior,
            ConfigurationMergeBehavior objectBehavior)
        {
            if (arrayBehavior != ConfigurationMergeBehavior.Replace && objectBehavior != ConfigurationMergeBehavior.Replace)
            {
                return;
            }

            // Process shallower nodes first so a replaced parent covers its descendants.
            foreach (KeyValuePair<string, ConfigurationNodeInfo> node in declared.OrderBy(static n => n.Key.Length))
            {
                bool isReplace =
                    (node.Value.Kind == ConfigurationNodeKind.Positional && arrayBehavior == ConfigurationMergeBehavior.Replace) ||
                    (node.Value.Kind == ConfigurationNodeKind.Named && objectBehavior == ConfigurationMergeBehavior.Replace);

                if (!isReplace)
                {
                    continue;
                }

                string mergedPath = RewritePath(node.Key, declared, offsets);
                PurgeUnder(merged, mergedPath);
                RemoveUnder(arrayCounts, mergedPath);
                RemoveUnder(offsets, mergedPath);

                if (node.Value.Kind == ConfigurationNodeKind.Positional)
                {
                    offsets[mergedPath] = 0;
                }
            }
        }

        private static string RewritePath(
            string path,
            Dictionary<string, ConfigurationNodeInfo> declared,
            Dictionary<string, int> offsets)
        {
            string[] segments = path.Split(KeyDelimiter);
            string? originalParent = null;
            string? mergedParent = null;

            foreach (string segment in segments)
            {
                string mergedSegment = segment;

                if (originalParent is not null &&
                    declared.TryGetValue(originalParent, out ConfigurationNodeInfo info) &&
                    info.Kind == ConfigurationNodeKind.Positional &&
                    int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out int index))
                {
                    offsets.TryGetValue(mergedParent!, out int offset);
                    mergedSegment = (index + offset).ToString(CultureInfo.InvariantCulture);
                }

                mergedParent = mergedParent is null ? mergedSegment : mergedParent + KeyDelimiter + mergedSegment;
                originalParent = originalParent is null ? segment : originalParent + KeyDelimiter + segment;
            }

            return mergedParent!;
        }

        private static Dictionary<string, ConfigurationNodeInfo> DiscoverDeclaredNodes(
            IConfigurationMergeMetadata metadata,
            List<KeyValuePair<string, string?>> entries)
        {
            var declared = new Dictionary<string, ConfigurationNodeInfo>(StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, string?> entry in entries)
            {
                string key = entry.Key;
                int start = 0;

                while (true)
                {
                    int next = key.IndexOf(KeyDelimiter, start);
                    string prefix = next < 0 ? key : key.Substring(0, next);

                    if (seen.Add(prefix) && metadata.TryGetNodeInfo(prefix, out ConfigurationNodeInfo info))
                    {
                        declared[prefix] = info;
                    }

                    if (next < 0)
                    {
                        break;
                    }

                    start = next + 1;
                }
            }

            return declared;
        }

        private static void EnumerateProvider(IConfigurationProvider provider, List<KeyValuePair<string, string?>> entries)
        {
            var stack = new Stack<string?>();
            stack.Push(null);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (stack.Count > 0)
            {
                string? parentPath = stack.Pop();

                foreach (string childKey in provider.GetChildKeys(Enumerable.Empty<string>(), parentPath))
                {
                    string fullKey = parentPath is null ? childKey : parentPath + KeyDelimiter + childKey;

                    if (!visited.Add(fullKey))
                    {
                        continue;
                    }

                    if (provider.TryGet(fullKey, out string? value))
                    {
                        entries.Add(new KeyValuePair<string, string?>(fullKey, value));
                    }

                    stack.Push(fullKey);
                }
            }
        }

        private static void PurgeUnder(Dictionary<string, string?> merged, string mergedPath)
        {
            string prefix = mergedPath + KeyDelimiter;
            List<string>? toRemove = null;

            foreach (string key in merged.Keys)
            {
                if (key.Equals(mergedPath, StringComparison.OrdinalIgnoreCase) ||
                    key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    (toRemove ??= new List<string>()).Add(key);
                }
            }

            if (toRemove is not null)
            {
                foreach (string key in toRemove)
                {
                    merged.Remove(key);
                }
            }
        }

        private static void RemoveUnder(Dictionary<string, int> counts, string mergedPath)
        {
            string prefix = mergedPath + KeyDelimiter;
            List<string>? toRemove = null;

            foreach (string key in counts.Keys)
            {
                if (key.Equals(mergedPath, StringComparison.OrdinalIgnoreCase) ||
                    key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    (toRemove ??= new List<string>()).Add(key);
                }
            }

            if (toRemove is not null)
            {
                foreach (string key in toRemove)
                {
                    counts.Remove(key);
                }
            }
        }

        private sealed class MergedConfigurationProvider : ConfigurationProvider
        {
            public MergedConfigurationProvider(IDictionary<string, string?> data)
            {
                Data = data;
            }
        }
    }
}
