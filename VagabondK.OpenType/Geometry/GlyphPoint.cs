namespace VagabondK.OpenType.Geometry
{
    /// <summary>
    /// 글리프 윤곽선의 단일 점을 나타냅니다.
    /// 좌표는 폰트 단위(font unit)로 표현되며 소수가 허용됩니다.
    /// TTF(glyf)는 정수 전용이라 직렬화 시 반올림되고, OTF(CFF)는 16.16 fixed point로 저장됩니다.
    /// </summary>
    public class GlyphPoint
    {
        /// <summary>
        /// X 좌표(폰트 단위)입니다. 소수가 허용되며,
        /// pen 기원점 기준 절대 좌표입니다. Y 양수 방향은 위쪽입니다.
        /// </summary>
        public double X { get; }

        /// <summary>
        /// Y 좌표(폰트 단위)입니다. 소수가 허용되며,
        /// baseline이 y=0이고 양수 방향은 위쪽입니다.
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// 점이 곡선 위(on-curve)인지 여부를 반환합니다.
        /// <c>false</c>이면 2차 베지어 곡선의 제어점(off-curve)입니다.
        /// glyf의 flags 비트 0(ON_CURVE_POINT)에 해당합니다.
        /// </summary>
        public bool IsOnCurve { get; }

        /// <summary>
        /// 좌표와 on-curve 여부를 지정하여 <see cref="GlyphPoint"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="x">X 좌표(폰트 단위)입니다. 소수가 허용됩니다.</param>
        /// <param name="y">Y 좌표(폰트 단위)입니다. 소수가 허용됩니다.</param>
        /// <param name="isOnCurve">곡선 위 점 여부입니다. <c>false</c>이면 제어점입니다.</param>
        public GlyphPoint(double x, double y, bool isOnCurve)
        {
            X = x;
            Y = y;
            IsOnCurve = isOnCurve;
        }
    }
}
