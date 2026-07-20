// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Represents the root node for a configuration.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    [DebuggerTypeProxy(typeof(ConfigurationRootDebugView))]
    public class ConfigurationRoot : IConfigurationRoot, IDisposable
    {
        private readonly IList<IConfigurationProvider> _providers;
        private readonly List<IDisposable> _changeTokenRegistrations;
        private readonly ConfigurationMergeBehavior _arrayMergeBehavior;
        private readonly ConfigurationMergeBehavior _objectMergeBehavior;
        private readonly object _mergeViewLock = new object();
        private IList<IConfigurationProvider> _readProviders;
        private volatile bool _mergeViewDirty = true;
        private ConfigurationReloadToken _changeToken = new ConfigurationReloadToken();

        /// <summary>
        /// Initializes a Configuration root with a list of providers.
        /// </summary>
        /// <param name="providers">The <see cref="IConfigurationProvider"/>s for this configuration.</param>
        public ConfigurationRoot(IList<IConfigurationProvider> providers)
            : this(providers, ConfigurationMergeBehavior.Append, ConfigurationMergeBehavior.Append)
        {
        }

        internal ConfigurationRoot(
            IList<IConfigurationProvider> providers,
            ConfigurationMergeBehavior arrayMergeBehavior,
            ConfigurationMergeBehavior objectMergeBehavior)
        {
            ArgumentNullException.ThrowIfNull(providers);

            _providers = providers;
            _arrayMergeBehavior = arrayMergeBehavior;
            _objectMergeBehavior = objectMergeBehavior;
            _readProviders = providers;
            _changeTokenRegistrations = new List<IDisposable>(providers.Count);
            foreach (IConfigurationProvider p in providers)
            {
                p.Load();
                _changeTokenRegistrations.Add(ChangeToken.OnChange(p.GetReloadToken, RaiseChanged));
            }
        }

        // The providers used to answer reads. When a provider reports structure via
        // IConfigurationMergeMetadata, this is a single merged provider that applies the selected array and
        // object merge behaviors; otherwise it is the original provider list. The merged view is built lazily
        // and rebuilt whenever the underlying providers change, so reads always reflect the current provider state.
        internal IList<IConfigurationProvider> ReadProviders
        {
            get
            {
                if (_mergeViewDirty)
                {
                    lock (_mergeViewLock)
                    {
                        if (_mergeViewDirty)
                        {
                            IConfigurationProvider? merged = ConfigurationMergeEngine.TryBuildMergedProvider(
                                _providers, _arrayMergeBehavior, _objectMergeBehavior);

                            _readProviders = merged is null ? _providers : new IConfigurationProvider[] { merged };
                            _mergeViewDirty = false;
                        }
                    }
                }

                return _readProviders;
            }
        }

        /// <summary>
        /// The <see cref="IConfigurationProvider"/>s for this configuration.
        /// </summary>
        public IEnumerable<IConfigurationProvider> Providers => _providers;

        /// <summary>
        /// Gets or sets the value corresponding to a configuration key.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>The configuration value.</returns>
        public string? this[string key]
        {
            get => GetConfiguration(ReadProviders, key);
            set
            {
                SetConfiguration(_providers, key, value);
                _mergeViewDirty = true;
            }
        }

        /// <summary>
        /// Gets the immediate children subsections.
        /// </summary>
        /// <returns>The children.</returns>
        public IEnumerable<IConfigurationSection> GetChildren() => this.GetChildrenImplementation(null);

        /// <summary>
        /// Returns a <see cref="IChangeToken"/> that can be used to observe when this configuration is reloaded.
        /// </summary>
        /// <returns>The <see cref="IChangeToken"/>.</returns>
        public IChangeToken GetReloadToken() => _changeToken;

        /// <summary>
        /// Gets a configuration subsection with the specified key.
        /// </summary>
        /// <param name="key">The key of the configuration section.</param>
        /// <returns>The <see cref="IConfigurationSection"/>.</returns>
        /// <remarks>
        ///     This method will never return <c>null</c>. If no matching subsection is found with the specified key,
        ///     an empty <see cref="IConfigurationSection"/> is returned.
        /// </remarks>
        public IConfigurationSection GetSection(string key)
            => new ConfigurationSection(this, key);

        /// <summary>
        /// Forces the configuration values to be reloaded from the underlying sources.
        /// </summary>
        public void Reload()
        {
            foreach (IConfigurationProvider provider in _providers)
            {
                provider.Load();
            }
            RaiseChanged();
        }

        private void RaiseChanged()
        {
            _mergeViewDirty = true;
            ConfigurationReloadToken previousToken = Interlocked.Exchange(ref _changeToken, new ConfigurationReloadToken());
            previousToken.OnReload();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // dispose change token registrations
            foreach (IDisposable registration in _changeTokenRegistrations)
            {
                registration.Dispose();
            }

            // dispose providers
            foreach (IConfigurationProvider provider in _providers)
            {
                (provider as IDisposable)?.Dispose();
            }
        }

        internal static string? GetConfiguration(IList<IConfigurationProvider> providers, string key)
        {
            for (int i = providers.Count - 1; i >= 0; i--)
            {
                IConfigurationProvider provider = providers[i];

                if (provider.TryGet(key, out string? value))
                {
                    return value;
                }
            }

            return null;
        }

        internal static void SetConfiguration(IList<IConfigurationProvider> providers, string key, string? value)
        {
            if (providers.Count == 0)
            {
                throw new InvalidOperationException(SR.Error_NoSources);
            }

            foreach (IConfigurationProvider provider in providers)
            {
                provider.Set(key, value);
            }
        }

        private string DebuggerToString()
        {
            return $"Sections = {ConfigurationSectionDebugView.FromConfiguration(this, this).Count}";
        }

        private sealed class ConfigurationRootDebugView
        {
            private readonly ConfigurationRoot _current;

            public ConfigurationRootDebugView(ConfigurationRoot current)
            {
                _current = current;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public ConfigurationSectionDebugView[] Items => ConfigurationSectionDebugView.FromConfiguration(_current, _current).ToArray();
        }
    }
}
