using System;
using System.Collections;
using System.Collections.Generic;

namespace VagabondK.OpenType.Geometry
{
    /// <summary>
    /// 글리프 윤곽선의 닫힌 윤곽(contour)을 나타냅니다.
    /// 그리기 커맨드(<see cref="ContourCommand"/>)의 집합으로 관리되며,
    /// 마지막 세그먼트는 자동으로 첫 점과 연결되어 닫힙니다.
    /// 좌표는 pen 기원점 기준 절대 좌표(폰트 단위)이며, Y 양수 방향은 위쪽입니다.
    /// <see cref="IList{T}"/>를 구현해 커맨드를 인덱스로 접근하거나
    /// 중간에 삽입/제거할 수 있습니다.
    /// </summary>
    public class GlyphContour : IList<ContourCommand>
    {
        private List<ContourCommand> commands;

        /// <summary>
        /// 이 윤곽에 포함된 그리기 커맨드 목록을 순서대로 반환합니다.
        /// 첫 커맨드는 MoveToCommand여야 합니다.
        /// </summary>
        public IReadOnlyList<ContourCommand> Commands => commands;

        /// <summary>
        /// <see cref="GlyphContour"/>의 새 인스턴스를 생성합니다.
        /// 여러 글리프가 같은 contour 인스턴스를 참조하면
        /// CFF에서는 서브루틴으로 추출됩니다.
        /// </summary>
        public GlyphContour()
        {
            commands = new List<ContourCommand>();
        }

        // ---- IList<ContourCommand> ----

        /// <summary>
        /// 커맨드 개수를 반환합니다.
        /// </summary>
        public int Count => commands.Count;

        /// <summary>
        /// 읽기 전용 여부를 반환합니다. 항상 false입니다.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// 지정된 인덱스의 커맨드를 가져오거나 설정합니다.
        /// 인덱스 0에는 MoveToCommand가 있어야 직렬화가 정상 동작합니다.
        /// </summary>
        public ContourCommand this[int index]
        {
            get => commands[index];
            set => commands[index] = value;
        }

        /// <summary>
        /// 커맨드를 목록 끝에 추가합니다.
        /// </summary>
        public void Add(ContourCommand item) => commands.Add(item);

        /// <summary>
        /// 지정된 인덱스에 커맨드를 삽입합니다.
        /// </summary>
        public void Insert(int index, ContourCommand item) => commands.Insert(index, item);

        /// <summary>
        /// 커맨드를 목록에서 제거합니다. 제거되면 true, 없으면 false입니다.
        /// </summary>
        public bool Remove(ContourCommand item) => commands.Remove(item);

        /// <summary>
        /// 커맨드가 목록에 있는지 확인합니다.
        /// </summary>
        public bool Contains(ContourCommand item) => commands.Contains(item);

        /// <summary>
        /// 커맨드의 인덱스를 반환합니다. 없으면 -1입니다.
        /// </summary>
        public int IndexOf(ContourCommand item) => commands.IndexOf(item);

        /// <summary>
        /// 커맨드를 배열에 복사합니다.
        /// </summary>
        public void CopyTo(ContourCommand[] array, int arrayIndex) => commands.CopyTo(array, arrayIndex);

        /// <summary>
        /// 커맨드를 순회하는 enumerators를 반환합니다.
        /// </summary>
        public IEnumerator<ContourCommand> GetEnumerator() => commands.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => commands.GetEnumerator();

        /// <summary>
        /// 모든 커맨드를 제거합니다. 로드한 contour를 비운 후
        /// <see cref="MoveTo"/> 등으로 다시 그릴 때 사용합니다.
        /// </summary>
        public void Clear() => commands.Clear();

        /// <summary>
        /// 지정된 인덱스의 커맨드를 제거합니다.
        /// </summary>
        /// <param name="index">제거할 커맨드의 인덱스입니다. (0-based)</param>
        public void RemoveAt(int index) => commands.RemoveAt(index);

        /// <summary>
        /// 윤곽의 시작점으로 이동합니다. contour의 첫 커맨드로 사용해야 합니다.
        /// CFF에서는 hmoveto/vmoveto/rmoveto로, glyf에서는 첫 on-curve 점으로 직렬화됩니다.
        /// </summary>
        /// <param name="x">X 좌표(폰트 단위)입니다. 소수가 허용됩니다.</param>
        /// <param name="y">Y 좌표(폰트 단위)입니다. 소수가 허용됩니다.</param>
        /// <returns>이 contour(플루언트 체인용)입니다.</returns>
        public GlyphContour MoveTo(double x, double y) { commands.Add(new MoveToCommand(x, y)); return this; }

        /// <summary>
        /// 현재 위치에서 지정된 점까지 직선을 그립니다.
        /// CFF에서는 rlineto(상대 오프셋)로, glyf에서는 on-curve 점으로 직렬화됩니다.
        /// </summary>
        /// <param name="x">종점의 X 좌표(폰트 단위)입니다. 소수가 허용됩니다.</param>
        /// <param name="y">종점의 Y 좌표(폰트 단위)입니다. 소수가 허용됩니다.</param>
        /// <returns>이 contour(플루언트 체인용)입니다.</returns>
        public GlyphContour LineTo(double x, double y) { commands.Add(new LineToCommand(x, y)); return this; }

        /// <summary>
        /// 현재 위치에서 지정된 제어점을 거치는 2차 베지어 곡선을 지정된 점까지 그립니다.
        /// glyf는 2차 베지어를 네이티브로 지원하며, CFF에서는 3차 베지어로 변환해
        /// rrcurveto로 기록합니다.
        /// </summary>
        /// <param name="controlX">제어점의 X 좌표(폰트 단위)입니다. 소수가 허용됩니다.</param>
        /// <param name="controlY">제어점의 Y 좌표(폰트 단위)입니다. 소수가 허용됩니다.</param>
        /// <param name="x">종점의 X 좌표(폰트 단위)입니다. 소수가 허용됩니다.</param>
        /// <param name="y">종점의 Y 좌표(폰트 단위)입니다. 소수가 허용됩니다.</param>
        /// <returns>이 contour(플루언트 체인용)입니다.</returns>
        public GlyphContour QuadTo(double controlX, double controlY, double x, double y)
        { commands.Add(new QuadraticBezierCommand(controlX, controlY, x, y)); return this; }

        /// <summary>
        /// 현재 위치에서 두 제어점을 거치는 3차 베지어 곡선을 지정된 점까지 그립니다.
        /// CFF(Type 2 charstring)는 3차 베지어를 네이티브로 지원하며,
        /// TTF(glyf)는 <see cref="EnumerateTtfPoints"/>에서 2개의 2차 베지어로 변환됩니다.
        /// </summary>
        /// <param name="c1x">1차 제어점의 X 좌표(폰트 단위)입니다.</param>
        /// <param name="c1y">1차 제어점의 Y 좌표(폰트 단위)입니다.</param>
        /// <param name="c2x">2차 제어점의 X 좌표(폰트 단위)입니다.</param>
        /// <param name="c2y">2차 제어점의 Y 좌표(폰트 단위)입니다.</param>
        /// <param name="x">종점의 X 좌표(폰트 단위)입니다.</param>
        /// <param name="y">종점의 Y 좌표(폰트 단위)입니다.</param>
        /// <returns>이 contour(플루언트 체인용)입니다.</returns>
        public GlyphContour CurveTo(double c1x, double c1y, double c2x, double c2y, double x, double y)
        { commands.Add(new CubicBezierCommand(c1x, c1y, c2x, c2y, x, y)); return this; }

        /// <summary>
        /// TTF(glyf) 직렬화용 점 목록을 순서대로 반환합니다.
        /// 3차 베지어 세그먼트는 <see cref="CubicBezierCommand.ToQuadraticBezierCommands"/>
        /// 로 2차 베지어 시퀀스로 변환됩니다.
        /// glyf의 점 배열(on-curve/off-curve 플래그 포함)을 구성하는 데 사용됩니다.
        /// </summary>
        public List<GlyphPoint> EnumerateTtfPoints()
        {
            var result = new List<GlyphPoint>();
            if (commands.Count == 0)
                return result;

            var start = (ContourCommand)commands[0];
            double curX = start.EndX, curY = start.EndY;
            result.Add(new GlyphPoint(curX, curY, true));

            for (int i = 1; i < commands.Count; i++)
            {
                var cmd = commands[i];
                if (cmd is QuadraticBezierCommand quad)
                {
                    result.Add(new GlyphPoint(quad.ControlX, quad.ControlY, false));
                    result.Add(new GlyphPoint(quad.X, quad.Y, true));
                    curX = quad.X;
                    curY = quad.Y;
                }
                else if (cmd is CubicBezierCommand cubic)
                {
                    // cubic → two quads (C1 연속을 보장하는 표준 변환, 결과는 캐시됨)
                    foreach (var q in cubic.ToQuadraticBezierCommands(curX, curY))
                    {
                        result.Add(new GlyphPoint(q.ControlX, q.ControlY, false));
                        result.Add(new GlyphPoint(q.X, q.Y, true));
                    }
                    curX = cubic.X;
                    curY = cubic.Y;
                }
                else
                {
                    // LineToCommand
                    result.Add(new GlyphPoint(cmd.EndX, cmd.EndY, true));
                    curX = cmd.EndX;
                    curY = cmd.EndY;
                }
            }
            return result;
        }

        /// <summary>
        /// 윤곽의 정확한 바운딩 박스를 계산합니다.
        /// 베지어 곡선은 제어점(convex hull)이 아닌 곡선 자체의 극값을 사용하며,
        /// 각 세그먼트의 도함수가 0이 되는 t(0..1)에서 곡선을 평가해
        /// 실제 곡선이 도달하는 최소/최대 좌표를 구합니다.
        /// head의 xMin/yMin/xMax/yMax, CFF의 FontBBox 계산에 사용됩니다.
        /// </summary>
        /// <param name="xMin">최소 X 좌표(폰트 단위)입니다.</param>
        /// <param name="yMin">최소 Y 좌표(폰트 단위)입니다.</param>
        /// <param name="xMax">최대 X 좌표(폰트 단위)입니다.</param>
        /// <param name="yMax">최대 Y 좌표(폰트 단위)입니다.</param>
        /// <returns>점이 하나 이상이면 true, 빈 윤곽이면 false입니다.</returns>
        public bool ComputeBoundingBox(out double xMin, out double yMin, out double xMax, out double yMax)
        {
            xMin = 0; yMin = 0; xMax = 0; yMax = 0;
            if (commands.Count == 0)
                return false;

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            double curX = commands[0].EndX, curY = commands[0].EndY;
            UpdateBBox(ref minX, ref minY, ref maxX, ref maxY, curX, curY);

            for (int i = 1; i < commands.Count; i++)
            {
                var cmd = commands[i];
                if (cmd is QuadraticBezierCommand quad)
                {
                    AddQuadraticExtremum(curX, curY, quad.ControlX, quad.ControlY, quad.X, quad.Y,
                        ref minX, ref minY, ref maxX, ref maxY);
                    UpdateBBox(ref minX, ref minY, ref maxX, ref maxY, quad.X, quad.Y);
                    curX = quad.X; curY = quad.Y;
                }
                else if (cmd is CubicBezierCommand cubic)
                {
                    AddCubicExtremum(curX, curY, cubic.C1X, cubic.C1Y, cubic.C2X, cubic.C2Y, cubic.X, cubic.Y,
                        ref minX, ref minY, ref maxX, ref maxY);
                    UpdateBBox(ref minX, ref minY, ref maxX, ref maxY, cubic.X, cubic.Y);
                    curX = cubic.X; curY = cubic.Y;
                }
                else
                {
                    // LineToCommand: 직선은 내부 극값이 없으므로 종점만 반영
                    UpdateBBox(ref minX, ref minY, ref maxX, ref maxY, cmd.EndX, cmd.EndY);
                    curX = cmd.EndX; curY = cmd.EndY;
                }
            }

            xMin = minX; yMin = minY; xMax = maxX; yMax = maxY;
            return true;
        }

        private static void UpdateBBox(ref double minX, ref double minY, ref double maxX, ref double maxY, double x, double y)
        {
            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
        }

        /// <summary>
        /// 2차 베지어 세그먼트의 내부 극값을 바운딩 박스에 반영합니다.
        /// B(t) = (1-t)²P0 + 2(1-t)t·C1 + t²P2, B'(t)=0 → t = (P0-C1)/(P2-2C1+P0)
        /// x, y 방향은 서로 다른 t에서 극값을 가지므로 독립적으로 계산합니다.
        /// </summary>
        private static void AddQuadraticExtremum(
            double p0x, double p0y, double c1x, double c1y, double p2x, double p2y,
            ref double minX, ref double minY, ref double maxX, ref double maxY)
        {
            double denomX = p2x - 2 * c1x + p0x;
            if (Math.Abs(denomX) > 1e-12)
            {
                double t = (p0x - c1x) / denomX;
                if (t > 0 && t < 1)
                {
                    double mt = 1 - t;
                    double x = mt * mt * p0x + 2 * mt * t * c1x + t * t * p2x;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                }
            }

            double denomY = p2y - 2 * c1y + p0y;
            if (Math.Abs(denomY) > 1e-12)
            {
                double t = (p0y - c1y) / denomY;
                if (t > 0 && t < 1)
                {
                    double mt = 1 - t;
                    double y = mt * mt * p0y + 2 * mt * t * c1y + t * t * p2y;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        /// <summary>
        /// 3차 베지어 세그먼트의 내부 극값을 바운딩 박스에 반영합니다.
        /// B'(t)=0은 2차 방정식 a·t²+b·t+c=0이며,
        /// a = -P0+3C1-3C2+P3, b = 2(P0-2C1+C2), c = C1-P0 입니다.
        /// x, y 방향은 독립적으로 계산합니다.
        /// </summary>
        private static void AddCubicExtremum(
            double p0x, double p0y, double c1x, double c1y, double c2x, double c2y, double p3x, double p3y,
            ref double minX, ref double minY, ref double maxX, ref double maxY)
        {
            AddCubicAxisExtremum(p0x, c1x, c2x, p3x, ref minX, ref maxX);
            AddCubicAxisExtremum(p0y, c1y, c2y, p3y, ref minY, ref maxY);
        }

        private static void AddCubicAxisExtremum(
            double p0, double c1, double c2, double p3,
            ref double min, ref double max)
        {
            double a = -p0 + 3 * c1 - 3 * c2 + p3;
            double b = 2 * (p0 - 2 * c1 + c2);
            double c = c1 - p0;

            double t1 = 0, t2 = 0;
            int rootCount;
            if (Math.Abs(a) < 1e-12)
            {
                // 선형: b·t + c = 0
                if (Math.Abs(b) < 1e-12)
                    return;
                t1 = -c / b;
                rootCount = 1;
            }
            else
            {
                double disc = b * b - 4 * a * c;
                if (disc < 0)
                    return;
                double sqrtDisc = Math.Sqrt(disc);
                t1 = (-b - sqrtDisc) / (2 * a);
                t2 = (-b + sqrtDisc) / (2 * a);
                rootCount = 2;
            }

            for (int i = 0; i < rootCount; i++)
            {
                double t = (i == 0) ? t1 : t2;
                if (t > 0 && t < 1)
                {
                    double mt = 1 - t;
                    double v = mt * mt * mt * p0 + 3 * mt * mt * t * c1 + 3 * mt * t * t * c2 + t * t * t * p3;
                    if (v < min) min = v;
                    if (v > max) max = v;
                }
            }
        }
    }
}
