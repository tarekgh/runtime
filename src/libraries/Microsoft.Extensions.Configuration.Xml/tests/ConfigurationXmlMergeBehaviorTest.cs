// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.Extensions.Configuration.Test;
using Microsoft.Extensions.Configuration.Xml;
using Xunit;

namespace Microsoft.Extensions.Configuration.Xml.Test
{
    public class ConfigurationXmlMergeBehaviorTest
    {
        private const string ServersAlphaBeta =
            "<settings><Servers><Server>alpha</Server><Server>beta</Server></Servers></settings>";
        private const string ServersGammaDelta =
            "<settings><Servers><Server>gamma</Server><Server>delta</Server></Servers></settings>";

        private static IConfigurationRoot BuildStreams(ConfigurationMergeBehavior? arrayBehavior, ConfigurationMergeBehavior? objectBehavior, params string[] documents)
        {
            var builder = new ConfigurationBuilder();
            if (arrayBehavior is ConfigurationMergeBehavior array)
            {
                builder.SetArrayMergeBehavior(array);
            }
            if (objectBehavior is ConfigurationMergeBehavior obj)
            {
                builder.SetObjectMergeBehavior(obj);
            }
            foreach (string document in documents)
            {
                builder.AddXmlStream(TestStreamHelpers.StringToStream(document));
            }
            return builder.Build();
        }

        [Fact]
        public void RepeatedElements_ReportPositionalNodeMetadata()
        {
            var source = new XmlStreamConfigurationSource { Stream = TestStreamHelpers.StringToStream(ServersAlphaBeta) };
            var provider = (XmlStreamConfigurationProvider)source.Build(new ConfigurationBuilder());
            provider.Load();

            var metadata = (IConfigurationMergeMetadata)provider;

            Assert.True(metadata.TryGetNodeInfo("Servers:Server", out ConfigurationNodeInfo arrayInfo));
            Assert.Equal(ConfigurationNodeKind.Positional, arrayInfo.Kind);
            Assert.Equal(2, arrayInfo.ElementCount);

            Assert.True(metadata.TryGetNodeInfo("Servers", out ConfigurationNodeInfo objectInfo));
            Assert.Equal(ConfigurationNodeKind.Named, objectInfo.Kind);

            Assert.False(metadata.TryGetNodeInfo("Servers:Server:0", out _));
        }

        [Fact]
        public void Array_AppendsAcrossSources_ByDefault()
        {
            IConfigurationRoot config = BuildStreams(arrayBehavior: null, objectBehavior: null, ServersAlphaBeta, ServersGammaDelta);

            Assert.Equal("alpha", config["Servers:Server:0"]);
            Assert.Equal("beta", config["Servers:Server:1"]);
            Assert.Equal("gamma", config["Servers:Server:2"]);
            Assert.Equal("delta", config["Servers:Server:3"]);
        }

        [Fact]
        public void Array_Replace_KeepsLastSourceOnly()
        {
            IConfigurationRoot config = BuildStreams(ConfigurationMergeBehavior.Replace, objectBehavior: null, ServersAlphaBeta, ServersGammaDelta);

            Assert.Equal("gamma", config["Servers:Server:0"]);
            Assert.Equal("delta", config["Servers:Server:1"]);
            Assert.Null(config["Servers:Server:2"]);
            Assert.Null(config["Servers:Server:3"]);
        }

        [Fact]
        public void Object_UnionsPropertiesByDefault()
        {
            const string endpointFull = "<settings><Endpoint><Host>a</Host><Port>80</Port></Endpoint></settings>";
            const string endpointHostOnly = "<settings><Endpoint><Host>b</Host></Endpoint></settings>";

            IConfigurationRoot config = BuildStreams(arrayBehavior: null, objectBehavior: null, endpointFull, endpointHostOnly);

            Assert.Equal("b", config["Endpoint:Host"]);
            Assert.Equal("80", config["Endpoint:Port"]);
        }

        [Fact]
        public void Object_Replace_DropsLowerSourceProperties()
        {
            const string endpointFull = "<settings><Endpoint><Host>a</Host><Port>80</Port></Endpoint></settings>";
            const string endpointHostOnly = "<settings><Endpoint><Host>b</Host></Endpoint></settings>";

            IConfigurationRoot config = BuildStreams(arrayBehavior: null, ConfigurationMergeBehavior.Replace, endpointFull, endpointHostOnly);

            Assert.Equal("b", config["Endpoint:Host"]);
            Assert.Null(config["Endpoint:Port"]);
        }

        [Fact]
        public void SingleSource_HasIdenticalLayout()
        {
            IConfigurationRoot config = BuildStreams(arrayBehavior: null, objectBehavior: null, ServersAlphaBeta);

            Assert.Equal("alpha", config["Servers:Server:0"]);
            Assert.Equal("beta", config["Servers:Server:1"]);
            Assert.Null(config["Servers:Server:2"]);
        }

        [Fact]
        public void ArrayAndObject_MergeIndependently()
        {
            const string first =
                "<settings><Endpoint><Host>a</Host></Endpoint><Servers><Server>alpha</Server><Server>beta</Server></Servers></settings>";
            const string second =
                "<settings><Endpoint><Port>443</Port></Endpoint><Servers><Server>gamma</Server><Server>delta</Server></Servers></settings>";

            IConfigurationRoot config = BuildStreams(arrayBehavior: null, objectBehavior: null, first, second);

            // Object axis unions
            Assert.Equal("a", config["Endpoint:Host"]);
            Assert.Equal("443", config["Endpoint:Port"]);

            // Array axis appends
            Assert.Equal("alpha", config["Servers:Server:0"]);
            Assert.Equal("beta", config["Servers:Server:1"]);
            Assert.Equal("gamma", config["Servers:Server:2"]);
            Assert.Equal("delta", config["Servers:Server:3"]);
        }

        [Fact]
        public void FileProvider_AppendsArraysAcrossSources()
        {
            string first = Path.GetTempFileName();
            string second = Path.GetTempFileName();
            try
            {
                File.WriteAllText(first, ServersAlphaBeta);
                File.WriteAllText(second, ServersGammaDelta);

                IConfigurationRoot config = new ConfigurationBuilder()
                    .AddXmlFile(first, optional: false)
                    .AddXmlFile(second, optional: false)
                    .Build();

                Assert.Equal("alpha", config["Servers:Server:0"]);
                Assert.Equal("beta", config["Servers:Server:1"]);
                Assert.Equal("gamma", config["Servers:Server:2"]);
                Assert.Equal("delta", config["Servers:Server:3"]);
            }
            finally
            {
                File.Delete(first);
                File.Delete(second);
            }
        }

        [Fact]
        public void MergedWithNonMetadataProvider_OverridesByExactKey()
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddXmlStream(TestStreamHelpers.StringToStream(ServersAlphaBeta))
                .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
                {
                    ["Servers:Server:0"] = "override"
                })
                .Build();

            Assert.Equal("override", config["Servers:Server:0"]);
            Assert.Equal("beta", config["Servers:Server:1"]);
        }
    }
}
