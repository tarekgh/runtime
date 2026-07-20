// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Implemented by a configuration provider that can describe the structure of the data it contributes,
    /// so that the configuration root can merge collections and objects across sources.
    /// </summary>
    /// <remarks>
    /// Providers that do not implement this interface contribute flat keys with no structure; their indexed
    /// keys are treated as overrides rather than appendable collection elements.
    /// </remarks>
    public interface IConfigurationMergeMetadata
    {
        /// <summary>
        /// Attempts to get structural information for the node at the specified path.
        /// </summary>
        /// <param name="path">The path of the node, using this provider's own keys.</param>
        /// <param name="info">When this method returns, contains the node information if the provider declares a structured node at <paramref name="path"/>.</param>
        /// <returns><see langword="true" /> if the provider declares a structured node at <paramref name="path"/>; otherwise, <see langword="false" />.</returns>
        bool TryGetNodeInfo(string path, out ConfigurationNodeInfo info);
    }
}
