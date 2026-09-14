namespace VagabondK.OpenType.Tables
{
    /// <summary>
    /// OpenType 테이블의 공통 기반 클래스입니다.
    /// 모든 테이블은 4자 태그와 바이너리 직렬화 기능을 공유합니다.
    /// sfnt 파일의 테이블 디렉터리는 각 테이블의 태그, checksum, 오프셋, 길이를 저장합니다.
    /// </summary>
    public abstract class OpenTypeTable
    {
        /// <summary>
        /// 테이블 태그(4자)를 반환합니다. (예: "head", "glyf")
        /// 테이블 디렉터리에서 태그의 ASCII 순서로 정렬됩니다.
        /// </summary>
        internal abstract string Tag { get; }

        /// <summary>
        /// 테이블을 바이너리 데이터로 직렬화합니다.
        /// </summary>
        /// <returns>테이블 바이트 배열입니다.</returns>
        internal abstract byte[] ToBytes();
    }
}
