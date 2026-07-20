// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Test;
using Xunit;

namespace Microsoft.Extensions
#if BUILDING_SOURCE_GENERATOR_TESTS
    .SourceGeneration
#endif
    .Configuration.Binder.Tests
{
    // These tests validate that the cross-source merge behavior applied at the configuration root is observed by the
    // binder. Because the merge happens before binding, the same tests exercise both the reflection binder and the
    // configuration binding source generator.
    public sealed partial class ConfigurationBinderMergeTests : ConfigurationBinderTestsBase
    {
        public class MergeEndpoint
        {
            public string Host { get; set; }
            public int Port { get; set; }
        }

        private static IConfiguration BuildMerged(
            ConfigurationMergeBehavior? arrayBehavior,
            ConfigurationMergeBehavior? objectBehavior,
            params string[] jsonDocuments)
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
            foreach (string json in jsonDocuments)
            {
                builder.AddJsonStream(TestStreamHelpers.StringToStream(json));
            }
            return builder.Build();
        }

        [Fact]
        public void BindArray_AppendsByDefault()
        {
            IConfiguration config = BuildMerged(null, null,
                @"{ ""servers"": [ ""A"", ""B"" ] }",
                @"{ ""servers"": [ ""C"" ] }");

            string[] servers = config.GetSection("servers").Get<string[]>();

            Assert.Equal(new[] { "A", "B", "C" }, servers);
        }

        [Fact]
        public void BindList_AppendsByDefault()
        {
            IConfiguration config = BuildMerged(null, null,
                @"{ ""servers"": [ ""A"", ""B"" ] }",
                @"{ ""servers"": [ ""C"" ] }");

            List<string> servers = config.GetSection("servers").Get<List<string>>();

            Assert.Equal(new[] { "A", "B", "C" }, servers);
        }

        [Fact]
        public void BindHashSet_UnionsByDefault()
        {
            IConfiguration config = BuildMerged(null, null,
                @"{ ""ports"": [ ""80"", ""443"" ] }",
                @"{ ""ports"": [ ""443"", ""8080"" ] }");

            HashSet<string> ports = config.GetSection("ports").Get<HashSet<string>>();

            Assert.Equal(3, ports.Count);
            Assert.Contains("80", ports);
            Assert.Contains("443", ports);
            Assert.Contains("8080", ports);
        }

        [Fact]
        public void BindArray_Replace_KeepsLastSourceOnly()
        {
            IConfiguration config = BuildMerged(ConfigurationMergeBehavior.Replace, null,
                @"{ ""servers"": [ ""A"", ""B"" ] }",
                @"{ ""servers"": [ ""C"" ] }");

            string[] servers = config.GetSection("servers").Get<string[]>();

            Assert.Equal(new[] { "C" }, servers);
        }

        [Fact]
        public void BindObject_UnionsPropertiesByDefault()
        {
            IConfiguration config = BuildMerged(null, null,
                @"{ ""endpoint"": { ""host"": ""a"", ""port"": ""80"" } }",
                @"{ ""endpoint"": { ""host"": ""b"" } }");

            MergeEndpoint endpoint = config.GetSection("endpoint").Get<MergeEndpoint>();

            Assert.Equal("b", endpoint.Host);
            Assert.Equal(80, endpoint.Port);
        }

        [Fact]
        public void BindObject_Replace_DropsLowerSourceProperties()
        {
            IConfiguration config = BuildMerged(null, ConfigurationMergeBehavior.Replace,
                @"{ ""endpoint"": { ""host"": ""a"", ""port"": ""80"" } }",
                @"{ ""endpoint"": { ""host"": ""b"" } }");

            MergeEndpoint endpoint = config.GetSection("endpoint").Get<MergeEndpoint>();

            Assert.Equal("b", endpoint.Host);
            Assert.Equal(0, endpoint.Port);
        }

        [Fact]
        public void BindDictionary_UnionsByDefault()
        {
            IConfiguration config = BuildMerged(null, null,
                @"{ ""limits"": { ""cpu"": ""1"", ""mem"": ""512"" } }",
                @"{ ""limits"": { ""cpu"": ""2"" } }");

            Dictionary<string, string> limits = config.GetSection("limits").Get<Dictionary<string, string>>();

            Assert.Equal("2", limits["cpu"]);
            Assert.Equal("512", limits["mem"]);
            Assert.Equal(2, limits.Count);
        }

        [Fact]
        public void BindDictionary_Replace_KeepsLastSourceOnly()
        {
            IConfiguration config = BuildMerged(null, ConfigurationMergeBehavior.Replace,
                @"{ ""limits"": { ""cpu"": ""1"", ""mem"": ""512"" } }",
                @"{ ""limits"": { ""cpu"": ""2"" } }");

            Dictionary<string, string> limits = config.GetSection("limits").Get<Dictionary<string, string>>();

            Assert.Equal("2", limits["cpu"]);
            Assert.Single(limits);
        }
    }
}
