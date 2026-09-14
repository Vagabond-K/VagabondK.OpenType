namespace VagabondK.OpenType.CFF
{
    /// <summary>
    /// CFF DICT operator입니다.
    /// Top DICT와 Private DICT가 공유하는 operator 번호 공간입니다.
    /// </summary>
    enum DictOperator
    {
        // 1바이트 operator (0-21, 12 제외)
        /// <summary>version입니다. (SID) 예: "1.0". 대부분의 렌더러는 "1.0"을 기대합니다.</summary>
        Version = 0,
        /// <summary>Notice입니다. (SID) 애플리케이션이 표시할 고지 문자열.</summary>
        Notice = 1,
        /// <summary>FullName입니다. (SID) PostScript에서 폰트 식별에 사용됩니다.</summary>
        FullName = 2,
        /// <summary>FamilyName입니다. (SID) 같은 family의 여러 스타일을 묶는 데 사용됩니다.</summary>
        FamilyName = 3,
        /// <summary>Weight입니다. (SID) 굵기를 나타내는 이름. 예: "Regular", "Bold"</summary>
        Weight = 4,
        /// <summary>FontBBox입니다. (array: xMin, yMin, xMax, yMax) 렌더러가 레이아웃과 클리핑에 사용합니다.</summary>
        FontBBox = 5,
        /// <summary>BlueValues입니다. (array, delta) x-height/cap-height 등 수평 정렬 y 좌표.</summary>
        BlueValues = 6,
        /// <summary>OtherBlues입니다. (array, delta) descender/underscore 등 기타 정렬 y 좌표.</summary>
        OtherBlues = 7,
        /// <summary>FamilyBlues입니다. (array, delta) family 공유 BlueValues.</summary>
        FamilyBlues = 8,
        /// <summary>FamilyOtherBlues입니다. (array, delta) family 공유 OtherBlues.</summary>
        FamilyOtherBlues = 9,
        /// <summary>StdHW(Standard Horizontal Width)입니다. (number) charstring에 width가 명시되지 않을 때 사용됩니다.</summary>
        StdHW = 10,
        /// <summary>StdVW(Standard Vertical Width)입니다. (number) charstring에 width가 명시되지 않을 때 사용됩니다.</summary>
        StdVW = 11,
        /// <summary>UniqueID입니다. (number) 이 폰트를 고유하게 식별하는 정수입니다.</summary>
        UniqueID = 13,
        /// <summary>XUID입니다. (array: 1개 또는 4개 정수) UniqueID를 보완하는 확장 식별자입니다.</summary>
        XUID = 14,
        /// <summary>Charset입니다. (offset) glyph ID → SID 매핑 구조의 오프셋.</summary>
        Charset = 15,
        /// <summary>Encoding입니다. (offset/array) 문자 코드 → glyph ID 매핑.</summary>
        Encoding = 16,
        /// <summary>CharStrings입니다. (offset) charstring INDEX의 오프셋.</summary>
        CharStrings = 17,
        /// <summary>Private입니다. (size, offset) Private DICT의 크기와 오프셋.</summary>
        Private = 18,
        /// <summary>Subrs입니다. (offset) Private Subrs INDEX의 오프셋 (Private DICT 기준 상대값).</summary>
        Subrs = 19,
        /// <summary>DefaultWidthX입니다. (number) width 미명시 시 기본 수평 advance width.</summary>
        DefaultWidthX = 20,
        /// <summary>NominalWidthX입니다. (number) width 기록 생략 기준 width.</summary>
        NominalWidthX = 21,
        /// <summary>VsIndex입니다. (number) 수직 스템 힌팅 인덱스.</summary>
        VsIndex = 22,
        /// <summary>MaxStack입니다. (number) charstring 검증 시 최대 스택 깊이.</summary>
        MaxStack = 25,

        // 2바이트 operator (0x0C + 두 번째 바이트, enum 값에 (b0 << 8) | b1 패킹)
        /// <summary>Copyright입니다. (SID) 저작권 고지 문자열.</summary>
        Copyright = 0x0C00,
        /// <summary>IsFixedPitch입니다. (number) 0=비례, 0이 아님=고정폭.</summary>
        IsFixedPitch = 0x0C01,
        /// <summary>ItalicAngle입니다. (number) 수직선 기준 반시계 방향 기울임 각도(도).</summary>
        ItalicAngle = 0x0C02,
        /// <summary>UnderlinePosition입니다. (number) 밑줄 윗부분 y 좌표 (일반적으로 음수).</summary>
        UnderlinePosition = 0x0C03,
        /// <summary>UnderlineThickness입니다. (number) post 테이블의 underlineThickness와 일치시키는 것이 좋습니다.</summary>
        UnderlineThickness = 0x0C04,
        /// <summary>PaintType입니다. (number) 0=채움, 1=윤곽 렌더링.</summary>
        PaintType = 0x0C05,
        /// <summary>CharstringType입니다. (number) 0=simple, 1=standard Type 2.</summary>
        CharstringType = 0x0C06,
        /// <summary>FontMatrix입니다. (array: xx, xy, yx, yy, x, y) 폰트 단위 → text space 변환 행렬.</summary>
        FontMatrix = 0x0C07,
        /// <summary>StrokeWidth입니다. (number) PaintType=1(윤곽) 시 선 두께.</summary>
        StrokeWidth = 0x0C08,
        /// <summary>BlueScale입니다. (number) blue zone 스냅 강도 (0~1).</summary>
        BlueScale = 0x0C09,
        /// <summary>BlueShift입니다. (number) blue zone 높이(두께).</summary>
        BlueShift = 0x0C0A,
        /// <summary>BlueFuzz입니다. (number) blue zone 스냅 허용 최대 거리.</summary>
        BlueFuzz = 0x0C0B,
        /// <summary>StemSnapH입니다. (array, delta) 수평 스템 허용 두께 목록.</summary>
        StemSnapH = 0x0C0C,
        /// <summary>StemSnapV입니다. (array, delta) 수직 스템 허용 두께 목록.</summary>
        StemSnapV = 0x0C0D,
        /// <summary>ForceBold입니다. (number) 0이 아니면 스템을 최소 1픽셀로 강제.</summary>
        ForceBold = 0x0C0E,
        /// <summary>LanguageGroup입니다. (number) 스템 힌팅 언어 그룹 (0=Latin, 5=Korean Level 1 등).</summary>
        LanguageGroup = 0x0C11,
        /// <summary>ExpansionFactor입니다. (number) 스템 확장 최대 비율.</summary>
        ExpansionFactor = 0x0C12,
        /// <summary>InitialRandomSeed입니다. (number) 스템 힌팅 결정적 난수 시드.</summary>
        InitialRandomSeed = 0x0C13,
        /// <summary>SyntheticBase입니다. (number) 합성 bold/italic의 기반 폰트 인덱스.</summary>
        SyntheticBase = 0x0C14,
        /// <summary>PostScript입니다. (SID) 폰트 로드 시 실행되는 PostScript 프로그램.</summary>
        PostScript = 0x0C15,
        /// <summary>BaseFontName입니다. (SID) 합성 폰트의 기반 폰트 이름.</summary>
        BaseFontName = 0x0C16,
        /// <summary>BaseFontBlend입니다. (array) CID 블렌딩 가중치.</summary>
        BaseFontBlend = 0x0C17,
        /// <summary>ROS입니다. (array: Registry, Ordering, Supplement) CID 폰트 식별자.</summary>
        ROS = 0x0C1E,
        /// <summary>CIDFontVersion입니다. (SID) CID 폰트 버전.</summary>
        CIDFontVersion = 0x0C1F,
        /// <summary>CIDFontRevision입니다. (number) CID 폰트 리비전.</summary>
        CIDFontRevision = 0x0C20,
        /// <summary>CIDFontType입니다. (number) CID 폰트 타입.</summary>
        CIDFontType = 0x0C21,
        /// <summary>CIDCount입니다. (number) CID 개수.</summary>
        CIDCount = 0x0C22,
        /// <summary>UIDBase입니다. (number) CID 폰트 UID 기준값.</summary>
        UIDBase = 0x0C23,
        /// <summary>FDArray입니다. (offset) CID 폰트 FD(Font Directory) 배열 오프셋.</summary>
        FDArray = 0x0C24,
        /// <summary>FDSelect입니다. (offset) CID → FD 매핑 오프셋.</summary>
        FDSelect = 0x0C25,
        /// <summary>FontName입니다. (SID) PostScript에서 폰트를 참조할 때 사용됩니다.</summary>
        FontName = 0x0C26
    }
}
