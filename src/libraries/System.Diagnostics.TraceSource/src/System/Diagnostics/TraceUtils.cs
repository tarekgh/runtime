// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;

namespace System.Diagnostics
{
    internal static class TraceUtils
    {
        internal static void VerifyAttributes(StringDictionary? attributes, string[]? supportedAttributes, object parent)
        {
            ArgumentNullException.ThrowIfNull(attributes);

            foreach (string key in attributes.Keys)
            {
                if (supportedAttributes is null || !supportedAttributes.Contains(key))
                {
                    throw new ArgumentException(SR.Format(SR.AttributeNotSupported, key, parent.GetType().FullName));
                }
            }
        }
    }
}
