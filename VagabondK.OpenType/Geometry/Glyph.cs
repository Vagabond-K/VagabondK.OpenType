using System;

namespace VagabondK.OpenType.Geometry
{
    /// <summary>
    /// 폰트의 단일 글리프를 나타냅니다.
    /// 윤곽(outline)과 수평 메트릭(advance width, left side bearing)을 포함합니다.
    /// advance width와 left side bearing은 hmtx 테이블에 기록되며,
    /// right side bearing은 advance width - left side bearing - (xMax - xMin)으로 계산됩니다.
    /// </summary>
    public class Glyph
    {
        /// <summary>
        /// 글리프의 윤곽을 반환합니다.
        /// glyf(TTF) 또는 CFF charstring으로 직렬화됩니다.
        /// </summary>
        public GlyphOutline Outline { get; }

        /// <summary>
        /// advance width(폰트 단위)입니다.
        /// 이 글리프를 그린 후 다음 글리프의 pen 기원점을 얼마나 오른쪽으로
        /// 이동할지를 결정하며, 글리프 간 간격의 기본 폭이 됩니다.
        /// 로드한 폰트의 글리프 메트릭을 수정할 수 있습니다.
        /// </summary>
        public int AdvanceWidth { get; set; }

        /// <summary>
        /// left side bearing(폰트 단위)입니다.
        /// pen 기원점(x=0)에서 글리프 윤곽의 왼쪽 끝(xMin)까지의 거리로,
        /// TTF 렌더러는 이 값 기준으로 glyf 좌표를 재배치하므로
        /// 실제 윤곽의 xMin과 일치해야 합니다.
        /// 로드한 폰트의 글리프 메트릭을 수정할 수 있습니다.
        /// </summary>
        public int LeftSideBearing { get; set; }

        /// <summary>
        /// <see cref="Glyph"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="outline">글리프 윤곽입니다.</param>
        /// <param name="advanceWidth">advance width(폰트 단위)입니다.</param>
        /// <param name="leftSideBearing">왼쪽 사이드 베어링(폰트 단위)입니다.</param>
        public Glyph(GlyphOutline outline, int advanceWidth, int leftSideBearing)
        {
            Outline = outline;
            AdvanceWidth = advanceWidth;
            LeftSideBearing = leftSideBearing;
        }

        /// <summary>
        /// <see cref="Glyph"/>의 새 인스턴스를 생성합니다.
        /// left side bearing은 윤곽의 실제 xMin으로 자동 계산됩니다.
        /// </summary>
        /// <param name="outline">글리프 윤곽입니다.</param>
        /// <param name="advanceWidth">advance width(폰트 단위)입니다.</param>
        public Glyph(GlyphOutline outline, int advanceWidth)
            : this(outline, advanceWidth, ComputeLeftSideBearing(outline))
        {
        }

        /// <summary>
        /// <see cref="Glyph"/>의 새 인스턴스를 생성합니다.
        /// left side bearing은 윤곽의 실제 xMin으로, advance width는
        /// xMax - xMin + 2*lsb(대칭 사이드 베어링)로 자동 계산됩니다.
        /// </summary>
        /// <param name="outline">글리프 윤곽입니다.</param>
        public Glyph(GlyphOutline outline)
            : this(outline, ComputeAdvanceWidth(outline))
        {
        }

        /// <summary>
        /// 직사각형 글리프를 생성합니다.
        /// left side bearing은 x(왼쪽 끝)로 자동 계산됩니다.
        /// </summary>
        /// <param name="x">왼쪽 X 좌표(폰트 단위)입니다.</param>
        /// <param name="y">아래쪽 Y 좌표(폰트 단위)입니다.</param>
        /// <param name="w">폭(폰트 단위)입니다.</param>
        /// <param name="h">높이(폰트 단위)입니다.</param>
        /// <param name="advanceWidth">advance width(폰트 단위)입니다.</param>
        public static Glyph Rectangle(double x, double y, double w, double h, int advanceWidth)
        {
            var outline = new GlyphOutline();
            outline.AddRectangle(x, y, w, h);
            return new Glyph(outline, advanceWidth);
        }

        /// <summary>
        /// 원 글리프를 생성합니다.
        /// left side bearing은 cx - r(왼쪽 끝)로 자동 계산됩니다.
        /// </summary>
        /// <param name="cx">중심 X 좌표(폰트 단위)입니다.</param>
        /// <param name="cy">중심 Y 좌표(폰트 단위)입니다.</param>
        /// <param name="r">반지름(폰트 단위)입니다.</param>
        /// <param name="advanceWidth">advance width(폰트 단위)입니다.</param>
        public static Glyph Circle(double cx, double cy, double r, int advanceWidth)
        {
            var outline = new GlyphOutline();
            outline.AddCircle(cx, cy, r);
            return new Glyph(outline, advanceWidth);
        }

        /// <summary>
        /// 윤곽의 바운딩 박스(contour별 xMin/xMax 극값의 min/max)를 계산합니다.
        /// 빈 윤곽이면 false를 반환합니다.
        /// </summary>
        private static bool ComputeBoundingBox(GlyphOutline outline, out double xMin, out double xMax)
        {
            xMin = double.MaxValue;
            xMax = double.MinValue;
            foreach (var contour in outline.Contours)
                if (contour.ComputeBoundingBox(out double cMin, out _, out double cMax, out _))
                {
                    xMin = Math.Min(xMin, cMin);
                    xMax = Math.Max(xMax, cMax);
                }
            return xMin != double.MaxValue;
        }

        /// <summary>
        /// left side bearing을 윤곽의 실제 xMin으로 계산합니다.
        /// lsb는 펜 기원점(x=0)에서 글리프 윤곽의 왼쪽 끝(xMin)까지의 거리로,
        /// TTF 렌더러가 lsb 기준으로 glyf 좌표를 재배치하므로 실제 xMin과 일치해야 합니다.
        /// contour별 xMin(곡선 극값)의 최소값을 반올림하여 반환하며, 빈 윤곽이면 0을 반환합니다.
        /// </summary>
        private static int ComputeLeftSideBearing(GlyphOutline outline)
            => ComputeBoundingBox(outline, out double xMin, out _) ? (int)Math.Round(xMin) : 0;

        /// <summary>
        /// advance width를 xMax - xMin + 2*lsb(대칭 사이드 베어링)로 계산합니다.
        /// 빈 윤곽이면 0을 반환합니다.
        /// </summary>
        private static int ComputeAdvanceWidth(GlyphOutline outline)
        {
            if (!ComputeBoundingBox(outline, out double xMin, out double xMax))
                return 0;
            int lsb = (int)Math.Round(xMin);
            return (int)Math.Round(xMax - xMin + 2 * lsb);
        }
    }
}
