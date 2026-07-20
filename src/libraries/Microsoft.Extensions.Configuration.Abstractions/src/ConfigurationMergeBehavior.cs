// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Specifies how a configuration node contributed by one source is combined with the same node
    /// contributed by a lower-precedence source.
    /// </summary>
    public enum ConfigurationMergeBehavior
    {
        /// <summary>
        /// Accumulates the node across sources. For arrays, the elements of the higher-precedence source
        /// are added after the elements of the lower-precedence source. For objects, the members are unioned.
        /// </summary>
        Append = 0,

        /// <summary>
        /// Replaces the node. The highest-precedence source that contributes the node supplies its entire
        /// contents, and the contributions of lower-precedence sources for that node are discarded.
        /// </summary>
        Replace = 1,
    }
}
