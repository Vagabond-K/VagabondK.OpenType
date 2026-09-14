using VagabondK.OpenType.Geometry;

namespace VagabondK.OpenType.CFF
{
    /// <summary>
    /// CFF Top DICT 옵션입니다.
    /// Top DICT는 CFF 테이블 최상위 레벨의 딕셔너리로, 폰트 전체에 대한 정보
    /// (이름, 메트릭, 하위 구조 오프셋 등)를 담습니다.
    /// null인 항목은 Top DICT에 포함하지 않습니다.
    /// </summary>
    public class TopDict
    {
        /// <summary>
        /// version입니다. (op 0, SID) CFF 형식 버전 문자열로, 대부분의 렌더러는 "1.0"을 기대합니다. 예: "1.0"
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Notice입니다. (op 1, SID) 일부 애플리케이션(예: 폰트 관리자)이 표시하는 고지 문자열입니다.
        /// </summary>
        public string Notice { get; set; }

        /// <summary>
        /// FullName입니다. (op 2, SID) PostScript 애플리케이션에서 폰트를 식별하는 데 사용되는 전체 이름입니다. 예: "VagabondK Regular"
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// FamilyName입니다. (op 3, SID) 같은 family의 여러 스타일을 묶는 데 사용됩니다. 예: "VagabondK"
        /// </summary>
        public string FamilyName { get; set; }

        /// <summary>
        /// Weight입니다. (op 4, SID) 굵기를 나타내는 이름입니다. CFF 표준 weight 이름(Thin~Black)을 사용합니다.
        /// </summary>
        public CffWeight? Weight { get; set; }

        /// <summary>
        /// Copyright입니다. (op 12 0, SID) 저작권 고지 문자열입니다.
        /// </summary>
        public string Copyright { get; set; }

        /// <summary>
        /// PostScript입니다. (op 12 21, SID) 폰트 로드 시 실행되는 PostScript 프로그램입니다.
        /// </summary>
        public string PostScript { get; set; }

        /// <summary>
        /// BaseFontName입니다. (op 12 22, SID) 합성(synthetic) 폰트의 경우 기반이 되는 폰트의 이름입니다.
        /// </summary>
        public string BaseFontName { get; set; }

        /// <summary>
        /// FontName입니다. (op 12 38, SID) PostScript에서 폰트를 참조할 때 사용하는 이름입니다.
        /// </summary>
        public string FontName { get; set; }

        /// <summary>
        /// IsFixedPitch입니다. (op 12 1) 0이면 비례(proportional) 폰트, 0이 아니면 고정폭(monospaced) 폰트입니다.
        /// </summary>
        public int? IsFixedPitch { get; set; }

        /// <summary>
        /// ItalicAngle입니다. (op 12 2) 수직선 기준 반시계 방향 기울임 각도(도)입니다.
        /// </summary>
        public double? ItalicAngle { get; set; }

        /// <summary>
        /// UnderlinePosition입니다. (op 12 3) 밑줄 윗부분의 y 좌표로, baseline 아래로 내려가는 값이므로 일반적으로 음수입니다.
        /// </summary>
        public int? UnderlinePosition { get; set; }

        /// <summary>
        /// UnderlineThickness입니다. (op 12 4) 밑줄 두께입니다. post 테이블의 underlineThickness와 일치시키는 것이 좋습니다.
        /// </summary>
        public int? UnderlineThickness { get; set; }

        /// <summary>
        /// PaintType입니다. (op 12 5) 0이면 채움(fill), 1이면 윤곽(stroke) 렌더링입니다.
        /// </summary>
        public int? PaintType { get; set; }

        /// <summary>
        /// CharstringType입니다. (op 12 6) 0이면 simple, 1이면 standard Type 2 charstring입니다.
        /// </summary>
        public int? CharstringType { get; set; }

        /// <summary>
        /// FontMatrix [xx, xy, yx, yy, x, y]입니다. (op 12 7) 폰트 단위를 text space로 변환하는 affine 행렬로,
        /// 기본값은 [0.001 0 0 0.001 0 0]입니다.
        /// </summary>
        public FontMatrix? FontMatrix { get; set; }

        /// <summary>
        /// StrokeWidth입니다. (op 12 8) PaintType이 1(윤곽)일 때 사용하는 선 두께입니다.
        /// </summary>
        public double? StrokeWidth { get; set; }

        /// <summary>
        /// UniqueID입니다. (op 13) 이 폰트를 고유하게 식별하는 정수 ID입니다.
        /// </summary>
        public int? UniqueID { get; set; }

        /// <summary>
        /// FontBBox [xMin, yMin, xMax, yMax]입니다. (op 5) 폰트 전체의 바운딩 박스로, 렌더러가 레이아웃과 클리핑에 사용합니다.
        /// </summary>
        internal double[] FontBBox { get; set; }

        /// <summary>
        /// XUID입니다. (op 14) 폰트를 고유하게 식별하는 확장 ID로, 1개 또는 4개의 정수 배열입니다.
        /// </summary>
        public int[] XUID { get; set; }

        /// <summary>
        /// SyntheticBase입니다. (op 12 20) 합성 bold/italic의 경우 기반 폰트의 인덱스입니다.
        /// </summary>
        public int? SyntheticBase { get; set; }

        /// <summary>
        /// MaxStack입니다. (op 25) charstring 검증 시 허용하는 최대 스택 깊이입니다.
        /// </summary>
        public int? MaxStack { get; set; }
    }
}
