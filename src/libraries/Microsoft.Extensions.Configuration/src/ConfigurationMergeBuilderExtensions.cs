// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Provides extension methods for selecting how configuration collections and objects are merged
    /// across sources.
    /// </summary>
    public static class ConfigurationMergeBuilderExtensions
    {
        internal const string ArrayMergeBehaviorKey = "Microsoft.Extensions.Configuration:ArrayMergeBehavior";
        internal const string ObjectMergeBehaviorKey = "Microsoft.Extensions.Configuration:ObjectMergeBehavior";

        /// <summary>
        /// Sets how array (positional) nodes are merged across sources for configuration built from this builder.
        /// </summary>
        /// <param name="builder">The configuration builder.</param>
        /// <param name="behavior">The merge behavior to apply to array nodes.</param>
        /// <returns>The same <see cref="IConfigurationBuilder"/> so that multiple calls can be chained.</returns>
        public static IConfigurationBuilder SetArrayMergeBehavior(this IConfigurationBuilder builder, ConfigurationMergeBehavior behavior)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Properties[ArrayMergeBehaviorKey] = behavior;
            return builder;
        }

        /// <summary>
        /// Sets how object (named) nodes, including dictionaries, are merged across sources for configuration
        /// built from this builder.
        /// </summary>
        /// <param name="builder">The configuration builder.</param>
        /// <param name="behavior">The merge behavior to apply to object nodes.</param>
        /// <returns>The same <see cref="IConfigurationBuilder"/> so that multiple calls can be chained.</returns>
        public static IConfigurationBuilder SetObjectMergeBehavior(this IConfigurationBuilder builder, ConfigurationMergeBehavior behavior)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Properties[ObjectMergeBehaviorKey] = behavior;
            return builder;
        }

        internal static ConfigurationMergeBehavior GetArrayMergeBehavior(this IConfigurationBuilder builder)
            => GetBehavior(builder, ArrayMergeBehaviorKey, ConfigurationMergeBehavior.Append);

        internal static ConfigurationMergeBehavior GetObjectMergeBehavior(this IConfigurationBuilder builder)
            => GetBehavior(builder, ObjectMergeBehaviorKey, ConfigurationMergeBehavior.Append);

        private static ConfigurationMergeBehavior GetBehavior(IConfigurationBuilder builder, string key, ConfigurationMergeBehavior defaultValue)
            => builder.Properties.TryGetValue(key, out object? value) && value is ConfigurationMergeBehavior behavior
                ? behavior
                : defaultValue;
    }
}
