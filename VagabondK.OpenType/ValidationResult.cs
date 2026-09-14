namespace VagabondK.OpenType
{
    /// <summary>
    /// <see cref="FontBase.Validate"/> 결과의 심각도입니다.
    /// </summary>
    public enum ValidationLevel
    {
        /// <summary>
        /// 경고입니다. 폰트는 생성되지만, 렌더링이나 유효성 검사에서 문제가 될 수 있습니다.
        /// </summary>
        Warning,

        /// <summary>
        /// 오류입니다. 폰트가 의도대로 렌더링되지 않거나 유효하지 않을 가능성이 높습니다.
        /// </summary>
        Error
    }

    /// <summary>
    /// <see cref="FontBase.Validate"/>의 단일 검사 결과입니다.
    /// </summary>
    public struct ValidationResult
    {
        /// <summary>
        /// 심각도입니다.
        /// </summary>
        public ValidationLevel Level { get; }

        /// <summary>
        /// 검사 결과 메시지입니다. (영어)
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// <see cref="ValidationResult"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="level">심각도입니다.</param>
        /// <param name="message">검사 결과 메시지입니다.</param>
        public ValidationResult(ValidationLevel level, string message)
        {
            Level = level;
            Message = message;
        }
    }
}
