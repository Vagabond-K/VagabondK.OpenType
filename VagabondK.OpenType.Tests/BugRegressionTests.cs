using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VagabondK.OpenType;
using VagabondK.OpenType.Geometry;

namespace VagabondK.OpenType.Tests;

/// <summary>
/// 수정된 버그들의 회귀 테스트입니다.
/// 각 테스트는 버그가 재발하면 실패하도록 설계되었습니다.
/// </summary>
public class BugRegressionTests
{
    // ── #1 loca long 형식 자동 승격 ──────────────────────────────

    [Fact]
    public void LargeTtf_AutoPromotesToLongLoca()
    {
        // glyf가 Offset16 한계(131070바이트)를 넘는 큰 TTF를 만든다.
        var builder = FontFactory.NewBuilder();
        var rnd = new Random(1);
        for (int i = 0; i < 300; i++)
        {
            var outline = new GlyphOutline();
            var contour = outline.BeginContour();
            contour.MoveTo(0, 0);
            for (int j = 0; j < 300; j++)
                contour.LineTo(rnd.Next(-2000, 2000), rnd.Next(-2000, 2000));
            builder.AddGlyph(0x2000 + i, new Glyph(outline, 500));
        }

        byte[] font = FontFactory.ToBytes<TtfFont>(builder);

        // head.indexToLocFormat(offset 50 기준이 아니라 head 테이블 내부)이 1이어야 한다.
        var dir = FontFactory.ReadTableDirectory(font);
        uint headOff = dir["head"].offset;
        // head: version(4) fontRevision(4) checkSumAdjustment(4) magicNumber(4) flags(2) unitsPerEm(2)
        //       created(8) modified(8) xMin..yMax(8) macStyle(2) lowestRecPPEM(2) fontDirectionHint(2) indexToLocFormat(2)
        ushort indexToLocFormat = FontFactory.U16(font, (int)headOff + 50);
        Assert.Equal((ushort)1, indexToLocFormat);

        // loca 오프셋이 단조 증가해야 한다 (잘림 없으면 자동 성립).
        uint locaOff = dir["loca"].offset;
        int numGlyphs = 301;
        int prev = -1;
        for (int i = 0; i < numGlyphs; i++)
        {
            int o = (int)((uint)font[locaOff + i * 4] << 24 | (uint)font[locaOff + i * 4 + 1] << 16
                        | (uint)font[locaOff + i * 4 + 2] << 8 | font[locaOff + i * 4 + 3]);
            Assert.True(o >= prev, $"loca offset decreased at glyph {i}: {o} < {prev}");
            prev = o;
        }

        // 자체 리더로 다시 읽을 수 있어야 한다.
        var reloaded = FontFactory.Load(font);
        Assert.Equal(300, reloaded.Count());
    }

    [Fact]
    public void SmallTtf_KeepsShortLoca()
    {
        var builder = FontFactory.NewBuilder();
        builder.AddGlyph('1', FontFactory.RectGlyph(50, 0, 200, 700, 300));

        byte[] font = FontFactory.ToBytes<TtfFont>(builder);
        var dir = FontFactory.ReadTableDirectory(font);
        ushort indexToLocFormat = FontFactory.U16(font, (int)dir["head"].offset + 50);
        Assert.Equal((ushort)0, indexToLocFormat);
    }

    // ── #3 hhea minLeftSideBearing / minRightSideBearing ────────

    [Fact]
    public void Hhea_MinBearings_AllPositiveNotZero()
    {
        // 모든 글리프의 LSB/RSB가 양수인 폰트.
        var builder = FontFactory.NewBuilder();
        for (char c = '1'; c <= '3'; c++)
            builder.AddGlyph(c, FontFactory.RectGlyph(50, 0, 200, 700, 500));

        byte[] font = FontFactory.ToBytes<OtfFont>(builder);
        var dir = FontFactory.ReadTableDirectory(font);
        uint h = dir["hhea"].offset;
        // hhea: version(4) ascender(2) descender(2) lineGap(2) advanceWidthMax(2)
        //       minLeftSideBearing(2) minRightSideBearing(2) xMaxExtent(2)
        short minLsb = FontFactory.I16(font, (int)h + 12);
        short minRsb = FontFactory.I16(font, (int)h + 14);

        // .notdef는 (100,0)-(400,700), adv 500 → lsb 100, rsb 100.
        // 추가 글리프는 lsb 50, rsb 250. 전체 최소: lsb 50, rsb 100.
        Assert.Equal((short)50, minLsb);
        Assert.Equal((short)100, minRsb);
    }

    [Fact]
    public void Hhea_MinBearings_NegativeWhenPresent()
    {
        // 음수 LSB가 있으면 그 값이 최소가 된다.
        var builder = FontFactory.NewBuilder();
        builder.AddGlyph('1', FontFactory.RectGlyph(-30, 0, 200, 700, 300));

        byte[] font = FontFactory.ToBytes<OtfFont>(builder);
        var dir = FontFactory.ReadTableDirectory(font);
        short minLsb = FontFactory.I16(font, (int)dir["hhea"].offset + 12);
        Assert.Equal((short)-30, minLsb);
    }

    // ── #4 CFF number 인코딩 (32767 초과 정수) ──────────────────

    [Fact]
    public void Cff_LargeIntegerAdvance_RoundTrips()
    {
        // advance width 40000은 hmtx uint16에는 들지만 CFF 16.16 fixed(±32768)는 넘는다.
        // op 29(longint)로 기록되어야 값이 보존된다.
        var builder = FontFactory.NewBuilder();
        var outline = new GlyphOutline();
        outline.BeginContour().MoveTo(0, 0).LineTo(100, 0).LineTo(100, 700).LineTo(0, 700);
        builder.AddGlyph('z', new Glyph(outline, 40000));

        byte[] font = FontFactory.ToBytes<OtfFont>(builder);
        var reloaded = FontFactory.Load(font);
        Assert.Equal(40000, reloaded['z'].AdvanceWidth);
    }

    [Fact]
    public void Cff_NumberEncoding_UsesLongintAbove32767()
    {
        // charstring에 op 29(0x1D) + 4바이트가 등장해야 한다.
        var builder = FontFactory.NewBuilder();
        var outline = new GlyphOutline();
        outline.BeginContour().MoveTo(0, 0).LineTo(100, 0).LineTo(100, 700).LineTo(0, 700);
        builder.AddGlyph('z', new Glyph(outline, 40000));

        byte[] font = FontFactory.ToBytes<OtfFont>(builder);
        // 40000 = 0x00009C40 → "29 00 00 9C 40" 패턴이 CFF에 존재해야 한다.
        Assert.True(IndexOfSequence(font, new byte[] { 0x1D, 0x00, 0x00, 0x9C, 0x40 }) >= 0,
            "CFF charstring must encode 40000 with op 29 (longint).");
    }

    // ── #5 int16/uint16 범위 검증 (조용한 잘림 금지) ────────────

    [Fact]
    public void OutOfRangeCoordinate_Throws()
    {
        var builder = FontFactory.NewBuilder();
        var outline = new GlyphOutline();
        outline.BeginContour().MoveTo(0, 0).LineTo(70000, 0).LineTo(70000, 40000).LineTo(0, 40000);
        builder.AddGlyph('X', new Glyph(outline, 70000));

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Save<TtfFont>(new MemoryStream()));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Save<OtfFont>(new MemoryStream()));
    }

    [Fact]
    public void NegativeAdvanceWidth_Throws()
    {
        var builder = FontFactory.NewBuilder();
        var outline = new GlyphOutline();
        outline.AddRectangle(0, 0, 100, 100);
        builder.AddGlyph('n', new Glyph(outline, -50));

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Save<OtfFont>(new MemoryStream()));
    }

    [Fact]
    public void AdvanceWidthAbove65535_Throws()
    {
        var builder = FontFactory.NewBuilder();
        var outline = new GlyphOutline();
        outline.AddRectangle(0, 0, 100, 100);
        builder.AddGlyph('w', new Glyph(outline, 70000));

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Save<OtfFont>(new MemoryStream()));
    }

    // ── #6 FontReader hmtx 누락 ─────────────────────────────────

    [Fact]
    public void FontReader_MissingHmtx_Throws()
    {
        // 디렉터리의 hmtx 태그명을 바꿔 리더가 hmtx를 찾지 못하게 한다.
        // (레이아웃은 그대로 두어 다른 테이블 파싱에는 영향 없음)
        var builder = FontFactory.NewBuilder();
        builder.AddGlyph('1', FontFactory.RectGlyph(50, 0, 200, 700, 300));
        byte[] font = FontFactory.ToBytes<OtfFont>(builder);

        int numTables = (font[4] << 8) | font[5];
        bool found = false;
        for (int i = 0; i < numTables; i++)
        {
            int e = 12 + i * 16;
            if (System.Text.Encoding.ASCII.GetString(font, e, 4) == "hmtx")
            {
                font[e] = (byte)'X';
                found = true;
                break;
            }
        }
        Assert.True(found, "test precondition: hmtx table entry must exist");

        Assert.Throws<InvalidOperationException>(() => FontFactory.Load(font));
    }

    // ── 공통 불변식 ─────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GeneratedFont_ChecksumIsValid(bool otf)
    {
        var builder = FontFactory.NewBuilder();
        builder.AddGlyph('1', FontFactory.RectGlyph(50, 0, 200, 700, 300));
        builder.AddGlyph('2', FontFactory.RectGlyph(50, 0, 200, 700, 300));

        byte[] font = otf ? FontFactory.ToBytes<OtfFont>(builder) : FontFactory.ToBytes<TtfFont>(builder);
        Assert.Equal(0xB1B0AFBAu, FontFactory.ChecksumSum(font));
    }

    private static int IndexOfSequence(byte[] data, byte[] seq)
    {
        for (int i = 0; i <= data.Length - seq.Length; i++)
        {
            bool ok = true;
            for (int j = 0; j < seq.Length; j++)
                if (data[i + j] != seq[j]) { ok = false; break; }
            if (ok) return i;
        }
        return -1;
    }
}
