namespace VagabondK.OpenType
{
    /// <summary>
    /// hhea caretSlopeRise / caretSlopeRun 값을 한 쌍으로 정의하는 값 객체입니다.
    /// <see cref="FontBuilder.CaretSlope"/>에 설정하면 hhea의 caretSlope 필드에 적용됩니다.
    /// </summary>
    public class CaretSlope
    {
        /// <summary>
        /// hhea caretSlopeRise에 cascade되는 값입니다.
        /// </summary>
        public int Rise { get; set; }

        /// <summary>
        /// hhea caretSlopeRun에 cascade되는 값입니다.
        /// </summary>
        public int Run { get; set; }
    }
}
