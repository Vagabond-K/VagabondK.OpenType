using VagabondK.Indicators;
using VagabondK.Indicators.DigitalFonts;
using VagabondK.Indicators.GeometryUtil;
using VagabondK.OpenType;
using VagabondK.OpenType.Geometry;

namespace SevenSegmentSample
{
    /// <summary>
    /// 7-segment 디지털 숫자(0~9) 폰트 생성 샘플입니다.
    /// 7개 세그먼트(a~g)를 공유 contour로 정의해 CFF 서브루틴을 검증합니다.
    /// <see cref="Main"/>에서 전달하는 기울임각에 따라 face가 결정됩니다: 0 → Regular(수직), 0이 아닌 값 → Italic(기울임).
    /// 폰트 이름(subfamily)과 파일 이름은 기울임각에 따라 자동으로 바뀝니다.
    /// 모든 숫자는 동일한 advance width(고정폭)를 사용합니다.
    /// OTF와 TTF 파일 각 하나를 생성합니다.
    /// lsb는 고정값이 아니라 <see cref="Glyph"/> 생성자가 글리프별 실제 xMin으로 자동 계산합니다.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Indicators 라이브러리의 좌표계(좌상단 기원, Y 하향)를 OpenType 좌표계(좌하단 기원, Y 상향)로 변환하는 드로잉 컨텍스트입니다.
        /// Y축은 <paramref name="height"/> 기준으로 반전(FlipY)하고, X축은 <paramref name="shiftX"/>만큼 이동해
        /// 글리프를 advance width 중앙에 배치합니다.
        /// </summary>
        class GlyphContourDrawingContext(double height, double shiftX) : PartDrawingContext<GlyphContour>
        {
            protected override void OnBeginPath(in Point startPoint)
                => Renderer.MoveTo(ShiftX(startPoint.X), FlipY(startPoint.Y));
            protected override void OnDrawLine(in Point endPoint)
                => Renderer.LineTo(ShiftX(endPoint.X), FlipY(endPoint.Y));
            protected override void OnDrawQuadraticBezier(in Point controlPoint, in Point endPoint)
                => Renderer.QuadTo(ShiftX(controlPoint.X), FlipY(controlPoint.Y), ShiftX(endPoint.X), FlipY(endPoint.Y));
            protected override void OnDrawCubicBezier(in Point controlPoint1, in Point controlPoint2, in Point endPoint)
                => Renderer.CurveTo(ShiftX(controlPoint1.X), FlipY(controlPoint1.Y), ShiftX(controlPoint2.X), FlipY(controlPoint2.Y), ShiftX(endPoint.X), FlipY(endPoint.Y));
            protected override void OnClosePath() { }

            private double ShiftX(double x) => x + shiftX;
            private double FlipY(double y) => height - y;
        }

        private static void Main()
        {
            // 기울임각(도)을 바꿔서 다른 face를 생성합니다: 0 → Regular(수직), 0이 아닌 값 → Italic(기울임).
            double slantAngleDeg = 10.0;
            // 기울임각에 따라 subfamily 자동 결정: 0 → Regular, 0이 아닌 값 → Italic
            string subfamily = slantAngleDeg == 0 ? "Regular" : "Italic";
            // 7-segment 폰트 정의 (세로 800, 세그먼트 두께 40%, 틈새 12%)
            var sevenSegFont = new SevenSegmentFont
            {
                Size = 800,
                WidthRatio = 1,
                SlantAngle = slantAngleDeg,
                Thickness = 0.4,
                Gap = 0.12,
                CornerChamfer = 0.5,
                CornerGapWarping = 0.38,
                MiddleChamfer = 0.4,
            };

            // SevenSegmentFont의 실제 렌더링 치수 계산 (advance width 설정용)
            var actualHeight = sevenSegFont.Size;
            var thickness = Math.Min(sevenSegFont.Thickness, 1) * actualHeight / 3;
            var maxGapXY = (actualHeight - thickness * 3) / 4;
            var gapXY = Math.Min(sevenSegFont.Gap, 1) * maxGapXY;
            var width = Math.Max(sevenSegFont.WidthRatio * (thickness + maxGapXY) * 2, (thickness + gapXY) * 2);

            // 글자 간 간격(폭의 30%)을 포함한 고정폭 advance width
            var spacing = width * 0.3;
            var advanceWidth = (int)Math.Ceiling(width + spacing);

            // 세그먼트(a~g)별 contour 렌더링. 모든 숫자가 같은 contour 인스턴스를 공유하므로
            // CFF에서는 7개 세그먼트가 Private Subrs로 등록되어 파일 크기가 줄어듭니다.
            var drawingContext = new GlyphContourDrawingContext(actualHeight, spacing / 2);
            var segmentDictionary = sevenSegFont.Segments.Select(segment =>
            {
                var contour = new GlyphContour();
                drawingContext.Renderer = contour;
                drawingContext.DrawPart(segment);
                return new
                {
                    segment,
                    contour
                };
            }).ToDictionary(item => item.segment, item => item.contour);

            // 0~9 숫자와 특수 문자(공백, 대시, 언더스코어, 괄호) 글리프 생성
            var glyphs = Enumerable.Range(0, 10).Select(i => (char)('0' + i))
                .Union([' ', '-', '_', '[', ']', '(', ')'])
                .Select(c =>
                {
                    var outline = new GlyphOutline();

                    if (c == ' ')
                    {
                        // 공백: 빈 contour(단일 moveto)로 advance width만 확보
                        var contour = outline.BeginContour();
                        contour.MoveTo(0, 0);
                    }
                    else
                    {
                        foreach (var part in sevenSegFont.GetCharacterSegments(c, DigitalSegmentFilter.ActiveOnly))
                            if (segmentDictionary.TryGetValue(part, out var contour))
                                outline.AddContour(contour);
                    }
                    return new
                    {
                        c,
                        Glyph = new Glyph(outline, advanceWidth)
                    };
                }).ToArray();


            // OTF/TTF 공통 폰트 메타데이터를 설정한 빌더를 생성합니다.
            // UnitsPerEm 1000 기준 ascender 1125 / descender -250.
            // FontBuilder가 metrics/italic/fixedPitch/caretSlope를
            // hhea/OS/2/post/head(CFF Top DICT 포함)에 일관되게 cascade합니다.
            // 윤곽에 물리 shear가 적용되어도 italicAngle은 실제 기울임 각도(우측 기울임은 음수)로 설정합니다.
            // 주류 렌더러(GDI/DirectWrite/FreeType)는 italicAngle을 보고 윤곽에 합성 shear를 적용하지 않고,
            // 커서 기울임과 스타일 식별 메타데이터로만 사용하므로 이중 기울임이 발생하지 않습니다.
            // (Arial Italic도 윤곽을 물리적으로 기울이면서 italicAngle=-12를 설정하는 동일한 패턴)
            // 수직선 기준 반시계 방향 각도(도)이므로 우측 기울임은 음수 (Arial Italic -12와 동일).
            var builder = new FontBuilder()
                .UnitsPerEm(1000)
                .XAvgCharWidth(advanceWidth) // 고정폭: 모든 글리프가 동일한 advance width
                .BreakChar(0x20) // U+0020 SPACE
                .UseTypoMetrics()
                .Family("VagabondK Digital")
                .Subfamily(subfamily)
                .UniqueId($"VagabondK Digital {subfamily}: 1.0")
                .Version("Version 1.0")
                .Metrics(new FontMetrics
                {
                    Ascender = 1125,
                    Descender = -250,
                    LineGap = 0,
                })
                .FixedPitch(true)
                .ItalicAngle(-slantAngleDeg);
            builder.AddGlyphs(glyphs.Select(g => new KeyValuePair<int, Glyph>(g.c, g.Glyph)));

            // OTF 생성 (확장자 .otf → OtfFont)
            string otfPath = Path.Combine(AppContext.BaseDirectory, $"VagabondK_Digital-{subfamily}.otf");
            builder.Save(otfPath);
            Console.WriteLine($"OTF 파일 생성 완료: {otfPath}");

            // TTF 생성 (확장자 .ttf → TtfFont)
            string ttfPath = Path.Combine(AppContext.BaseDirectory, $"VagabondK_Digital-{subfamily}.ttf");
            builder.Save(ttfPath);
            Console.WriteLine($"TTF 파일 생성 완료: {ttfPath}");
        }
    }
}
