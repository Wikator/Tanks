using System;
using UnityEngine;

namespace FishNet.Serializing.Helping
{
    /// <summary>
    ///     Credit to https://github.com/viliwonka
    ///     https://github.com/FirstGearGames/FishNet/pull/23
    /// </summary>
    public static class Quaternion64Compression
    {
        // 64 bit quaternion compression
        // [4 bits] largest component
        // [21 bits] higher res  
        // [21 bits] higher res
        // [20 bits] higher res
        // sum is 64 bits
        private const float Maximum = +1.0f / 1.414214f;
        private const int BitsPerAxis_H = 21; // higher res, 21 bits
        private const int BitsPerAxis_L = 20; // lower res, 20 bits
        private const int LargestComponentShift = BitsPerAxis_H * 2 + BitsPerAxis_L * 1;
        private const int AShift = BitsPerAxis_H + BitsPerAxis_L;
        private const int BShift = BitsPerAxis_L;
        private const int IntScale_H = (1 << (BitsPerAxis_H - 1)) - 1;
        private const int IntMask_H = (1 << BitsPerAxis_H) - 1;
        private const int IntScale_L = (1 << (BitsPerAxis_L - 1)) - 1;
        private const int IntMask_L = (1 << BitsPerAxis_L) - 1;

        public static ulong Compress(Quaternion quaternion)
        {
            var absX = Mathf.Abs(quaternion.x);
            var absY = Mathf.Abs(quaternion.y);
            var absZ = Mathf.Abs(quaternion.z);
            var absW = Mathf.Abs(quaternion.w);

            var largestComponent = ComponentType.X;
            var largestAbs = absX;
            var largest = quaternion.x;

            if (absY > largestAbs)
            {
                largestAbs = absY;
                largestComponent = ComponentType.Y;
                largest = quaternion.y;
            }

            if (absZ > largestAbs)
            {
                largestAbs = absZ;
                largestComponent = ComponentType.Z;
                largest = quaternion.z;
            }

            if (absW > largestAbs)
            {
                largestComponent = ComponentType.W;
                largest = quaternion.w;
            }

            float a = 0;
            float b = 0;
            float c = 0;

            switch (largestComponent)
            {
                case ComponentType.X:
                    a = quaternion.y;
                    b = quaternion.z;
                    c = quaternion.w;
                    break;
                case ComponentType.Y:
                    a = quaternion.x;
                    b = quaternion.z;
                    c = quaternion.w;
                    break;
                case ComponentType.Z:
                    a = quaternion.x;
                    b = quaternion.y;
                    c = quaternion.w;
                    break;
                case ComponentType.W:
                    a = quaternion.x;
                    b = quaternion.y;
                    c = quaternion.z;
                    break;
            }

            if (largest < 0)
            {
                a = -a;
                b = -b;
                c = -c;
            }

            var integerA = ScaleToUint_H(a);
            var integerB = ScaleToUint_H(b);
            var integerC = ScaleToUint_L(c);

            return ((ulong)largestComponent << LargestComponentShift) | (integerA << AShift) | (integerB << BShift) |
                   integerC;
        }

        private static ulong ScaleToUint_H(float v)
        {
            var normalized = v / Maximum;
            return (ulong)Mathf.RoundToInt(normalized * IntScale_H) & IntMask_H;
        }

        private static ulong ScaleToUint_L(float v)
        {
            var normalized = v / Maximum;
            return (ulong)Mathf.RoundToInt(normalized * IntScale_L) & IntMask_L;
        }

        private static float ScaleToFloat_H(ulong v)
        {
            var unscaled = v * Maximum / IntScale_H;

            if (unscaled > Maximum)
                unscaled -= Maximum * 2;
            return unscaled;
        }

        private static float ScaleToFloat_L(ulong v)
        {
            var unscaled = v * Maximum / IntScale_L;

            if (unscaled > Maximum)
                unscaled -= Maximum * 2;
            return unscaled;
        }

        public static Quaternion Decompress(ulong compressed)
        {
            var largestComponentType = (ComponentType)(compressed >> LargestComponentShift);
            var integerA = (compressed >> AShift) & IntMask_H;
            var integerB = (compressed >> BShift) & IntMask_H;
            var integerC = compressed & IntMask_L;

            var a = ScaleToFloat_H(integerA);
            var b = ScaleToFloat_H(integerB);
            var c = ScaleToFloat_L(integerC);

            Quaternion rotation;
            switch (largestComponentType)
            {
                case ComponentType.X:
                    // (?) y z w
                    rotation.y = a;
                    rotation.z = b;
                    rotation.w = c;
                    rotation.x = Mathf.Sqrt(1 - rotation.y * rotation.y
                                              - rotation.z * rotation.z
                                              - rotation.w * rotation.w);
                    break;
                case ComponentType.Y:
                    // x (?) z w
                    rotation.x = a;
                    rotation.z = b;
                    rotation.w = c;
                    rotation.y = Mathf.Sqrt(1 - rotation.x * rotation.x
                                              - rotation.z * rotation.z
                                              - rotation.w * rotation.w);
                    break;
                case ComponentType.Z:
                    // x y (?) w
                    rotation.x = a;
                    rotation.y = b;
                    rotation.w = c;
                    rotation.z = Mathf.Sqrt(1 - rotation.x * rotation.x
                                              - rotation.y * rotation.y
                                              - rotation.w * rotation.w);
                    break;
                case ComponentType.W:
                    // x y z (?)
                    rotation.x = a;
                    rotation.y = b;
                    rotation.z = c;
                    rotation.w = Mathf.Sqrt(1 - rotation.x * rotation.x
                                              - rotation.y * rotation.y
                                              - rotation.z * rotation.z);
                    break;
                default:
                    // Should never happen!
                    throw new ArgumentOutOfRangeException("Unknown rotation component type: " +
                                                          largestComponentType);
            }

            return rotation;
        }
    }
}