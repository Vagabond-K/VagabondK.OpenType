using System;
using System.Collections.Generic;

namespace VagabondK.OpenType.Geometry
{
    /// <summary>
    /// 윤곽 그리기 커맨드의 기본 클래스입니다.
    /// 각 커맨드는 이전 위치에서 종점까지의 세그먼트를 나타냅니다.
    /// 좌표는 pen 기원점 기준 절대 좌표(폰트 단위)이며, Y 양수 방향은 위쪽입니다.
    /// </summary>
    public abstract class ContourCommand
    {
        /// <summary>커맨드의 종점 X 좌표(폰트 단위)입니다.</summary>
        public abstract double EndX { get; }

        /// <summary>커맨드의 종점 Y 좌표(폰트 단위)입니다.</summary>
        public abstract double EndY { get; }
    }

    /// <summary>
    /// 윤곽의 시작점으로 이동하는 커맨드입니다.
    /// CFF에서는 hmoveto/vmoveto/rmoveto, glyf에서는 첫 점(on-curve)으로 직렬화됩니다.
    /// </summary>
    public class MoveToCommand : ContourCommand
    {
        /// <summary>시작점 X 좌표입니다.</summary>
        public double X { get; }

        /// <summary>시작점 Y 좌표입니다.</summary>
        public double Y { get; }

        /// <inheritdoc />
        public override double EndX => X;

        /// <inheritdoc />
        public override double EndY => Y;

        /// <summary>
        /// <see cref="MoveToCommand"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        public MoveToCommand(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// 직선을 그리는 커맨드입니다.
    /// CFF에서는 rlineto(상대 오프셋), glyf에서는 on-curve 점으로 직렬화됩니다.
    /// </summary>
    public class LineToCommand : ContourCommand
    {
        /// <summary>종점 X 좌표입니다.</summary>
        public double X { get; }

        /// <summary>종점 Y 좌표입니다.</summary>
        public double Y { get; }

        /// <inheritdoc />
        public override double EndX => X;

        /// <inheritdoc />
        public override double EndY => Y;

        /// <summary>
        /// <see cref="LineToCommand"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        public LineToCommand(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// 2차 베지어 곡선을 그리는 커맨드입니다.
    /// glyf는 2차 베지어를 네이티브로 지원하며, CFF에서는 quad를
    /// 3차 베지어로 변환해 rrcurveto로 기록합니다.
    /// </summary>
    public class QuadraticBezierCommand : ContourCommand
    {
        /// <summary>제어점 X 좌표입니다. 곡선은 이 점을 거치지 않고 방향만 영향을 받습니다.</summary>
        public double ControlX { get; }

        /// <summary>제어점 Y 좌표입니다.</summary>
        public double ControlY { get; }

        /// <summary>종점 X 좌표입니다.</summary>
        public double X { get; }

        /// <summary>종점 Y 좌표입니다.</summary>
        public double Y { get; }

        /// <inheritdoc />
        public override double EndX => X;

        /// <inheritdoc />
        public override double EndY => Y;

        /// <summary>
        /// <see cref="QuadraticBezierCommand"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        public QuadraticBezierCommand(double controlX, double controlY, double x, double y)
        {
            ControlX = controlX;
            ControlY = controlY;
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// 3차 베지어 곡선을 그리는 커맨드입니다.
    /// CFF(Type 2 charstring)는 3차 베지어를 네이티브로 지원하며,
    /// TTF(glyf)는 2차 베지어만 지원하므로 직렬화 시
    /// <see cref="ToQuadraticBezierCommands"/>로 2차 베지어 시퀀스로 변환됩니다.
    /// </summary>
    public class CubicBezierCommand : ContourCommand
    {
        /// <summary>1차 제어점 X 좌표입니다. 시작점에서의 접선 방향을 결정합니다.</summary>
        public double C1X { get; }

        /// <summary>1차 제어점 Y 좌표입니다.</summary>
        public double C1Y { get; }

        /// <summary>2차 제어점 X 좌표입니다. 종점에서의 접선 방향을 결정합니다.</summary>
        public double C2X { get; }

        /// <summary>2차 제어점 Y 좌표입니다.</summary>
        public double C2Y { get; }

        /// <summary>종점 X 좌표입니다.</summary>
        public double X { get; }

        /// <summary>종점 Y 좌표입니다.</summary>
        public double Y { get; }

        /// <inheritdoc />
        public override double EndX => X;

        /// <inheritdoc />
        public override double EndY => Y;

        /// <summary>
        /// <see cref="CubicBezierCommand"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        public CubicBezierCommand(double c1x, double c1y, double c2x, double c2y, double x, double y)
        {
            C1X = c1x;
            C1Y = c1y;
            C2X = c2x;
            C2Y = c2y;
            X = x;
            Y = y;
        }

        private List<QuadraticBezierCommand> quads;

        /// <summary>
        /// 2차 근사 허용 오차(폰트 단위)입니다.
        /// 3차 성분의 길이가 8 * MaxError 이하이면 단일 2차 베지어로 근사합니다.
        /// </summary>
        private const double MaxError = 1.0;

        /// <summary>
        /// 이 3차 베지어를 2차 베지어 시퀀스로 변환한 결과를 반환합니다.
        /// TTF(glyf)는 3차 베지어를 지원하지 않아 2차로 변환하며,
        /// fonttools cu2qu 방식의 적응적 분할을 사용합니다.
        /// 3차 성분이 허용 오차 이하이면 단일 2차로 근사하고,
        /// 아니면 t=0.5에서 드 카스텔조 분할 후 재귀적으로 처리합니다.
        /// 각 2차 베지어의 제어점은 양 끝점 접선 교차점을 사용해
        /// 분할 지점에서 C1 연속(부드러운 연결)을 보장합니다.
        /// 변환 결과는 시작점 기준 한 번만 계산해 캐시하며, 이후 호출은 캐시를 재사용합니다.
        /// </summary>
        /// <param name="startX">시작점 X 좌표(이 세그먼트의 출발점)입니다.</param>
        /// <param name="startY">시작점 Y 좌표(이 세그먼트의 출발점)입니다.</param>
        internal IReadOnlyList<QuadraticBezierCommand> ToQuadraticBezierCommands(double startX, double startY)
        {
            if (quads != null)
                return quads;

            var result = new List<QuadraticBezierCommand>();
            AppendQuadratic(result, startX, startY, C1X, C1Y, C2X, C2Y, X, Y);
            quads = result;
            return quads;
        }

        /// <summary>
        /// cu2qu 방식의 적응적 분할을 재귀적으로 수행합니다.
        /// 3차 성분 c = P3 - 3C2 + 3C1 - P0 의 길이가 8 * MaxError 이하이면
        /// 단일 2차 베지어로 근사하고, 아니면 t=0.5에서 드 카스텔조 분할 후 재귀 처리합니다.
        /// </summary>
        private static void AppendQuadratic(
            List<QuadraticBezierCommand> result,
            double p0x, double p0y, double c1x, double c1y, double c2x, double c2y, double p3x, double p3y)
        {
            // 3차 성분: c = P3 - 3C2 + 3C1 - P0
            double ccx = p3x - 3 * c2x + 3 * c1x - p0x;
            double ccy = p3y - 3 * c2y + 3 * c1y - p0y;
            double cLen = Math.Sqrt(ccx * ccx + ccy * ccy);

            if (cLen <= 8 * MaxError)
            {
                // 2차에 충분히 가까움: 단일 2차 베지어로 근사
                // C1 연속 보장을 위해 접선 교차점 제어점 사용, 평행 시 최소제곱 근사
                double qx, qy;
                if (IntersectTangents(p0x, p0y, c1x - p0x, c1y - p0y,
                                      p3x, p3y, p3x - c2x, p3y - c2y, out qx, out qy))
                {
                    result.Add(new QuadraticBezierCommand(qx, qy, p3x, p3y));
                }
                else
                {
                    qx = (3 * c1x + 3 * c2x - p0x - p3x) / 4.0;
                    qy = (3 * c1y + 3 * c2y - p0y - p3y) / 4.0;
                    result.Add(new QuadraticBezierCommand(qx, qy, p3x, p3y));
                }
                return;
            }

            // t=0.5에서 드 카스텔조 분할
            // 1차: A = (P0+C1)/2, B = (C1+C2)/2, C = (C2+P3)/2
            double ax = (p0x + c1x) / 2.0;
            double ay = (p0y + c1y) / 2.0;
            double bx = (c1x + c2x) / 2.0;
            double by = (c1y + c2y) / 2.0;
            double cx = (c2x + p3x) / 2.0;
            double cy = (c2y + p3y) / 2.0;

            // 2차: D = (A+B)/2, E = (B+C)/2
            double dx = (ax + bx) / 2.0;
            double dy = (ay + by) / 2.0;
            double ex = (bx + cx) / 2.0;
            double ey = (by + cy) / 2.0;

            // 3차: M = (D+E)/2 — t=0.5 곡선 위의 점
            double mx = (dx + ex) / 2.0;
            double my = (dy + ey) / 2.0;

            // 각 반 3차에 대해 재귀 처리
            AppendQuadratic(result, p0x, p0y, ax, ay, dx, dy, mx, my);
            AppendQuadratic(result, mx, my, ex, ey, cx, cy, p3x, p3y);
        }

        /// <summary>
        /// 두 접선의 교차점을 계산합니다.
        /// 직선1: (p0x,p0y) + s*(r1x,r1y), 직선2: (q0x,q0y) + t*(r2x,r2y)
        /// 교차점이 직선1 시작점 기준 올바른 방향(s &gt; 0)에 있을 때만 true를 반환합니다.
        /// </summary>
        private static bool IntersectTangents(
            double p0x, double p0y, double r1x, double r1y,
            double q0x, double q0y, double r2x, double r2y,
            out double ix, out double iy)
        {
            ix = 0;
            iy = 0;
            double denom = r1x * r2y - r1y * r2x;
            if (Math.Abs(denom) < 1e-12)
                return false; // 평행

            double s = ((q0x - p0x) * r2y - (q0y - p0y) * r2x) / denom;
            if (s <= 0)
                return false; // 교차점이 잘못된 방향(루프 발생)

            ix = p0x + s * r1x;
            iy = p0y + s * r1y;
            return true;
        }
    }
}
