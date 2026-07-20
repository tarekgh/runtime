// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration.Test;
using Xunit;

namespace Microsoft.Extensions.Configuration.Json.Test
{
    public class ConfigurationMergeBehaviorTest
    {
        private static JsonConfigurationSource CreateSource(string json)
            => new JsonConfigurationSource { FileProvider = TestStreamHelpers.StringToFileProvider(json), Optional = true };

        private static IConfigurationRoot BuildJson(params string[] jsonDocuments)
        {
            var builder = new ConfigurationBuilder();
            foreach (string json in jsonDocuments)
            {
                builder.Add(CreateSource(json));
            }
            return builder.Build();
        }

        [Fact]
        public void Array_AppendsByDefault_AcrossSources()
        {
            IConfigurationRoot config = BuildJson(
                @"{ ""servers"": [ ""A"", ""B"" ] }",
                @"{ ""servers"": [ ""C"" ] }");

            string[] values = config.GetSection("servers").GetChildren().Select(c => c.Value).ToArray();

            Assert.Equal(new[] { "A", "B", "C" }, values);
            Assert.Equal("A", config["servers:0"]);
            Assert.Equal("B", config["servers:1"]);
            Assert.Equal("C", config["servers:2"]);
        }

        [Fact]
        public void Array_AppendsAcrossThreeSources()
        {
            IConfigurationRoot config = BuildJson(
                @"{ ""servers"": [ ""A"", ""B"" ] }",
                @"{ ""servers"": [ ""C"" ] }",
                @"{ ""servers"": [ ""D"", ""E"" ] }");

            string[] values = config.GetSection("servers").GetChildren().Select(c => c.Value).ToArray();

            Assert.Equal(new[] { "A", "B", "C", "D", "E" }, values);
        }

        [Fact]
        public void Array_Replace_KeepsLastSourceOnly()
        {
            var builder = new ConfigurationBuilder();
            builder.SetArrayMergeBehavior(ConfigurationMergeBehavior.Replace);
            builder.Add(CreateSource(@"{ ""servers"": [ ""A"", ""B"" ] }"));
            builder.Add(CreateSource(@"{ ""servers"": [ ""C"" ] }"));
            IConfigurationRoot config = builder.Build();

            string[] values = config.GetSection("servers").GetChildren().Select(c => c.Value).ToArray();

            Assert.Equal(new[] { "C" }, values);
            Assert.Null(config["servers:1"]);
        }

        [Fact]
        public void Array_Replace_UsesHighestPrecedenceEvenWhenShorter()
        {
            var builder = new ConfigurationBuilder();
            builder.SetArrayMergeBehavior(ConfigurationMergeBehavior.Replace);
            builder.Add(CreateSource(@"{ ""servers"": [ ""A"", ""B"", ""C"" ] }"));
            builder.Add(CreateSource(@"{ ""servers"": [ ""X"" ] }"));
            IConfigurationRoot config = builder.Build();

            string[] values = config.GetSection("servers").GetChildren().Select(c => c.Value).ToArray();

            Assert.Equal(new[] { "X" }, values);
        }

        [Fact]
        public void ArrayOfObjects_AppendsByDefault()
        {
            IConfigurationRoot config = BuildJson(
                @"{ ""endpoints"": [ { ""host"": ""a"" } ] }",
                @"{ ""endpoints"": [ { ""host"": ""b"" }, { ""host"": ""c"" } ] }");

            Assert.Equal("a", config["endpoints:0:host"]);
            Assert.Equal("b", config["endpoints:1:host"]);
            Assert.Equal("c", config["endpoints:2:host"]);
            Assert.Equal(3, config.GetSection("endpoints").GetChildren().Count());
        }

        [Fact]
        public void NestedArrays_AppendOuterOnly()
        {
            IConfigurationRoot config = BuildJson(
                @"{ ""matrix"": [ [ ""a"", ""b"" ] ] }",
                @"{ ""matrix"": [ [ ""c"" ] ] }");

            Assert.Equal("a", config["matrix:0:0"]);
            Assert.Equal("b", config["matrix:0:1"]);
            Assert.Equal("c", config["matrix:1:0"]);
            Assert.Equal(2, config.GetSection("matrix").GetChildren().Count());
        }

        [Fact]
        public void Object_UnionsPropertiesByDefault()
        {
            IConfigurationRoot config = BuildJson(
                @"{ ""options"": { ""a"": ""1"", ""b"": ""2"" } }",
                @"{ ""options"": { ""b"": ""20"", ""c"": ""3"" } }");

            Assert.Equal("1", config["options:a"]);
            Assert.Equal("20", config["options:b"]);
            Assert.Equal("3", config["options:c"]);
            Assert.Equal(3, config.GetSection("options").GetChildren().Count());
        }

        [Fact]
        public void Object_Replace_KeepsLastSourceOnly()
        {
            var builder = new ConfigurationBuilder();
            builder.SetObjectMergeBehavior(ConfigurationMergeBehavior.Replace);
            builder.Add(CreateSource(@"{ ""options"": { ""a"": ""1"", ""b"": ""2"" } }"));
            builder.Add(CreateSource(@"{ ""options"": { ""c"": ""3"" } }"));
            IConfigurationRoot config = builder.Build();

            Assert.Null(config["options:a"]);
            Assert.Null(config["options:b"]);
            Assert.Equal("3", config["options:c"]);
            Assert.Equal(1, config.GetSection("options").GetChildren().Count());
        }

        [Fact]
        public void NumericObjectKeys_AreNotTreatedAsArray()
        {
            IConfigurationRoot config = BuildJson(
                @"{ ""map"": { ""0"": ""a"", ""1"": ""b"" } }",
                @"{ ""map"": { ""1"": ""B"", ""2"": ""c"" } }");

            Assert.Equal("a", config["map:0"]);
            Assert.Equal("B", config["map:1"]);
            Assert.Equal("c", config["map:2"]);
            Assert.Equal(3, config.GetSection("map").GetChildren().Count());
        }

        [Fact]
        public void ObjectContainingArray_MergesIndependently()
        {
            IConfigurationRoot config = BuildJson(
                @"{ ""parent"": { ""list"": [ ""a"" ], ""name"": ""x"" } }",
                @"{ ""parent"": { ""list"": [ ""b"" ], ""extra"": ""y"" } }");

            Assert.Equal("a", config["parent:list:0"]);
            Assert.Equal("b", config["parent:list:1"]);
            Assert.Equal("x", config["parent:name"]);
            Assert.Equal("y", config["parent:extra"]);
        }

        [Fact]
        public void SingleJsonSource_HasIdenticalLayout()
        {
            IConfigurationRoot config = BuildJson(@"{ ""servers"": [ ""A"", ""B"", ""C"" ] }");

            Assert.Equal("A", config["servers:0"]);
            Assert.Equal("B", config["servers:1"]);
            Assert.Equal("C", config["servers:2"]);
            Assert.Equal(3, config.GetSection("servers").GetChildren().Count());
        }

        [Fact]
        public void NonMetadataProvider_OverridesByExactKey()
        {
            var builder = new ConfigurationBuilder();
            builder.Add(CreateSource(@"{ ""servers"": [ ""A"", ""B"" ] }"));
            builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["servers:0"] = "OVERRIDE"
            });
            IConfigurationRoot config = builder.Build();

            Assert.Equal("OVERRIDE", config["servers:0"]);
            Assert.Equal("B", config["servers:1"]);
        }

        [Fact]
        public void MetadataOntoNonMetadata_FallsBackToOverride()
        {
            // A non-metadata provider does not report structure, so its indexed keys cannot be safely offset.
            // A later metadata provider therefore overrides by exact key, preserving the historical behavior.
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["servers:0"] = "A",
                ["servers:1"] = "B"
            });
            builder.Add(CreateSource(@"{ ""servers"": [ ""C"" ] }"));
            IConfigurationRoot config = builder.Build();

            Assert.Equal("C", config["servers:0"]);
            Assert.Equal("B", config["servers:1"]);
        }

        [Fact]
        public void Reload_RebuildsMergedView()
        {
            var source1 = CreateSource(@"{ ""servers"": [ ""A"" ] }");
            var source2 = CreateSource(@"{ ""servers"": [ ""B"" ] }");

            var builder = new ConfigurationBuilder();
            builder.Add(source1);
            builder.Add(source2);
            IConfigurationRoot config = builder.Build();

            Assert.Equal(new[] { "A", "B" }, config.GetSection("servers").GetChildren().Select(c => c.Value).ToArray());

            config.Reload();

            Assert.Equal(new[] { "A", "B" }, config.GetSection("servers").GetChildren().Select(c => c.Value).ToArray());
        }

        private static ConfigurationManager BuildManager(params string[] jsonDocuments)
        {
            var manager = new ConfigurationManager();
            foreach (string json in jsonDocuments)
            {
                ((IConfigurationBuilder)manager).Add(CreateSource(json));
            }
            return manager;
        }

        [Fact]
        public void ConfigurationManager_Array_AppendsByDefault()
        {
            using ConfigurationManager config = BuildManager(
                @"{ ""servers"": [ ""A"", ""B"" ] }",
                @"{ ""servers"": [ ""C"" ] }");

            Assert.Equal(new[] { "A", "B", "C" }, config.GetSection("servers").GetChildren().Select(c => c.Value).ToArray());
            Assert.Equal("A", config["servers:0"]);
            Assert.Equal("C", config["servers:2"]);
            Assert.Equal("C", config.GetSection("servers:2").Value);
        }

        [Fact]
        public void ConfigurationManager_Array_Replace()
        {
            var config = new ConfigurationManager();
            config.SetArrayMergeBehavior(ConfigurationMergeBehavior.Replace);
            ((IConfigurationBuilder)config).Add(CreateSource(@"{ ""servers"": [ ""A"", ""B"" ] }"));
            ((IConfigurationBuilder)config).Add(CreateSource(@"{ ""servers"": [ ""C"" ] }"));

            using (config)
            {
                Assert.Equal(new[] { "C" }, config.GetSection("servers").GetChildren().Select(c => c.Value).ToArray());
                Assert.Null(config["servers:1"]);
            }
        }

        [Fact]
        public void ConfigurationManager_Object_Replace()
        {
            var config = new ConfigurationManager();
            config.SetObjectMergeBehavior(ConfigurationMergeBehavior.Replace);
            ((IConfigurationBuilder)config).Add(CreateSource(@"{ ""options"": { ""a"": ""1"", ""b"": ""2"" } }"));
            ((IConfigurationBuilder)config).Add(CreateSource(@"{ ""options"": { ""c"": ""3"" } }"));

            using (config)
            {
                Assert.Null(config["options:a"]);
                Assert.Null(config["options:b"]);
                Assert.Equal("3", config["options:c"]);
                Assert.Equal(1, config.GetSection("options").GetChildren().Count());
            }
        }

        [Fact]
        public void ConfigurationManager_AddingSourceLater_RebuildsMergedView()
        {
            using ConfigurationManager config = BuildManager(@"{ ""servers"": [ ""A"", ""B"" ] }");

            Assert.Equal(new[] { "A", "B" }, config.GetSection("servers").GetChildren().Select(c => c.Value).ToArray());

            ((IConfigurationBuilder)config).Add(CreateSource(@"{ ""servers"": [ ""C"" ] }"));

            Assert.Equal(new[] { "A", "B", "C" }, config.GetSection("servers").GetChildren().Select(c => c.Value).ToArray());
        }

        [Fact]
        public void ConfigurationManager_RemovingSource_RebuildsMergedView()
        {
            using ConfigurationManager config = BuildManager(
                @"{ ""servers"": [ ""A"", ""B"" ] }",
                @"{ ""servers"": [ ""C"" ] }");

            Assert.Equal(new[] { "A", "B", "C" }, config.GetSection("servers").GetChildren().Select(c => c.Value).ToArray());

            // Remove the appended source (the default MemoryConfigurationSource is at index 0).
            config.Sources.RemoveAt(config.Sources.Count - 1);

            Assert.Equal(new[] { "A", "B" }, config.GetSection("servers").GetChildren().Select(c => c.Value).ToArray());
        }

        [Fact]
        public void ConfigurationManager_Reload_KeepsMergedView()
        {
            using ConfigurationManager config = BuildManager(
                @"{ ""servers"": [ ""A"" ] }",
                @"{ ""servers"": [ ""B"" ] }");

            Assert.Equal(new[] { "A", "B" }, config.GetSection("servers").GetChildren().Select(c => c.Value).ToArray());

            ((IConfigurationRoot)config).Reload();

            Assert.Equal(new[] { "A", "B" }, config.GetSection("servers").GetChildren().Select(c => c.Value).ToArray());
        }

        [Fact]
        public void ConfigurationManager_MatchesConfigurationRoot()
        {
            string json1 = @"{ ""parent"": { ""list"": [ ""a"" ], ""name"": ""x"" } }";
            string json2 = @"{ ""parent"": { ""list"": [ ""b"" ], ""extra"": ""y"" } }";

            IConfigurationRoot root = BuildJson(json1, json2);
            using ConfigurationManager manager = BuildManager(json1, json2);

            foreach (string key in new[] { "parent:list:0", "parent:list:1", "parent:name", "parent:extra" })
            {
                Assert.Equal(root[key], manager[key]);
            }
        }
    }
}
