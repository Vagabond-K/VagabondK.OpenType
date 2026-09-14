namespace VagabondK.OpenType
{
    /// <summary>
    /// 폰트의 수직 metrics를 한 번에 정의하는 값 객체입니다.
    /// <see cref="FontBuilder.Metrics"/>에 설정하면 hhea/OS/2의 metrics 필드에 적용됩니다.
    /// </summary>
    public class FontMetrics
    {
        /// <summary>
        /// hhea ascender / OS/2 sTypoAscender에 cascade되는 값입니다.
        /// null이면 750을 사용합니다.
        /// </summary>
        public int? Ascender { get; set; }

        /// <summary>
        /// hhea descender / OS/2 sTypoDescender에 cascade되는 값입니다.
        /// null이면 -250을 사용합니다.
        /// </summary>
        public int? Descender { get; set; }

        /// <summary>
        /// hhea lineGap / OS/2 sTypoLineGap에 cascade되는 값입니다.
        /// null이면 0을 사용합니다.
        /// </summary>
        public int? LineGap { get; set; }

        /// <summary>
        /// OS/2 usWinAscent에 cascade되는 값입니다.
        /// null이면 Ascender(또는 750)를 사용합니다.
        /// </summary>
        public int? WinAscent { get; set; }

        /// <summary>
        /// OS/2 usWinDescent에 cascade되는 값입니다.
        /// null이면 -Descender(또는 250)를 사용합니다.
        /// </summary>
        public int? WinDescent { get; set; }
    }
}
