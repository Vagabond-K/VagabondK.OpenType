using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VagabondK.OpenType;
using VagabondK.OpenType.Geometry;

namespace VagabondK.OpenType.Tests;

/// <summary>
/// 폰트 생성/저장/로드에 쓰는 공통 헬퍼입니다.
/// </summary>
internal static class FontFactory
{
    /// <summary>필수 name이 채워진 빌더를 만듭니다.</summary>
    public static FontBuilder NewBuilder(string family = "TestFont") =>
        new FontBuilder()
            .UnitsPerEm(1000)
            .Family(family)
            .Subfamily("Regular")
            .UniqueId($"{family}: 1.0")
            .Version("Version 1.0");

    /// <summary>사각형 윤곽 글리프를 만듭니다.</summary>
    public static Glyph RectGlyph(double x, double y, double w, double h, int advance) =>
        Glyph.Rectangle(x, y, w, h, advance);

    /// <summary>폰트를 메모리에 직렬화합니다.</summary>
    public static byte[] ToBytes<T>(FontBuilder builder) where T : FontBase, new()
    {
        using var ms = new MemoryStream();
        builder.Save<T>(ms);
        return ms.ToArray();
    }

    /// <summary>바이트에서 폰트를 로드합니다.</summary>
    public static FontBase Load(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        return FontBase.Load(ms);
    }

    /// <summary>sfnt 테이블 디렉터리를 파싱합니다. (tag → offset, length)</summary>
    public static Dictionary<string, (uint offset, uint length)> ReadTableDirectory(byte[] font)
    {
        int numTables = (font[4] << 8) | font[5];
        var result = new Dictionary<string, (uint, uint)>();
        for (int i = 0; i < numTables; i++)
        {
            int e = 12 + i * 16;
            string tag = System.Text.Encoding.ASCII.GetString(font, e, 4);
            uint offset = (uint)((font[e + 8] << 24) | (font[e + 9] << 16) | (font[e + 10] << 8) | font[e + 11]);
            uint length = (uint)((font[e + 12] << 24) | (font[e + 13] << 16) | (font[e + 14] << 8) | font[e + 15]);
            result[tag] = (offset, length);
        }
        return result;
    }

    /// <summary>uint16(빅 엔디안) 읽기.</summary>
    public static ushort U16(byte[] b, int i) => (ushort)((b[i] << 8) | b[i + 1]);

    /// <summary>int16(빅 엔디안) 읽기.</summary>
    public static short I16(byte[] b, int i) => (short)U16(b, i);

    /// <summary>
    /// OpenType checksum 규칙: 전체 폰트(헤더+디렉터리+테이블)의 uint32 합은
    /// 0xB1B0AFBA와 같아야 한다.
    /// </summary>
    public static uint ChecksumSum(byte[] font)
    {
        int numTables = (font[4] << 8) | font[5];
        uint sum = 0;
        for (int i = 0; i < numTables; i++)
        {
            int e = 12 + i * 16;
            uint offset = ((uint)font[e + 8] << 24) | ((uint)font[e + 9] << 16) | ((uint)font[e + 10] << 8) | font[e + 11];
            uint length = ((uint)font[e + 12] << 24) | ((uint)font[e + 13] << 16) | ((uint)font[e + 14] << 8) | font[e + 15];
            sum += SumRange(font, (int)offset, (int)length);
        }
        sum += SumRange(font, 0, 12 + numTables * 16);
        return sum;
    }

    private static uint SumRange(byte[] data, int offset, int length)
    {
        uint sum = 0;
        for (int i = 0; i < length; i += 4)
        {
            uint v = 0;
            for (int j = 0; j < 4; j++)
            {
                v <<= 8;
                int k = offset + i + j;
                if (k < data.Length && i + j < length)
                    v |= data[k];
            }
            sum += v;
        }
        return sum;
    }
}
