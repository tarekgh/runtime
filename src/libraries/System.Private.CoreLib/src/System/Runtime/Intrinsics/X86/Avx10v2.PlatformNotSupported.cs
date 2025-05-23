// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 Avx10.2 hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    public abstract class Avx10v2 : Avx10v1
    {
        internal Avx10v2() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get { return false; } }

        /// <summary>
        ///   <para>  VMINMAXPD xmm1{k1}{z}, xmm2, xmm3/m128/m64bcst, imm8</para>
        /// </summary>
        public static Vector128<double> MinMax(Vector128<double> left, Vector128<double> right, [ConstantExpected] byte control)  { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  VMINMAXPD ymm1{k1}{z}, ymm2, ymm3/m256/m64bcst {sae}, imm8</para>
        /// </summary>
        public static Vector256<double> MinMax(Vector256<double> left, Vector256<double> right, [ConstantExpected] byte control)  { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  VMINMAXPS xmm1{k1}{z}, xmm2, xmm3/m128/m32bcst, imm8</para>
        /// </summary>
        public static Vector128<float> MinMax(Vector128<float> left, Vector128<float> right, [ConstantExpected] byte control)  { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  VMINMAXPS ymm1{k1}{z}, ymm2, ymm3/m256/m32bcst {sae}, imm8</para>
        /// </summary>
        public static Vector256<float> MinMax(Vector256<float> left, Vector256<float> right, [ConstantExpected] byte control)  { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  VMINMAXSD xmm1{k1}{z}, xmm2, xmm3/m64 {sae}, imm8</para>
        /// </summary>
        public static Vector128<double> MinMaxScalar(Vector128<double> left, Vector128<double> right, [ConstantExpected] byte control)  { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  VMINMAXSS xmm1{k1}{z}, xmm2, xmm3/m32 {sae}, imm8</para>
        /// </summary>
        public static Vector128<float> MinMaxScalar(Vector128<float> left, Vector128<float> right, [ConstantExpected] byte control)  { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  VCVTPS2IBS xmm1{k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> ConvertToSByteWithSaturationAndZeroExtendToInt32(Vector128<float> value)  { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  VCVTPS2IBS ymm1{k1}{z}, ymm2/m256/m32bcst {er}</para>
        /// </summary>
        public static Vector256<int> ConvertToSByteWithSaturationAndZeroExtendToInt32(Vector256<float> value)  { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  VCVTPS2IUBS xmm1{k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> ConvertToByteWithSaturationAndZeroExtendToInt32(Vector128<float> value)  { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  VCVTPS2IUBS ymm1{k1}{z}, ymm2/m256/m32bcst {er}</para>
        /// </summary>
        public static Vector256<int> ConvertToByteWithSaturationAndZeroExtendToInt32(Vector256<float> value)  { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  VCVTTPS2IBS xmm1{k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> ConvertToSByteWithTruncatedSaturationAndZeroExtendToInt32(Vector128<float> value)  { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  VCVTTPS2IBS ymm1{k1}{z}, ymm2/m256/m32bcst {sae}</para>
        /// </summary>
        public static Vector256<int> ConvertToSByteWithTruncatedSaturationAndZeroExtendToInt32(Vector256<float> value)  { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  VCVTTPS2IUBS xmm1{k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> ConvertToByteWithTruncatedSaturationAndZeroExtendToInt32(Vector128<float> value)  { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  VCVTTPS2IUBS ymm1{k1}{z}, ymm2/m256/m32bcst {sae}</para>
        /// </summary>
        public static Vector256<int> ConvertToByteWithTruncatedSaturationAndZeroExtendToInt32(Vector256<float> value)  { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  VMOVD xmm1, xmm2/m32</para>
        /// </summary>
        public static Vector128<int> MoveScalar(Vector128<int> value)  { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  VMOVD xmm1, xmm2/m32</para>
        /// </summary>
        public static Vector128<uint> MoveScalar(Vector128<uint> value)  { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  VMOVW xmm1, xmm2/m16</para>
        /// </summary>
        public static Vector128<short> MoveScalar(Vector128<short> value)  { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  VMOVW xmm1, xmm2/m16</para>
        /// </summary>
        public static Vector128<ushort> MoveScalar(Vector128<ushort> value)  { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  VMOVW xmm1/m16, xmm2</para>
        /// </summary>
        public static unsafe void StoreScalar(short* address, Vector128<short> source)  { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  VMOVW xmm1/m16, xmm2</para>
        /// </summary>
        public static unsafe void StoreScalar(ushort* address, Vector128<ushort> source)  { throw new PlatformNotSupportedException(); }

        /// <summary>Provides access to the x86 AVX10.2 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public new abstract class X64 : Avx10v1.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }

        }

        /// <summary>Provides access to the x86 AVX10.2/512 hardware instructions via intrinsics.</summary>
        public new abstract class V512 : Avx10v1.V512
        {
            internal V512() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }

            /// <summary>
            ///   <para>  VMINMAXPD zmm1{k1}{z}, zmm2, zmm3/m512/m64bcst {sae}, imm8</para>
            /// </summary>
            public static Vector512<double> MinMax(Vector512<double> left, Vector512<double> right, [ConstantExpected] byte control)  { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>  VMINMAXPS zmm1{k1}{z}, zmm2, zmm3/m512/m32bcst {sae}, imm8</para>
            /// </summary>
            public static Vector512<float> MinMax(Vector512<float> left, Vector512<float> right, [ConstantExpected] byte control)  { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>  VCVTPS2IBS zmm1{k1}{z}, zmm2/m512/m32bcst {er}</para>
            /// </summary>
            public static Vector512<int> ConvertToSByteWithSaturationAndZeroExtendToInt32(Vector512<float> value)  { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>  VCVTPS2IBS zmm1{k1}{z}, zmm2/m512/m32bcst {er}</para>
            /// </summary>
            public static Vector512<int> ConvertToSByteWithSaturationAndZeroExtendToInt32(Vector512<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>  VCVTPS2IUBS zmm1{k1}{z}, zmm2/m512/m32bcst {er}</para>
            /// </summary>
            public static Vector512<int> ConvertToByteWithSaturationAndZeroExtendToInt32(Vector512<float> value)  { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>  VCVTPS2IUBS zmm1{k1}{z}, zmm2/m512/m32bcst {er}</para>
            /// </summary>
            public static Vector512<int> ConvertToByteWithSaturationAndZeroExtendToInt32(Vector512<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>  VCVTTPS2IUBS zmm1{k1}{z}, zmm2/m512/m32bcst {sae}</para>
            /// </summary>
            public static Vector512<int> ConvertToSByteWithTruncatedSaturationAndZeroExtendToInt32(Vector512<float> value)  { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>  VCVTTPS2IUBS zmm1{k1}{z}, zmm2/m512/m32bcst {sae}</para>
            /// </summary>
            public static Vector512<int> ConvertToByteWithTruncatedSaturationAndZeroExtendToInt32(Vector512<float> value)  { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>  VMPSADBW zmm1{k1}{z}, zmm2, zmm3/m512, imm8</para>
            /// </summary>
            public static Vector512<ushort> MultipleSumAbsoluteDifferences(Vector512<byte> left, Vector512<byte> right, [ConstantExpected] byte mask)  { throw new PlatformNotSupportedException(); }

            /// <summary>Provides access to the x86 AVX10.1/512 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
            [Intrinsic]
            public new abstract class X64 : Avx10v1.V512.X64
            {
                internal X64() { }

                /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
                /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
                /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
                public static new bool IsSupported { [Intrinsic] get { return false; } }
            }
        }
    }
}
