// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Describes a structured configuration node reported by a provider that implements
    /// <see cref="IConfigurationMergeMetadata"/>.
    /// </summary>
    public readonly struct ConfigurationNodeInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationNodeInfo"/> struct.
        /// </summary>
        /// <param name="kind">The kind of the node.</param>
        /// <param name="elementCount">The number of elements the provider contributes for a <see cref="ConfigurationNodeKind.Positional"/> node.</param>
        public ConfigurationNodeInfo(ConfigurationNodeKind kind, int elementCount)
        {
            Kind = kind;
            ElementCount = elementCount;
        }

        /// <summary>
        /// Gets the kind of the node.
        /// </summary>
        public ConfigurationNodeKind Kind { get; }

        /// <summary>
        /// Gets the number of elements the provider contributes for the node. This value is only meaningful
        /// when <see cref="Kind"/> is <see cref="ConfigurationNodeKind.Positional"/>.
        /// </summary>
        public int ElementCount { get; }
    }
}
