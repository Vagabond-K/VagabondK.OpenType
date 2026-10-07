using System;
using System.Linq;
using VagabondK.OpenType;
using VagabondK.OpenType.Geometry;

namespace VagabondK.OpenType.Tests;

/// <summary>
/// Save → Load 왕복(round-trip) 테스트입니다.
/// </summary>
public class RoundTripTests
{
    private static FontBuilder BuildSample()
    {
        var builder = FontFactory.NewBuilder()
            .Metrics(new FontMetrics { Ascender = 750, Descender = -250, LineGap = 0 });

        // 다중 contour(직선)
        var a = new GlyphOutline();
        a.BeginContour().MoveTo(0, 0).LineTo(100, 0).LineTo(100, 700).LineTo(0, 700);
        a.BeginContour().MoveTo(20, 20).LineTo(20, 680).LineTo(80, 680).LineTo(80, 20);
        builder.AddGlyph('A', new Glyph(a, 400));

        // 타원(2차 베지어)
        var b = new GlyphOutline();
        b.AddEllipse(250, 350, 200, 300);
        builder.AddGlyph('B', new Glyph(b, 500));

        // 3차 베지어
        var c = new GlyphOutline();
        c.BeginContour().MoveTo(50, 0).LineTo(50, 700)
            .CurveTo(350, 700, 750, 550, 750, 350)
            .CurveTo(750, 150, 350, 0, 50, 0);
        builder.AddGlyph('C', new Glyph(c, 800));

        // 빈 윤곽(스페이스)
        builder.AddGlyph(' ', new Glyph(new GlyphOutline(), 250));

        // 공유 contour(서브루틴)
        var shared = new GlyphOutline();
        var sc = shared.AddRectangle(10, 10, 100, 100);
        for (char ch = 'x'; ch <= 'z'; ch++)
        {
            var ol = new GlyphOutline();
            ol.AddContour(sc);
            builder.AddGlyph(ch, new Glyph(ol, 200));
        }

        return builder;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SampleFont_RoundTrips(bool otf)
    {
        byte[] font = otf ? FontFactory.ToBytes<OtfFont>(BuildSample()) : FontFactory.ToBytes<TtfFont>(BuildSample());
        var reloaded = FontFactory.Load(font);

        Assert.Equal(7, reloaded.Count());
        Assert.Equal(400, reloaded['A'].AdvanceWidth);
        Assert.Equal(2, reloaded['A'].Outline.Contours.Count);
        Assert.Equal(500, reloaded['B'].AdvanceWidth);
        Assert.Single(reloaded['B'].Outline.Contours);
        Assert.Equal(800, reloaded['C'].AdvanceWidth);
        Assert.Equal(250, reloaded[' '].AdvanceWidth);
        Assert.Equal(200, reloaded['x'].AdvanceWidth);
        Assert.Equal(200, reloaded['z'].AdvanceWidth);

        // 재저장해도 오류가 없다.
        using var ms = new System.IO.MemoryStream();
        if (otf) ((OtfFont)reloaded).Save(ms); else ((TtfFont)reloaded).Save(ms);
        Assert.True(ms.Length > 0);
    }

    [Fact]
    public void SharedContours_RemainSharedAfterOtfRoundTrip()
    {
        byte[] font = FontFactory.ToBytes<OtfFont>(BuildSample());
        var reloaded = FontFactory.Load(font);

        // CFF 서브루틴으로 저장된 공유 contour는 로드 후 같은 인스턴스로 복원된다.
        Assert.True(ReferenceEquals(reloaded['x'].Outline.Contours[0], reloaded['y'].Outline.Contours[0]));
    }

    [Fact]
    public void NonBmpCharacters_RoundTrip()
    {
        var builder = FontFactory.NewBuilder();
        builder.AddGlyph(0x1F600, FontFactory.RectGlyph(50, 0, 200, 700, 300));
        builder.AddGlyph(0x1F601, FontFactory.RectGlyph(50, 0, 200, 700, 300));

        byte[] font = FontFactory.ToBytes<OtfFont>(builder);
        var reloaded = FontFactory.Load(font);

        Assert.True(reloaded.ContainsGlyph(0x1F600));
        Assert.True(reloaded.ContainsGlyph(0x1F601));
        Assert.Equal(2, reloaded.Count());
    }

    [Fact]
    public void ItalicCascade_AppliesToAllTables()
    {
        var builder = FontFactory.NewBuilder().Subfamily("Italic").ItalicAngle(-12).FixedPitch(true);
        builder.AddGlyph('1', FontFactory.RectGlyph(50, 0, 200, 700, 300));

        var font = builder.Build<OtfFont>();
        // isFixedPitch가 켜지면 모든 글리프 advance가 균일해야 하므로 .notdef도 맞춘다.
        font.Notdef.AdvanceWidth = 300;

        using var ms = new System.IO.MemoryStream();
        font.Save(ms);
        var reloaded = FontFactory.Load(ms.ToArray());

        Assert.Equal(-12, reloaded.Post.ItalicAngle, 3);
        Assert.True(reloaded.Post.IsFixedPitch);
        Assert.NotEqual(0, reloaded.Hhea.CaretSlopeRun);
        var issues = reloaded.Validate().Where(r => r.Level == ValidationLevel.Error).ToList();
        Assert.Empty(issues);
    }

    [Fact]
    public void GlyphIndexer_UpsertAndRemove()
    {
        var font = FontFactory.NewBuilder().Build<TtfFont>();
        font['a'] = FontFactory.RectGlyph(0, 0, 100, 100, 100);
        font['b'] = FontFactory.RectGlyph(0, 0, 100, 100, 100);
        Assert.Equal(2, font.Count());

        font['a'] = FontFactory.RectGlyph(0, 0, 100, 100, 150);
        Assert.Equal(2, font.Count());
        Assert.Equal(150, font['a'].AdvanceWidth);

        Assert.True(font.RemoveGlyph('a'));
        Assert.False(font.ContainsGlyph('a'));
        Assert.Single(font);
        Assert.Equal(100, font['b'].AdvanceWidth);

        // 제거 후에도 저장 가능(glyph ID 재조정 확인).
        using var ms = new System.IO.MemoryStream();
        font.Save(ms);
        var reloaded = (TtfFont)FontFactory.Load(ms.ToArray());
        Assert.False(reloaded.ContainsGlyph('a'));
        Assert.True(reloaded.ContainsGlyph('b'));
    }
}
