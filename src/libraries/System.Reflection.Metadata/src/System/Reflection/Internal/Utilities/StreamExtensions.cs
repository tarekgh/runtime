// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Reflection.Internal
{
    internal static partial class StreamExtensions
    {
        /// <summary>
        /// Attempts to read all of the requested bytes from the stream into the buffer
        /// </summary>
        /// <returns>
        /// The number of bytes read. Less than <paramref name="count" /> will
        /// only be returned if the end of stream is reached before all bytes can be read.
        /// </returns>
        /// <remarks>
        /// Unlike <see cref="Stream.Read(byte[], int, int)"/> it is not guaranteed that
        /// the stream position or the output buffer will be unchanged if an exception is
        /// returned.
        /// </remarks>
        internal static int TryReadAll(this Stream stream, byte[] buffer, int offset, int count)
        {
            // The implementations for many streams, e.g. FileStream, allows 0 bytes to be
            // read and returns 0, but the documentation for Stream.Read states that 0 is
            // only returned when the end of the stream has been reached. Rather than deal
            // with this contradiction, let's just never pass a count of 0 bytes
            Debug.Assert(count > 0);

            int totalBytesRead;
            int bytesRead;
            for (totalBytesRead = 0; totalBytesRead < count; totalBytesRead += bytesRead)
            {
                // Note: Don't attempt to save state in-between calls to .Read as it would
                // require a possibly massive intermediate buffer array
                bytesRead = stream.Read(buffer,
                                        offset + totalBytesRead,
                                        count - totalBytesRead);
                if (bytesRead == 0)
                {
                    break;
                }
            }
            return totalBytesRead;
        }

#if NET
        internal static int TryReadAll(this Stream stream, Span<byte> buffer)
#if NET
            => stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
#else
        {
            int totalBytesRead = 0;
            while (totalBytesRead < buffer.Length)
            {
                int bytesRead = stream.Read(buffer.Slice(totalBytesRead));
                if (bytesRead == 0)
                {
                    break;
                }

                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }
#endif
#endif

        /// <summary>
        /// Resolve image size as either the given user-specified size or distance from current position to end-of-stream.
        /// Also performs the relevant argument validation and publicly visible caller has same argument names.
        /// </summary>
        /// <exception cref="ArgumentException">size is 0 and distance from current position to end-of-stream can't fit in Int32.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Size is negative or extends past the end-of-stream from current position.</exception>
        internal static int GetAndValidateSize(Stream stream, int size, string streamParameterName)
        {
            long maxSize = stream.Length - stream.Position;

            if (size < 0 || size > maxSize)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            if (size != 0)
            {
                return size;
            }

            if (maxSize > int.MaxValue)
            {
                throw new ArgumentException(SR.StreamTooLarge, streamParameterName);
            }

            return (int)maxSize;
        }
    }
}
