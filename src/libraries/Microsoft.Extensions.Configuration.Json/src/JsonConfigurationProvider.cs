// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Microsoft.Extensions.Configuration.Json
{
    /// <summary>
    /// Provides configuration key-value pairs that are obtained from a JSON file.
    /// </summary>
    public class JsonConfigurationProvider : FileConfigurationProvider, IConfigurationMergeMetadata
    {
        private Dictionary<string, ConfigurationNodeInfo>? _nodes;

        /// <summary>
        /// Initializes a new instance with the specified source.
        /// </summary>
        /// <param name="source">The source settings.</param>
        public JsonConfigurationProvider(JsonConfigurationSource source) : base(source) { }

        /// <summary>
        /// Loads the JSON data from a stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        public override void Load(Stream stream)
        {
            try
            {
                JsonConfigurationParseResult result = JsonConfigurationFileParser.Parse(stream);
                Data = result.Data;
                _nodes = result.Nodes;
            }
            catch (JsonException e)
            {
                throw new FormatException(SR.Error_JSONParseError, e);
            }
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
