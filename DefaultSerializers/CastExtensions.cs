using System;
using System.Runtime.CompilerServices;

public static class CastExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt64 EnsureUInt64(this UInt64 x) => checked((UInt64)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int64 ToInt64(this UInt64 x) => checked((Int64)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt32 ToUInt32(this UInt64 x) => checked((UInt32)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int32 ToInt32(this UInt64 x) => checked((Int32)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt16 ToUInt16(this UInt64 x) => checked((UInt16)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int16 ToInt16(this UInt64 x) => checked((Int16)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Byte ToByte(this UInt64 x) => checked((Byte)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SByte ToSByte(this UInt64 x) => checked((SByte)x);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt64 ToUInt64(this Int64 x) => checked((UInt64)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int64 EnsureInt64(this Int64 x) => checked((Int64)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt32 ToUInt32(this Int64 x) => checked((UInt32)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int32 ToInt32(this Int64 x) => checked((Int32)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt16 ToUInt16(this Int64 x) => checked((UInt16)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int16 ToInt16(this Int64 x) => checked((Int16)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Byte ToByte(this Int64 x) => checked((Byte)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SByte ToSByte(this Int64 x) => checked((SByte)x);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt64 ToUInt64(this UInt32 x) => checked((UInt64)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int64 ToInt64(this UInt32 x) => checked((Int64)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt32 EnsureUInt32(this UInt32 x) => checked((UInt32)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int32 ToInt32(this UInt32 x) => checked((Int32)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt16 ToUInt16(this UInt32 x) => checked((UInt16)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int16 ToInt16(this UInt32 x) => checked((Int16)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Byte ToByte(this UInt32 x) => checked((Byte)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SByte ToSByte(this UInt32 x) => checked((SByte)x);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt64 ToUInt64(this Int32 x) => checked((UInt64)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int64 ToInt64(this Int32 x) => checked((Int64)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt32 ToUInt32(this Int32 x) => checked((UInt32)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int32 EnsureInt32(this Int32 x) => checked((Int32)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt16 ToUInt16(this Int32 x) => checked((UInt16)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int16 ToInt16(this Int32 x) => checked((Int16)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Byte ToByte(this Int32 x) => checked((Byte)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SByte ToSByte(this Int32 x) => checked((SByte)x);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt64 ToUInt64(this UInt16 x) => checked((UInt64)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int64 ToInt64(this UInt16 x) => checked((Int64)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt32 ToUInt32(this UInt16 x) => checked((UInt32)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int32 ToInt32(this UInt16 x) => checked((Int32)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt16 EnsureUInt16(this UInt16 x) => checked((UInt16)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int16 ToInt16(this UInt16 x) => checked((Int16)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Byte ToByte(this UInt16 x) => checked((Byte)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SByte ToSByte(this UInt16 x) => checked((SByte)x);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt64 ToUInt64(this Int16 x) => checked((UInt64)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int64 ToInt64(this Int16 x) => checked((Int64)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt32 ToUInt32(this Int16 x) => checked((UInt32)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int32 ToInt32(this Int16 x) => checked((Int32)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt16 ToUInt16(this Int16 x) => checked((UInt16)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int16 EnsureInt16(this Int16 x) => checked((Int16)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Byte ToByte(this Int16 x) => checked((Byte)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SByte ToSByte(this Int16 x) => checked((SByte)x);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt64 ToUInt64(this Byte x) => checked((UInt64)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int64 ToInt64(this Byte x) => checked((Int64)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt32 ToUInt32(this Byte x) => checked((UInt32)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int32 ToInt32(this Byte x) => checked((Int32)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt16 ToUInt16(this Byte x) => checked((UInt16)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int16 ToInt16(this Byte x) => checked((Int16)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Byte EnsureByte(this Byte x) => checked((Byte)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SByte ToSByte(this Byte x) => checked((SByte)x);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt64 ToUInt64(this SByte x) => checked((UInt64)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int64 ToInt64(this SByte x) => checked((Int64)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt32 ToUInt32(this SByte x) => checked((UInt32)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int32 ToInt32(this SByte x) => checked((Int32)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt16 ToUInt16(this SByte x) => checked((UInt16)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int16 ToInt16(this SByte x) => checked((Int16)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Byte ToByte(this SByte x) => checked((Byte)x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SByte EnsureSByte(this SByte x) => checked((SByte)x);
}

public static class ClampToExtensions
{
    public static SByte ClampToSByte(this byte i) => i > SByte.MaxValue ? SByte.MaxValue : (SByte)i;
    public static Byte ClampToByte(this sbyte i) => i < 0 ? (byte)0 : (Byte)i;

    public static Byte ClampToByte(this short i) => i > Byte.MaxValue ? Byte.MaxValue : i < Byte.MinValue ? Byte.MinValue : (Byte)i;
    public static SByte ClampToSByte(this short i) => i > SByte.MaxValue ? SByte.MaxValue : i < SByte.MinValue ? SByte.MinValue : (SByte)i;
    public static UInt16 ClampToUInt16(this short i) => i < UInt16.MinValue ? UInt16.MinValue : (UInt16)i;

    public static Byte ClampToByte(this ushort i) => i > Byte.MaxValue ? Byte.MaxValue : (Byte)i;
    public static SByte ClampToSByte(this ushort i) => i > SByte.MaxValue ? SByte.MaxValue : (SByte)i;
    public static Int16 ClampToInt16(this ushort i) => i > Int16.MaxValue ? Int16.MaxValue : (Int16)i;

    public static Byte ClampToByte(this int i) => i > Byte.MaxValue ? Byte.MaxValue : i < Byte.MinValue ? Byte.MinValue : (Byte)i;
    public static SByte ClampToSByte(this int i) => i > SByte.MaxValue ? SByte.MaxValue : i < SByte.MinValue ? SByte.MinValue : (SByte)i;
    public static Int16 ClampToInt16(this int i) => i > Int16.MaxValue ? Int16.MaxValue : i < Int16.MinValue ? Int16.MinValue : (Int16)i;
    public static UInt16 ClampToUInt16(this int i) => i > UInt16.MaxValue ? UInt16.MaxValue : i < UInt16.MinValue ? UInt16.MinValue : (UInt16)i;
    public static UInt32 ClampToUInt32(this int i) => i < UInt32.MinValue ? UInt32.MinValue : (UInt32)i;

    public static Byte ClampToByte(this uint i) => i > Byte.MaxValue ? Byte.MaxValue : (Byte)i;
    public static SByte ClampToSByte(this uint i) => i > SByte.MaxValue ? SByte.MaxValue : (SByte)i;
    public static Int16 ClampToInt16(this uint i) => i > Int16.MaxValue ? Int16.MaxValue : (Int16)i;
    public static UInt16 ClampToUInt16(this uint i) => i > UInt16.MaxValue ? UInt16.MaxValue : (UInt16)i;
    public static Int32 ClampToInt32(this uint i) => i > Int32.MaxValue ? Int32.MaxValue : (Int32)i;

    public static Byte ClampToByte(this long i) => i > Byte.MaxValue ? Byte.MaxValue : i < Byte.MinValue ? Byte.MinValue : (Byte)i;
    public static SByte ClampToSByte(this long i) => i > SByte.MaxValue ? SByte.MaxValue : i < SByte.MinValue ? SByte.MinValue : (SByte)i;
    public static Int16 ClampToInt16(this long i) => i > Int16.MaxValue ? Int16.MaxValue : i < Int16.MinValue ? Int16.MinValue : (Int16)i;
    public static UInt16 ClampToUInt16(this long i) => i > UInt16.MaxValue ? UInt16.MaxValue : i < UInt16.MinValue ? UInt16.MinValue : (UInt16)i;
    public static Int32 ClampToInt32(this long i) => i > Int32.MaxValue ? Int32.MaxValue : i < Int32.MinValue ? Int32.MinValue : (Int32)i;
    public static UInt32 ClampToUInt32(this long i) => i > UInt32.MaxValue ? UInt32.MaxValue : i < UInt32.MinValue ? UInt32.MinValue : (UInt32)i;
    public static UInt64 ClampToUInt64(this long i) => i < 0 ? (UInt64)0 : (UInt64)i;

    public static Byte ClampToByte(this ulong i) => i > Byte.MaxValue ? Byte.MaxValue : (Byte)i;
    public static SByte ClampToSByte(this ulong i) => i > (ulong)SByte.MaxValue ? SByte.MaxValue : (SByte)i;
    public static Int16 ClampToInt16(this ulong i) => i > (ulong)Int16.MaxValue ? Int16.MaxValue : (Int16)i;
    public static UInt16 ClampToUInt16(this ulong i) => i > UInt16.MaxValue ? UInt16.MaxValue : (UInt16)i;
    public static Int32 ClampToInt32(this ulong i) => i > (ulong)Int32.MaxValue ? Int32.MaxValue : (Int32)i;
    public static UInt32 ClampToUInt32(this ulong i) => i > UInt32.MaxValue ? UInt32.MaxValue : (UInt32)i;
    public static Int64 ClampToInt64(this ulong i) => i > (ulong)Int64.MaxValue ? Int64.MaxValue : (Int64)i;
}