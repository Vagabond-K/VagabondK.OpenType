using System;
using System.IO;
using VagabondK.OpenType;
using VagabondK.OpenType.Geometry;

namespace SimpleFontSample
{
    /// <summary>
    /// TTF와 OTF 파일을 한 번에 생성하는 샘플입니다.
    /// 숫자 '1'~'9'에 다양한 도형을 그려 폰트 생성 라이브러리를 검증합니다.
    /// Windows 폰트 뷰어가 숫자 1부터 표시하므로 '1'부터 시작합니다.
    /// </summary>
    internal static class Program
    {
        private static void Main()
        {
            // OTF/TTF 공통 폰트 메타데이터를 설정한 빌더를 생성합니다.
            // CFF/glyf, hmtx, cmap, maxp 테이블은 ToBytes()에서 글리프 정보로 자동 생성됩니다.
            var builder = new FontBuilder()
                .UnitsPerEm(1000)
                .Copyright("Copyright (c) 2026 VagabondK")
                .Family("VagabondK")
                .Subfamily("Regular")
                .UniqueId("VagabondK: 1.0")
                .Version("Version 1.0")
                .Manufacturer("VagabondK")
                .Designer("VagabondK")
                .License("Free for use")
                // 한국어(ko-KR) name 항목 추가. 변경하지 않은 필드는 기본 언어 값을 상속합니다.
                .AddLanguage(0x0412, ko =>
                {
                    ko.Family = "방랑자K";
                    ko.UniqueId = "방랑자K: 1.0";
                })
                .Metrics(new FontMetrics
                {
                    Ascender = 750,
                    Descender = -250,
                    LineGap = 0,
                });

            // '1': 세로 막대(narrow bar), advance 300
            // 가장 좁은 폭. 기본 직선, int 좌표.
            var one = new GlyphOutline();
            var oneContour = one.AddRectangle(50, 0, 200, 700);
            builder.AddGlyph('1', new Glyph(one, 300));

            // '2': '1'의 막대 contour를 공유(서브루틴) + 정사각형, advance 450
            // 공유 contour → CFF에서는 Private Subrs로 등록되어 파일 크기가 줄어듭니다.
            var two = new GlyphOutline();
            two.AddContour(oneContour);
            two.BeginContour()
                .MoveTo(250, 0)
                .LineTo(400, 0)
                .LineTo(400, 150)
                .LineTo(250, 150);
            builder.AddGlyph('2', new Glyph(two, 450));

            // '3': 마름모(rhombus), advance 650
            // 대각선 렌더링 검증.
            var three = new GlyphOutline();
            three.BeginContour()
                .MoveTo(325, 0)
                .LineTo(600, 350)
                .LineTo(325, 700)
                .LineTo(50, 350);
            builder.AddGlyph('3', new Glyph(three, 650));

            // '4': D모양(직선 2변 + cubic 2변), advance 800
            // 3차 베지어 기본, TTF cubic→quad 변환 검증.
            var four = new GlyphOutline();
            four.BeginContour()
                .MoveTo(50, 0)
                .LineTo(50, 700)
                .CurveTo(350, 700, 750, 550, 750, 350)
                .CurveTo(750, 150, 350, 0, 50, 0);
            builder.AddGlyph('4', new Glyph(four, 800));

            // '5': 오각형(pentagon), advance 900
            // 중심 (450, 350), 반지름 350의 정오각형. 직선 5변.
            var five = new GlyphOutline();
            five.BeginContour()
                .MoveTo(450, 700)
                .LineTo(117, 458)
                .LineTo(244, 67)
                .LineTo(656, 67)
                .LineTo(783, 458);
            builder.AddGlyph('5', new Glyph(five, 900));

            // '6': 도넛(외곽 타원 + 안쪽 구멍), advance 1000
            // 여러 contour + 구멍 + non-zero winding 검증.
            // 외곽 타원: 중심 (500, 350), rx=450, ry=300
            var six = new GlyphOutline();
            six.AddEllipse(500, 350, 450, 300);
            // 안쪽 구멍: 중심 (500, 350), rx=200, ry=130 — winding 방향을 반대로(반시계)해 구멍 생성
            six.AddEllipse(500, 350, 200, 130, 4, false);
            builder.AddGlyph('6', new Glyph(six, 1000));

            // '7': 내림선(descender) 포함 7자형, advance 500
            // baseline(y=0) 아래로 y=-200까지 내려가 descender 영역을 검증합니다.
            var seven = new GlyphOutline();
            seven.BeginContour()
                .MoveTo(50, 700)
                .LineTo(450, 700)
                .LineTo(450, 570)
                .LineTo(200, -200)
                .LineTo(50, -200)
                .LineTo(250, 570)
                .LineTo(50, 570);
            builder.AddGlyph('7', new Glyph(seven, 500));

            // '8': 대형 사각형(wide), advance 1250
            // xMax=1200 > 1131 → CFF 3바이트 number(op 28) 검증.
            builder.AddGlyph('8', Glyph.Rectangle(50, 0, 1150, 700, 1250));

            // '9': L자형(concave), advance 700
            // 오목한 모양(winding) 검증.
            var nine = new GlyphOutline();
            nine.BeginContour()
                .MoveTo(50, 0)
                .LineTo(650, 0)
                .LineTo(650, 150)
                .LineTo(200, 150)
                .LineTo(200, 700)
                .LineTo(50, 700);
            builder.AddGlyph('9', new Glyph(nine, 700));

            // OTF 생성 (확장자 .otf → OtfFont)
            string otfPath = Path.Combine(AppContext.BaseDirectory, "VagabondK_Simple.otf");
            builder.Save(otfPath);
            Console.WriteLine($"OTF 파일 생성 완료: {otfPath}");

            // TTF 생성 (확장자 .ttf → TtfFont)
            string ttfPath = Path.Combine(AppContext.BaseDirectory, "VagabondK_Simple.ttf");
            builder.Save(ttfPath);
            Console.WriteLine($"TTF 파일 생성 완료: {ttfPath}");
        }
    }
}
