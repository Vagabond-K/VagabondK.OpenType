namespace VagabondK.OpenType.CFF
{
    /// <summary>
    /// CFF FontMatrix입니다. 2x3 affine 변환 행렬 [xx, xy, yx, yy, x, y]를 나타냅니다.
    /// 폰트 단위를 text space로 변환하는 데 사용됩니다.
    /// Top DICT의 FontMatrix 항목에 기록되며, charstring의 좌표가
    /// 실제 렌더링 공간으로 변환될 때 곱해지는 행렬입니다.
    /// 기본값은 [0.001 0 0 0.001 0 0]이며, unitsPerEm=1000일 때
    /// 폰트 단위를 1/1000 em 단위로 변환합니다.
    /// </summary>
    public struct FontMatrix
    {
        /// <summary>
        /// xx 성분으로, x 축 스케일입니다. 0.001이면 1000 폰트 단위가 1 em입니다.
        /// </summary>
        public double XX { get; }

        /// <summary>
        /// xy 성분으로, x 축의 y 방향 기울임(shear)입니다. 0이 아니면 글리프가 기울어집니다.
        /// </summary>
        public double XY { get; }

        /// <summary>
        /// yx 성분으로, y 축의 x 방향 기울임(shear)입니다.
        /// </summary>
        public double YX { get; }

        /// <summary>
        /// yy 성분으로, y 축 스케일입니다.
        /// </summary>
        public double YY { get; }

        /// <summary>
        /// x 이동(translation) 성분입니다.
        /// </summary>
        public double X { get; }

        /// <summary>
        /// y 이동(translation) 성분입니다.
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// <see cref="FontMatrix"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="xx">xx 성분입니다.</param>
        /// <param name="xy">xy 성분입니다.</param>
        /// <param name="yx">yx 성분입니다.</param>
        /// <param name="yy">yy 성분입니다.</param>
        /// <param name="x">x 이동 성분입니다.</param>
        /// <param name="y">y 이동 성분입니다.</param>
        public FontMatrix(double xx, double xy, double yx, double yy, double x, double y)
        {
            XX = xx;
            XY = xy;
            YX = yx;
            YY = yy;
            X = x;
            Y = y;
        }
    }
}
