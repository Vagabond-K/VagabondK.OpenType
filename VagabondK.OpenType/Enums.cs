using System;

namespace VagabondK.OpenType
{
    /// <summary>
    /// head 테이블의 flags 필드의 비트 플래그입니다.
    /// </summary>
    [Flags]
    public enum HeadFlag
    {
        /// <summary>
        /// bit 0: baseline이 y=0에 있습니다.
        /// </summary>
        BaselineAtY0 = 0x0001,

        /// <summary>
        /// bit 1: left sidebearing point가 x=0에 있습니다 (TrueType rasterizer에만 관련).
        /// 설정 시 모든 글리프의 pp1 = 0이며, 각 글리프의 xMin과 left side bearing이 같아야 합니다.
        /// </summary>
        LeftSidebearingAtX0 = 0x0002,

        /// <summary>
        /// bit 2: instructions가 point size에 의존할 수 있습니다.
        /// </summary>
        InstructionsMayDependOnPointSize = 0x0004,

        /// <summary>
        /// bit 3: 모든 내부 scaler 계산에서 ppem을 정수 값으로 강제합니다.
        /// hinted 폰트에는 설정하는 것이 강력히 권장됩니다.
        /// </summary>
        ForceIntegerPPem = 0x0008,

        /// <summary>
        /// bit 4: instructions가 advance width를 변경할 수 있습니다 (advance width가 선형적으로 스케일되지 않을 수 있음).
        /// </summary>
        InstructionsMayAlterAdvanceWidth = 0x0010,

        /// <summary>
        /// bit 11: 최적화 변환 또는 압축을 거쳤음에도 원본 폰트의 기능과 특성이 유지되는 lossless 상태입니다.
        /// </summary>
        Lossless = 0x0800,

        /// <summary>
        /// bit 12: 변환된 폰트입니다 (호환되는 metrics 생성).
        /// </summary>
        Converted = 0x1000,

        /// <summary>
        /// bit 13: ClearType®에 최적화된 폰트입니다.
        /// 임베디드 비트맵(EBDT)에 의존하는 폰트는 이 비트를 설정하지 않아야 합니다.
        /// </summary>
        OptimizedForClearType = 0x2000,

        /// <summary>
        /// bit 14: last resort 폰트입니다.
        /// cmap 서브테이블의 글리프가 코드포인트 범위의 일반적인 심볼 표현일 뿐,
        /// 해당 코드포인트를 실제로 지원하지 않음을 나타냅니다.
        /// </summary>
        LastResort = 0x4000
    }

    /// <summary>
    /// head 테이블의 macStyle 필드의 비트 플래그입니다.
    /// Windows에서는 OS/2 테이블의 fsSelection 비트가 우선 사용되며,
    /// macStyle 비트는 fsSelection 비트와 일치해야 합니다.
    /// </summary>
    [Flags]
    public enum MacStyleFlag
    {
        /// <summary>
        /// 플래그가 설정되지 않은 상태입니다.
        /// </summary>
        None = 0x0000,

        /// <summary>
        /// bit 0: Bold입니다.
        /// </summary>
        Bold = 0x0001,

        /// <summary>
        /// bit 1: Italic입니다.
        /// </summary>
        Italic = 0x0002,

        /// <summary>
        /// bit 2: Underline입니다.
        /// </summary>
        Underline = 0x0004,

        /// <summary>
        /// bit 3: Outline(중공) 글리프입니다.
        /// </summary>
        Outline = 0x0008,

        /// <summary>
        /// bit 4: Shadow입니다.
        /// </summary>
        Shadow = 0x0010,

        /// <summary>
        /// bit 5: Condensed(좁은) 글리프입니다.
        /// </summary>
        Condensed = 0x0020,

        /// <summary>
        /// bit 6: Extended(넓은) 글리프입니다.
        /// </summary>
        Extended = 0x0040
    }

    /// <summary>
    /// head 테이블의 fontDirectionHint 필드의 값입니다.
    /// 더 이상 사용되지 않으며(deprecated), 2로 설정하는 것이 권장됩니다.
    /// </summary>
    public enum DirectionHint
    {
        /// <summary>
        /// 방향성 글리프가 완전히 혼합된 경우.
        /// </summary>
        FullyMixed = 0,

        /// <summary>
        /// strongly left to right 글리프만 포함하는 경우.
        /// </summary>
        StronglyLeftToRight = 1,

        /// <summary>
        /// strongly left to right 글리프와 neutral(공백, 구두점 등)을 포함하는 경우.
        /// </summary>
        LeftToRightWithNeutrals = 2,

        /// <summary>
        /// strongly right to left 글리프만 포함하는 경우.
        /// </summary>
        StronglyRightToLeft = -1,

        /// <summary>
        /// strongly right to left 글리프와 neutral을 포함하는 경우.
        /// </summary>
        RightToLeftWithNeutrals = -2
    }

    /// <summary>
    /// OS/2 테이블의 usWeightClass입니다. 글리프의 시각적 굵기(선 두께)를 나타냅니다.
    /// 1 이상 1000 이하의 값이 유효합니다.
    /// </summary>
    public enum Weight
    {
        /// <summary>
        /// Thin(100).
        /// </summary>
        Thin = 100,

        /// <summary>
        /// Extra-light(200).
        /// </summary>
        ExtraLight = 200,

        /// <summary>
        /// Light(300).
        /// </summary>
        Light = 300,

        /// <summary>
        /// Normal(Regular)(400).
        /// </summary>
        Normal = 400,

        /// <summary>
        /// Medium(500).
        /// </summary>
        Medium = 500,

        /// <summary>
        /// Semi-bold(Demi-bold)(600).
        /// </summary>
        SemiBold = 600,

        /// <summary>
        /// Bold(700).
        /// </summary>
        Bold = 700,

        /// <summary>
        /// Extra-bold(Ultra-bold)(800).
        /// </summary>
        ExtraBold = 800,

        /// <summary>
        /// Black(Heavy)(900).
        /// </summary>
        Black = 900
    }

    /// <summary>
    /// OS/2 테이블의 usWidthClass입니다. 글리프의 정상 종횡비(너비 대 높이 비율)에 대한 상대적 변화를 나타냅니다.
    /// 5(Medium)가 정상(100%)입니다.
    /// </summary>
    public enum Width
    {
        /// <summary>
        /// Ultra-condensed(1, 50%).
        /// </summary>
        UltraCondensed = 1,

        /// <summary>
        /// Extra-condensed(2, 62.5%).
        /// </summary>
        ExtraCondensed = 2,

        /// <summary>
        /// Condensed(3, 75%).
        /// </summary>
        Condensed = 3,

        /// <summary>
        /// Semi-condensed(4, 87.5%).
        /// </summary>
        SemiCondensed = 4,

        /// <summary>
        /// Medium(normal)(5, 100%).
        /// </summary>
        Medium = 5,

        /// <summary>
        /// Semi-expanded(6, 112.5%).
        /// </summary>
        SemiExpanded = 6,

        /// <summary>
        /// Expanded(7, 125%).
        /// </summary>
        Expanded = 7,

        /// <summary>
        /// Extra-expanded(8, 150%).
        /// </summary>
        ExtraExpanded = 8,

        /// <summary>
        /// Ultra-expanded(9, 200%).
        /// </summary>
        UltraExpanded = 9
    }

    /// <summary>
    /// OS/2 테이블의 fsType입니다. 폰트 임베딩 라이선스 권한을 나타냅니다.
    /// bit 0-3(사용 권한)은 상호 배타적이며, 유효한 값은 0, 2, 4, 8 중 하나입니다.
    /// </summary>
    [Flags]
    public enum FontType
    {
        /// <summary>
        /// Installable embedding(0): 폰트를 임베딩하고 원격 시스템에 영구 설치할 수 있습니다.
        /// </summary>
        Installable = 0,

        /// <summary>
        /// Restricted License embedding(2): 법적 소유자의 명시적 허락 없이는 수정, 임베딩, 교환할 수 없습니다.
        /// </summary>
        RestrictedLicense = 2,

        /// <summary>
        /// Preview &amp; Print embedding(4): 문서의 열람 또는 인쇄 목적으로 임시 로드할 수 있습니다. 문서는 읽기 전용으로 열립니다.
        /// </summary>
        PreviewPrint = 4,

        /// <summary>
        /// Editable embedding(8): 임시 로드하고 편집할 수 있습니다.
        /// </summary>
        Editable = 8,

        /// <summary>
        /// No subsetting(0x0100): 임베딩 전에 폰트를 subset으로 만들 수 없습니다.
        /// </summary>
        NoSubsetting = 0x0100,

        /// <summary>
        /// Bitmap embedding only(0x0200): 폰트의 비트맵만 임베딩할 수 있으며, 윤곽 데이터는 임베딩할 수 없습니다.
        /// </summary>
        BitmapEmbeddingOnly = 0x0200
    }

    /// <summary>
    /// OS/2 테이블의 fsSelection입니다. 폰트 패턴의 특성에 대한 정보를 포함합니다.
    /// bit 0(Italic)은 head의 macStyle bit 1과, bit 5(Bold)은 macStyle bit 0과 일치해야 합니다.
    /// </summary>
    [Flags]
    public enum FontSelection
    {
        /// <summary>
        /// bit 0: Italic 또는 oblique 글리프를 포함합니다. 그렇지 않으면 수직(upright)입니다.
        /// </summary>
        Italic = 0x0001,

        /// <summary>
        /// bit 1: 글리프가 밑줄 처리되어 있습니다.
        /// </summary>
        Underscore = 0x0002,

        /// <summary>
        /// bit 2: 글리프의 전경과 배경이 반전되어 있습니다.
        /// </summary>
        Negative = 0x0004,

        /// <summary>
        /// bit 3: Outline(중공) 글리프입니다. 그렇지 않으면 채워진(solid) 글리프입니다.
        /// </summary>
        Outlined = 0x0008,

        /// <summary>
        /// bit 4: 글리프에 취소선이 그어져 있습니다.
        /// </summary>
        Strikeout = 0x0010,

        /// <summary>
        /// bit 5: 글리프가 굵게(emboldened) 처리되어 있습니다.
        /// </summary>
        Bold = 0x0020,

        /// <summary>
        /// bit 6: 글리프가 폰트의 표준 굵기/스타일입니다.
        /// </summary>
        Regular = 0x0040,

        /// <summary>
        /// bit 7: USE_TYPO_METRICS. 설정 시 애플리케이션은 기본 줄 간격으로
        /// sTypoAscender - sTypoDescender + sTypoLineGap을 사용하는 것이 강력히 권장됩니다.
        /// </summary>
        UseTypoMetrics = 0x0080,

        /// <summary>
        /// bit 8: WWS. name 테이블의 family/subfamily 문자열이 weight/width/slope family 모델과
        /// 일치하며 nameID 21/22가 필요 없음을 나타냅니다.
        /// </summary>
        WWS = 0x0100,

        /// <summary>
        /// bit 9: Oblique 글리프를 포함합니다.
        /// </summary>
        Oblique = 0x0200
    }
}
