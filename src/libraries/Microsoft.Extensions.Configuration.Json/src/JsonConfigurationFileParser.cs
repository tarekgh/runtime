// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Microsoft.Extensions.Configuration.Json
{
    internal sealed class JsonConfigurationFileParser
    {
        private JsonConfigurationFileParser() { }

        private readonly Dictionary<string, string?> _data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ConfigurationNodeInfo> _nodes = new Dictionary<string, ConfigurationNodeInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly Stack<string> _paths = new Stack<string>();

        public static JsonConfigurationParseResult Parse(Stream input)
            => new JsonConfigurationFileParser().ParseStream(input);

        private JsonConfigurationParseResult ParseStream(Stream input)
        {
            var jsonDocumentOptions = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };

            using (var reader = new StreamReader(input))
            using (JsonDocument doc = JsonDocument.Parse(reader.ReadToEnd(), jsonDocumentOptions))
            {
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    throw new FormatException(SR.Format(SR.Error_InvalidTopLevelJSONElement, doc.RootElement.ValueKind));
                }
                VisitObjectElement(doc.RootElement);
            }

            return new JsonConfigurationParseResult(_data, _nodes);
        }

        private void VisitObjectElement(JsonElement element)
        {
            var isEmpty = true;
            int count = 0;

            foreach (JsonProperty property in element.EnumerateObject())
            {
                isEmpty = false;
                count++;
                EnterContext(property.Name);
                VisitValue(property.Value);
                ExitContext();
            }

            if (_paths.Count > 0)
            {
                _nodes[_paths.Peek()] = new ConfigurationNodeInfo(ConfigurationNodeKind.Named, count);
            }

            SetNullIfElementIsEmpty(isEmpty);
        }

        private void VisitArrayElement(JsonElement element)
        {
            int index = 0;

            foreach (JsonElement arrayElement in element.EnumerateArray())
            {
                EnterContext(index.ToString());
                VisitValue(arrayElement);
                ExitContext();
                index++;
            }

            if (_paths.Count > 0)
            {
                _nodes[_paths.Peek()] = new ConfigurationNodeInfo(ConfigurationNodeKind.Positional, index);
            }

            SetEmptyIfElementIsEmpty(isEmpty: index == 0);
        }

        private void SetNullIfElementIsEmpty(bool isEmpty)
        {
            if (isEmpty && _paths.Count > 0)
            {
                _data[_paths.Peek()] = null;
            }
        }

        private void SetEmptyIfElementIsEmpty(bool isEmpty)
        {
            if (isEmpty && _paths.Count > 0)
            {
                _data[_paths.Peek()] = string.Empty;
            }
        }

        private void VisitValue(JsonElement value)
        {
            Debug.Assert(_paths.Count > 0);

            switch (value.ValueKind)
            {
                case JsonValueKind.Object:
                    VisitObjectElement(value);
                    break;

                case JsonValueKind.Array:
                    VisitArrayElement(value);
                    break;

                case JsonValueKind.Number:
                case JsonValueKind.String:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    string key = _paths.Peek();
                    if (_data.ContainsKey(key))
                    {
                        throw new FormatException(SR.Format(SR.Error_KeyIsDuplicated, key));
                    }
                    _data[key] = value.ValueKind == JsonValueKind.Null ? null : value.ToString();
                    break;

                default:
                    throw new FormatException(SR.Format(SR.Error_UnsupportedJSONToken, value.ValueKind));
            }
        }

        private void EnterContext(string context) =>
            _paths.Push(_paths.Count > 0 ?
                _paths.Peek() + ConfigurationPath.KeyDelimiter + context :
                context);

        private void ExitContext() => _paths.Pop();
    }

    internal sealed class JsonConfigurationParseResult
    {
        public JsonConfigurationParseResult(
            IDictionary<string, string?> data,
            Dictionary<string, ConfigurationNodeInfo> nodes)
        {
            Data = data;
            Nodes = nodes;
        }

        public IDictionary<string, string?> Data { get; }

        public Dictionary<string, ConfigurationNodeInfo> Nodes { get; }
    }
}
