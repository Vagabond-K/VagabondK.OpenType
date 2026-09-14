using System;
using System.Collections;
using System.Collections.Generic;

namespace VagabondK.OpenType.Geometry
{
    /// <summary>
    /// 글리프의 전체 윤곽(outline)을 나타냅니다.
    /// 하나 이상의 닫힌 윤곽(<see cref="GlyphContour"/>)으로 구성됩니다.
    /// 예를 들어 'B' 글리프는 바깥 윤곽과 안쪽 구멍 윤곽 2개로 이루어집니다.
    /// glyf 테이블의 simple glyph에 해당합니다.
    /// contour의 감김 방향(winding direction)은 이 구현에서 검증하지 않으며,
    /// 렌더러는 nonzero fill rule로 구멍을 처리합니다.
    /// <see cref="IList{T}"/>를 구현해 contour를 인덱스로 접근하거나
    /// 중간에 삽입/제거할 수 있습니다.
    /// </summary>
    public class GlyphOutline : IList<GlyphContour>
    {
        private List<GlyphContour> contours;

        /// <summary>
        /// 이 윤곽에 포함된 contour 목록을 반환합니다.
        /// </summary>
        public IReadOnlyList<GlyphContour> Contours => contours;

        /// <summary>
        /// <see cref="GlyphOutline"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        public GlyphOutline()
        {
            contours = new List<GlyphContour>();
        }

        // ---- IList<GlyphContour> ----

        /// <summary>
        /// contour 개수를 반환합니다.
        /// </summary>
        public int Count => contours.Count;

        /// <summary>
        /// 읽기 전용 여부를 반환합니다. 항상 false입니다.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// 지정된 인덱스의 contour를 가져오거나 설정합니다.
        /// </summary>
        public GlyphContour this[int index]
        {
            get => contours[index];
            set => contours[index] = value;
        }

        /// <summary>
        /// contour를 목록 끝에 추가합니다. <see cref="AddContour"/>와 동일합니다.
        /// </summary>
        public void Add(GlyphContour item) => AddContour(item);

        /// <summary>
        /// 지정된 인덱스에 contour를 삽입합니다.
        /// </summary>
        public void Insert(int index, GlyphContour item) => contours.Insert(index, item);

        /// <summary>
        /// contour를 목록에서 제거합니다. 제거되면 true, 없으면 false입니다.
        /// </summary>
        public bool Remove(GlyphContour item) => contours.Remove(item);

        /// <summary>
        /// contour가 목록에 있는지 확인합니다.
        /// </summary>
        public bool Contains(GlyphContour item) => contours.Contains(item);

        /// <summary>
        /// contour의 인덱스를 반환합니다. 없으면 -1입니다.
        /// </summary>
        public int IndexOf(GlyphContour item) => contours.IndexOf(item);

        /// <summary>
        /// contour를 배열에 복사합니다.
        /// </summary>
        public void CopyTo(GlyphContour[] array, int arrayIndex) => contours.CopyTo(array, arrayIndex);

        /// <summary>
        /// contour를 순회하는 enumerators를 반환합니다.
        /// </summary>
        public IEnumerator<GlyphContour> GetEnumerator() => contours.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => contours.GetEnumerator();

        /// <summary>
        /// 지정된 인덱스의 contour를 제거합니다.
        /// </summary>
        public void RemoveAt(int index) => contours.RemoveAt(index);

        /// <summary>
        /// 새로운 닫힌 윤곽(contour)을 시작하고 그 contour를 반환합니다.
        /// 반환된 <see cref="GlyphContour"/>에 <c>MoveTo</c>, <c>LineTo</c>, <c>QuadTo</c>, <c>CurveTo</c> 등을
        /// 호출하여 모양을 그립니다. 첫 커맨드는 반드시 <c>MoveTo</c>여야 합니다.
        /// </summary>
        /// <returns>그릴 contour입니다.</returns>
        public GlyphContour BeginContour()
        {
            var contour = new GlyphContour();
            contours.Add(contour);
            return contour;
        }

        /// <summary>
        /// 기존 contour를 이 윤곽에 추가합니다.
        /// 여러 글리프가 같은 contour 인스턴스를 참조하면
        /// CFF에서는 서브루틴으로 추출됩니다.
        /// </summary>
        /// <param name="contour">추가할 contour입니다.</param>
        public void AddContour(GlyphContour contour) => contours.Add(contour);

        /// <summary>
        /// 모든 contour를 제거합니다. 로드한 글리프의 윤곽을 비운 후
        /// <see cref="BeginContour"/> 등으로 다시 그릴 때 사용합니다.
        /// </summary>
        public void Clear() => contours.Clear();

        /// <summary>
        /// 지정된 인덱스의 contour를 제거합니다.
        /// </summary>
        /// <param name="index">제거할 contour의 인덱스입니다. (0-based)</param>
        public void RemoveContour(int index) => contours.RemoveAt(index);

        /// <summary>
        /// 직사각형 contour를 추가하고 그 contour를 반환합니다.
        /// (x, y)를 왼쪽 아래 꼭짓점으로 하는 직사각형을 그립니다.
        /// </summary>
        /// <param name="x">왼쪽 X 좌표(폰트 단위)입니다.</param>
        /// <param name="y">아래쪽 Y 좌표(폰트 단위)입니다.</param>
        /// <param name="w">폭(폰트 단위)입니다.</param>
        /// <param name="h">높이(폰트 단위)입니다.</param>
        /// <returns>추가된 contour입니다.</returns>
        public GlyphContour AddRectangle(double x, double y, double w, double h)
        {
            var contour = BeginContour();
            contour.MoveTo(x, y);
            contour.LineTo(x + w, y);
            contour.LineTo(x + w, y + h);
            contour.LineTo(x, y + h);
            return contour;
        }

        /// <summary>
        /// 원 contour를 추가하고 그 contour를 반환합니다.
        /// 3차 베지어 곡선으로 근사하며, 각 세그먼트의 제어점은
        /// (4/3)·tan(세그먼트 각도/4)·반지름으로 계산됩니다.
        /// 꼭대기(상단)에서 시작해 <paramref name="clockwise"/> 방향으로 그립니다.
        /// </summary>
        /// <param name="cx">중심 X 좌표(폰트 단위)입니다.</param>
        /// <param name="cy">중심 Y 좌표(폰트 단위)입니다.</param>
        /// <param name="r">반지름(폰트 단위)입니다.</param>
        /// <param name="segments">베지어 세그먼트 수(기본 4)입니다.</param>
        /// <param name="clockwise">시계 방향이면 true(기본), 반시계 방향이면 false입니다.</param>
        /// <returns>추가된 contour입니다.</returns>
        public GlyphContour AddCircle(double cx, double cy, double r, int segments = 4, bool clockwise = true)
            => AddEllipse(cx, cy, r, r, segments, clockwise);

        /// <summary>
        /// 타원 contour를 추가하고 그 contour를 반환합니다.
        /// 3차 베지어 곡선으로 근사하며, 각 세그먼트의 제어점은
        /// (4/3)·tan(세그먼트 각도/4)·반지름으로 계산됩니다.
        /// 꼭대기(상단)에서 시작해 <paramref name="clockwise"/> 방향으로 그립니다.
        /// </summary>
        /// <param name="cx">중심 X 좌표(폰트 단위)입니다.</param>
        /// <param name="cy">중심 Y 좌표(폰트 단위)입니다.</param>
        /// <param name="rx">X 반지름(폰트 단위)입니다.</param>
        /// <param name="ry">Y 반지름(폰트 단위)입니다.</param>
        /// <param name="segments">베지어 세그먼트 수(기본 4)입니다.</param>
        /// <param name="clockwise">시계 방향이면 true(기본), 반시계 방향이면 false입니다.</param>
        /// <returns>추가된 contour입니다.</returns>
        public GlyphContour AddEllipse(double cx, double cy, double rx, double ry, int segments = 4, bool clockwise = true)
        {
            var contour = BeginContour();
            int dir = clockwise ? -1 : 1;
            double dTheta = 2.0 * Math.PI / segments;
            double c = (4.0 / 3.0) * Math.Tan(dTheta / 4.0);
            double phi = Math.PI / 2.0; // 시작: 꼭대기(상단)
            contour.MoveTo(cx + rx * Math.Cos(phi), cy + ry * Math.Sin(phi));
            for (int i = 0; i < segments; i++)
            {
                double phi1 = phi + dir * dTheta;
                double c1x = cx + rx * Math.Cos(phi) - c * dir * rx * Math.Sin(phi);
                double c1y = cy + ry * Math.Sin(phi) + c * dir * ry * Math.Cos(phi);
                double c2x = cx + rx * Math.Cos(phi1) + c * dir * rx * Math.Sin(phi1);
                double c2y = cy + ry * Math.Sin(phi1) - c * dir * ry * Math.Cos(phi1);
                double ex = cx + rx * Math.Cos(phi1);
                double ey = cy + ry * Math.Sin(phi1);
                contour.CurveTo(c1x, c1y, c2x, c2y, ex, ey);
                phi = phi1;
            }
            return contour;
        }

        /// <summary>
        /// 윤곽의 바운딩 박스를 계산합니다.
        /// 각 contour의 정확한 바운딩 박스(곡선 극값 포함)를 통합하며,
        /// min은 내림(Floor), max는 올림(Ceiling)하여 모든 곡선 극값이 박스에 포함되도록 합니다.
        /// head의 xMin/yMin/xMax/yMax 계산과 동일한 규칙입니다.
        /// </summary>
        /// <param name="xMin">최소 X 좌표(폰트 단위)입니다.</param>
        /// <param name="yMin">최소 Y 좌표(폰트 단위)입니다.</param>
        /// <param name="xMax">최대 X 좌표(폰트 단위)입니다.</param>
        /// <param name="yMax">최대 Y 좌표(폰트 단위)입니다.</param>
        /// <returns>점이 하나 이상이면 true, 빈 윤곽이면 false입니다.</returns>
        public bool ComputeBoundingBox(out int xMin, out int yMin, out int xMax, out int yMax)
        {
            xMin = 0; yMin = 0; xMax = 0; yMax = 0;
            double gxMin = 0, gyMin = 0, gxMax = 0, gyMax = 0;
            bool hasPoint = false;

            foreach (var contour in contours)
            {
                if (contour.ComputeBoundingBox(out double cxMin, out double cyMin, out double cxMax, out double cyMax))
                {
                    if (!hasPoint)
                    {
                        gxMin = cxMin; gyMin = cyMin;
                        gxMax = cxMax; gyMax = cyMax;
                        hasPoint = true;
                    }
                    else
                    {
                        if (cxMin < gxMin) gxMin = cxMin;
                        if (cyMin < gyMin) gyMin = cyMin;
                        if (cxMax > gxMax) gxMax = cxMax;
                        if (cyMax > gyMax) gyMax = cyMax;
                    }
                }
            }

            if (!hasPoint)
                return false;

            xMin = (int)Math.Floor(gxMin);
            yMin = (int)Math.Floor(gyMin);
            xMax = (int)Math.Ceiling(gxMax);
            yMax = (int)Math.Ceiling(gyMax);
            return true;
        }
    }
}
