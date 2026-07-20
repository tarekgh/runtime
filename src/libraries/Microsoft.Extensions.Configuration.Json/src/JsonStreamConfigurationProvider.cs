// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Extensions.Configuration.Json
{
    /// <summary>
    /// Provides configuration key-value pairs that are obtained from a JSON stream.
    /// </summary>
    public class JsonStreamConfigurationProvider : StreamConfigurationProvider, IConfigurationMergeMetadata
    {
        private Dictionary<string, ConfigurationNodeInfo>? _nodes;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStreamConfigurationProvider"/> class.
        /// </summary>
        /// <param name="source">The <see cref="JsonStreamConfigurationSource"/>.</param>
        public JsonStreamConfigurationProvider(JsonStreamConfigurationSource source) : base(source) { }

        /// <summary>
        /// Loads JSON configuration key-value pairs from a stream into a provider.
        /// </summary>
        /// <param name="stream">The JSON <see cref="Stream"/> to load configuration data from.</param>
        public override void Load(Stream stream)
        {
            JsonConfigurationParseResult result = JsonConfigurationFileParser.Parse(stream);
            Data = result.Data;
            _nodes = result.Nodes;
        }

        bool IConfigurationMergeMetadata.TryGetNodeInfo(string path, out ConfigurationNodeInfo info)
        {
            Dictionary<string, ConfigurationNodeInfo>? nodes = _nodes;
            if (nodes is not null && nodes.TryGetValue(path, out info))
            {
                return true;
            }

            info = default;
            return false;
        }
    }
}
