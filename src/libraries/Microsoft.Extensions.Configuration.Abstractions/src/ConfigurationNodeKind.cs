// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Describes the shape of a configuration node as reported by a configuration provider.
    /// </summary>
    public enum ConfigurationNodeKind
    {
        /// <summary>
        /// An order-significant, integer-keyed sequence (for example, a JSON array). Binds to array and
        /// other list-like collection types.
        /// </summary>
        Positional = 0,

        /// <summary>
        /// A string-keyed map (for example, a JSON object). Binds to complex objects and dictionary types.
        /// </summary>
        Named = 1,
    }
}
