using System;
using System.Collections.Generic;
using VagabondK.OpenType;

namespace VagabondK.OpenType.Tables
{
    /// <summary>
    /// OS/2 테이블입니다. version 4를 사용합니다.
    /// OpenType 폰트에 필요한 metrics와 데이터를 제공합니다.
    /// version 4 이상을 사용하는 것이 강력히 권장됩니다.
    /// </summary>
    public class Os2Table : OpenTypeTable
    {
        /// <summary>
        /// sTypoAscender입니다.
        /// sTypoDescender와 sTypoLineGap과 함께 기본 줄 간격을 결정하며,
        /// fsSelection의 USE_TYPO_METRICS 비트가 설정되어 있을 때 사용됩니다.
        /// </summary>
        public int Ascender { get; set; } = 750;

        /// <summary>
        /// sTypoDescender입니다. 일반적으로 음수입니다.
        /// </summary>
        public int Descender { get; set; } = -250;

        /// <summary>
        /// xAvgCharWidth입니다. 0이 아닌 너비의 모든 글리프의 escapement(너비) 산술 평균입니다.
        /// 텍스트 줄 레이아웃 계산에 이 값을 의존하지 않는 것이 강력히 권장됩니다.
        /// </summary>
        public int XAvgCharWidth { get; set; }

        /// <summary>
        /// usWeightClass입니다. 글리프의 시각적 굵기를 나타냅니다.
        /// OS가 폰트 family 내 굵기 변형을 선택하는 데 사용합니다.
        /// </summary>
        public Weight WeightClass { get; set; } = Weight.Normal;

        /// <summary>
        /// usWidthClass입니다. 글리프의 정상 종횡비에 대한 상대적 변화를 나타냅니다.
        /// OS가 폰트 family 내 폭 변형을 선택하는 데 사용합니다.
        /// </summary>
        public Width WidthClass { get; set; } = Width.Medium;

        /// <summary>
        /// fsType입니다. 폰트 임베딩 라이선스 권한 플래그입니다.
        /// </summary>
        public FontType FsType { get; set; }

        /// <summary>
        /// ySubscriptXSize입니다. 하첨자(subscript)의 권장 수평 크기입니다. 0보다 커야 합니다.
        /// </summary>
        public int YSubscriptXSize { get; set; } = 700;

        /// <summary>
        /// ySubscriptYSize입니다. 하첨자의 권장 수직 크기입니다. 0보다 커야 합니다.
        /// </summary>
        public int YSubscriptYSize { get; set; } = 700;

        /// <summary>
        /// ySubscriptXOffset입니다. 하첨자의 권장 수평 오프셋입니다.
        /// 글리프 기원점에서 하첨자 글리프 기원점까지의 거리입니다.
        /// </summary>
        public int YSubscriptXOffset { get; set; }

        /// <summary>
        /// ySubscriptYOffset입니다. 하첨자의 baseline 기준 권장 수직 오프셋입니다.
        /// baseline 아래로 내려가는 값은 양수로 표현됩니다.
        /// </summary>
        public int YSubscriptYOffset { get; set; } = 143;

        /// <summary>
        /// ySuperscriptXSize입니다. 상첨자(superscript)의 권장 수평 크기입니다. 0보다 커야 합니다.
        /// </summary>
        public int YSuperscriptXSize { get; set; } = 700;

        /// <summary>
        /// ySuperscriptYSize입니다. 상첨자의 권장 수직 크기입니다. 0보다 커야 합니다.
        /// </summary>
        public int YSuperscriptYSize { get; set; } = 700;

        /// <summary>
        /// ySuperscriptXOffset입니다. 상첨자의 권장 수평 오프셋입니다.
        /// </summary>
        public int YSuperscriptXOffset { get; set; }

        /// <summary>
        /// ySuperscriptYOffset입니다. 상첨자의 baseline 기준 권장 수직 오프셋입니다.
        /// baseline 위로 올라가는 값은 양수로 표현됩니다.
        /// </summary>
        public int YSuperscriptYOffset { get; set; } = 306;

        /// <summary>
        /// yStrikeoutSize입니다. 취소선(strikeout) 선의 두께입니다. 0보다 커야 합니다.
        /// 일반적으로 em dash의 두께와 post의 underlineThickness와 일치해야 합니다.
        /// </summary>
        public int YStrikeoutSize { get; set; } = 49;

        /// <summary>
        /// yStrikeoutPosition입니다. 취소선 윗부분의 baseline 기준 위치입니다.
        /// 양수는 baseline 위, 음수는 baseline 아래를 나타냅니다.
        /// </summary>
        public int YStrikeoutPosition { get; set; } = 98;

        /// <summary>
        /// sFamilyClass입니다. 폰트 family의 디자인 분류입니다.
        /// 상위 바이트는 family class, 하위 바이트는 family subclass입니다.
        /// 요청한 폰트가 없을 때 대체 family를 선택하는 데 사용됩니다.
        /// </summary>
        public int FamilyClass { get; set; }

        /// <summary>
        /// panose입니다. 서체의 시각적 특성 분류입니다.
        /// </summary>
        public PanoseProperties Panose { get; set; } = new PanoseProperties(2, 0, 5, 0, 0, 0, 0, 0, 0, 0);

        /// <summary>
        /// achVendID입니다. 서체의 벤더를 식별하는 4자 태그입니다.
        /// 서체의 마케팅과 유통을 담당하는 회사의 식별자입니다.
        /// </summary>
        public string VendorId { get; set; } = "VAGB";

        /// <summary>
        /// fsSelection입니다. 폰트 스타일 선택 플래그입니다.
        /// </summary>
        public FontSelection FsSelection { get; set; } = FontSelection.Regular;

        /// <summary>
        /// usFirstCharIndex입니다. cmap(platform 3, encoding 0 또는 1)의 최소 유니코드 인덱스입니다.
        /// 글리프에서 자동 계산됩니다.
        /// </summary>
        internal int UsFirstCharIndex { get; set; }

        /// <summary>
        /// usLastCharIndex입니다. cmap(platform 3, encoding 0 또는 1)의 최대 유니코드 인덱스입니다.
        /// 글리프에서 자동 계산됩니다.
        /// </summary>
        internal int UsLastCharIndex { get; set; }

        /// <summary>
        /// ulUnicodeRange1입니다. 폰트가 지원하는 Unicode 블록 범위(bit 0-31)입니다.
        /// cmap에 매핑된 문자 코드에서 자동 계산됩니다.
        /// </summary>
        internal uint UnicodeRange1 { get; set; }

        /// <summary>
        /// ulUnicodeRange2입니다. 폰트가 지원하는 Unicode 블록 범위(bit 32-63)입니다.
        /// cmap에 매핑된 문자 코드에서 자동 계산됩니다.
        /// </summary>
        internal uint UnicodeRange2 { get; set; }

        /// <summary>
        /// ulUnicodeRange3입니다. 폰트가 지원하는 Unicode 블록 범위(bit 64-95)입니다.
        /// cmap에 매핑된 문자 코드에서 자동 계산됩니다.
        /// </summary>
        internal uint UnicodeRange3 { get; set; }

        /// <summary>
        /// ulUnicodeRange4입니다. 폰트가 지원하는 Unicode 블록 범위(bit 96-127)입니다.
        /// cmap에 매핑된 문자 코드에서 자동 계산됩니다.
        /// </summary>
        internal uint UnicodeRange4 { get; set; }

        /// <summary>
        /// sTypoLineGap입니다.
        /// sTypoAscender와 sTypoDescender와 함께 기본 줄 간격을 결정합니다.
        /// </summary>
        public int TypoLineGap { get; set; }

        /// <summary>
        /// usWinAscent입니다.
        /// baseline 위 클리핑 영역의 높이를 지정하는 데 사용됩니다.
        /// 글리프가 잘리지 않도록 yMax 이상으로 설정하는 것이 좋습니다.
        /// 0이면 Windows 폰트 뷰어에서 열리지 않으므로 1 이상이어야 합니다.
        /// </summary>
        public int WinAscent
        {
            get => winAscent;
            set
            {
                if (value <= 0)
                    throw new ArgumentException($"usWinAscent must be greater than 0 (got {value}).", nameof(value));
                winAscent = value;
            }
        }
        private int winAscent = 750;

        /// <summary>
        /// usWinDescent입니다.
        /// baseline 아래 클리핑 영역의 범위를 지정하는 데 사용됩니다.
        /// baseline 아래 거리를 양수로 표현하며, -yMin 이상으로 설정하는 것이 좋습니다.
        /// 0이면 Windows 폰트 뷰어에서 열리지 않으므로 1 이상이어야 합니다.
        /// </summary>
        public int WinDescent
        {
            get => winDescent;
            set
            {
                if (value <= 0)
                    throw new ArgumentException($"usWinDescent must be greater than 0 (got {value}).", nameof(value));
                winDescent = value;
            }
        }
        private int winDescent = 250;

        /// <summary>
        /// ulCodePageRange1입니다. 폰트가 지원하는 code page 범위(bit 0-31)입니다.
        /// bit 0(1252, Latin 1)이 기본으로 설정됩니다.
        /// </summary>
        public uint CodePageRange1 { get; set; } = 0x00000001;

        /// <summary>
        /// ulCodePageRange2입니다. 폰트가 지원하는 code page 범위(bit 32-63)입니다.
        /// </summary>
        public uint CodePageRange2 { get; set; }

        /// <summary>
        /// sxHeight입니다. baseline과 소문자(상승하지 않는 글자)의 대략적인 높이 사이의 거리입니다.
        /// 폰트 대체 시 x-height를 기준으로 크기 근사에 사용할 수 있습니다.
        /// </summary>
        public int XHeight { get; set; } = 500;

        /// <summary>
        /// sCapHeight입니다. baseline과 대문자의 대략적인 높이 사이의 거리입니다.
        /// 대문자 높이를 밀리미터로 지정하는 시스템이나 정렬 metrics로 사용할 수 있습니다.
        /// </summary>
        public int CapHeight { get; set; } = 700;

        /// <summary>
        /// usDefaultChar입니다. 요청한 문자를 폰트가 지원하지 않을 때 기본 글리프로
        /// 사용할 문자의 유니코드 코드포인트입니다. 0이면 glyph ID 0(.notdef)을 사용합니다.
        /// </summary>
        public int DefaultChar { get; set; }

        /// <summary>
        /// usBreakChar입니다. 단어 구분과 정렬(justify)에 사용되는 기본 break 문자의
        /// 유니코드 코드포인트입니다. 대부분의 폰트는 U+0020 SPACE를 사용합니다.
        /// </summary>
        public int BreakChar { get; set; }

        /// <summary>
        /// usMaxContext입니다. 이 폰트의 모든 feature 중 최대 target 글리프 컨텍스트 길이입니다.
        /// 예를 들어 pair kerning만 있는 폰트는 2, "ffi" 리가처가 있으면 3으로 설정합니다.
        /// </summary>
        public int MaxContext { get; set; }

        /// <inheritdoc />
        internal override string Tag => "OS/2";

        /// <inheritdoc />
        internal override byte[] ToBytes()
        {
            var writer = new OpenTypeBinaryWriter();
            writer.WriteUInt16(4); // version
            writer.WriteInt16((short)XAvgCharWidth); // xAvgCharWidth
            writer.WriteUInt16((ushort)(int)WeightClass); // usWeightClass
            writer.WriteUInt16((ushort)(int)WidthClass); // usWidthClass
            writer.WriteUInt16((ushort)(int)FsType); // fsType
            writer.WriteInt16((short)YSubscriptXSize); // ySubscriptXSize
            writer.WriteInt16((short)YSubscriptYSize); // ySubscriptYSize
            writer.WriteInt16((short)YSubscriptXOffset); // ySubscriptXOffset
            writer.WriteInt16((short)YSubscriptYOffset); // ySubscriptYOffset
            writer.WriteInt16((short)YSuperscriptXSize); // ySuperscriptXSize
            writer.WriteInt16((short)YSuperscriptYSize); // ySuperscriptYSize
            writer.WriteInt16((short)YSuperscriptXOffset); // ySuperscriptXOffset
            writer.WriteInt16((short)YSuperscriptYOffset); // ySuperscriptYOffset
            writer.WriteInt16((short)YStrikeoutSize); // yStrikeoutSize
            writer.WriteInt16((short)YStrikeoutPosition); // yStrikeoutPosition
            writer.WriteInt16((short)FamilyClass); // sFamilyClass
            writer.WriteUInt8((byte)Panose.FamilyType); // panose: bFamilyType
            writer.WriteUInt8((byte)Panose.SerifStyle); // panose: bSerifStyle
            writer.WriteUInt8((byte)Panose.Weight); // panose: bWeight
            writer.WriteUInt8((byte)Panose.Proportion); // panose: bProportion
            writer.WriteUInt8((byte)Panose.Contrast); // panose: bContrast
            writer.WriteUInt8((byte)Panose.StrokeVariation); // panose: bStrokeVariation
            writer.WriteUInt8((byte)Panose.ArmStyle); // panose: bArmStyle
            writer.WriteUInt8((byte)Panose.Letterform); // panose: bLetterform
            writer.WriteUInt8((byte)Panose.Midline); // panose: bMidline
            writer.WriteUInt8((byte)Panose.XHeight); // panose: bXHeight
            writer.WriteUInt32(UnicodeRange1); // ulUnicodeRange1
            writer.WriteUInt32(UnicodeRange2); // ulUnicodeRange2
            writer.WriteUInt32(UnicodeRange3); // ulUnicodeRange3
            writer.WriteUInt32(UnicodeRange4); // ulUnicodeRange4
            writer.WriteTag(VendorId); // achVendID
            writer.WriteUInt16((ushort)(int)FsSelection); // fsSelection
            writer.WriteUInt16((ushort)UsFirstCharIndex);
            writer.WriteUInt16((ushort)UsLastCharIndex);
            writer.WriteInt16((short)Ascender); // sTypoAscender
            writer.WriteInt16((short)Descender); // sTypoDescender
            writer.WriteInt16((short)TypoLineGap); // sTypoLineGap
            writer.WriteUInt16((ushort)WinAscent); // usWinAscent
            writer.WriteUInt16((ushort)WinDescent); // usWinDescent
            writer.WriteUInt32(CodePageRange1); // ulCodePageRange1
            writer.WriteUInt32(CodePageRange2); // ulCodePageRange2
            writer.WriteInt16((short)XHeight); // sxHeight
            writer.WriteInt16((short)CapHeight); // sCapHeight
            writer.WriteUInt16((ushort)DefaultChar); // usDefaultChar
            writer.WriteUInt16((ushort)BreakChar); // usBreakChar
            writer.WriteUInt16((ushort)MaxContext); // usMaxContext
            return writer.ToArray();
        }

        /// <summary>
        /// OS/2 테이블 바이트를 파싱하여 <see cref="Os2Table"/>을 생성합니다. <see cref="ToBytes"/>의 역입니다.
        /// </summary>
        /// <param name="reader">테이블 데이터를 읽는 리더입니다. (테이블 시작 위치)</param>
        internal static Os2Table FromBytes(OpenTypeBinaryReader reader)
        {
            var table = new Os2Table();
            reader.ReadUInt16(); // version
            table.XAvgCharWidth = reader.ReadInt16();
            table.WeightClass = (Weight)reader.ReadUInt16();
            table.WidthClass = (Width)reader.ReadUInt16();
            table.FsType = (FontType)reader.ReadUInt16();
            table.YSubscriptXSize = reader.ReadInt16();
            table.YSubscriptYSize = reader.ReadInt16();
            table.YSubscriptXOffset = reader.ReadInt16();
            table.YSubscriptYOffset = reader.ReadInt16();
            table.YSuperscriptXSize = reader.ReadInt16();
            table.YSuperscriptYSize = reader.ReadInt16();
            table.YSuperscriptXOffset = reader.ReadInt16();
            table.YSuperscriptYOffset = reader.ReadInt16();
            table.YStrikeoutSize = reader.ReadInt16();
            table.YStrikeoutPosition = reader.ReadInt16();
            table.FamilyClass = reader.ReadInt16();
            table.Panose = new PanoseProperties(
                reader.ReadUInt8(), reader.ReadUInt8(), reader.ReadUInt8(), reader.ReadUInt8(),
                reader.ReadUInt8(), reader.ReadUInt8(), reader.ReadUInt8(), reader.ReadUInt8(),
                reader.ReadUInt8(), reader.ReadUInt8());
            table.UnicodeRange1 = reader.ReadUInt32();
            table.UnicodeRange2 = reader.ReadUInt32();
            table.UnicodeRange3 = reader.ReadUInt32();
            table.UnicodeRange4 = reader.ReadUInt32();
            table.VendorId = reader.ReadTag();
            table.FsSelection = (FontSelection)reader.ReadUInt16();
            table.UsFirstCharIndex = reader.ReadUInt16();
            table.UsLastCharIndex = reader.ReadUInt16();
            table.Ascender = reader.ReadInt16();
            table.Descender = reader.ReadInt16();
            table.TypoLineGap = reader.ReadInt16();
            table.WinAscent = reader.ReadUInt16();
            table.WinDescent = reader.ReadUInt16();
            table.CodePageRange1 = reader.ReadUInt32();
            table.CodePageRange2 = reader.ReadUInt32();
            table.XHeight = reader.ReadInt16();
            table.CapHeight = reader.ReadInt16();
            table.DefaultChar = reader.ReadUInt16();
            table.BreakChar = reader.ReadUInt16();
            table.MaxContext = reader.ReadUInt16();
            return table;
        }

        /// <summary>
        /// ulUnicodeRange 비트에 해당하는 Unicode 블록 범위입니다.
        /// </summary>
        private class UnicodeRangeBlock
        {
            public int Bit;
            public int Start;
            public int End;

            public UnicodeRangeBlock(int bit, int start, int end)
            {
                Bit = bit;
                Start = start;
                End = end;
            }
        }

        private static readonly UnicodeRangeBlock[] UnicodeRangeBlocks =
        {
            new UnicodeRangeBlock(0, 0x0000, 0x007F),   // Basic Latin
            new UnicodeRangeBlock(1, 0x0080, 0x00FF),   // Latin-1 Supplement
            new UnicodeRangeBlock(2, 0x0100, 0x017F),   // Latin Extended-A
            new UnicodeRangeBlock(3, 0x0180, 0x024F),   // Latin Extended-B
            new UnicodeRangeBlock(4, 0x0250, 0x02AF),   // IPA Extensions
            new UnicodeRangeBlock(4, 0x1D00, 0x1D7F),   // Phonetic Extensions
            new UnicodeRangeBlock(4, 0x1D80, 0x1DBF),   // Phonetic Extensions Supplement
            new UnicodeRangeBlock(5, 0x02B0, 0x02FF),   // Spacing Modifier Letters
            new UnicodeRangeBlock(5, 0xA700, 0xA71F),   // Modifier Tone Letters
            new UnicodeRangeBlock(6, 0x0300, 0x036F),   // Combining Diacritical Marks
            new UnicodeRangeBlock(6, 0x1DC0, 0x1DFF),   // Combining Diacritical Marks Supplement
            new UnicodeRangeBlock(7, 0x0370, 0x03FF),   // Greek and Coptic
            new UnicodeRangeBlock(8, 0x2C80, 0x2CFF),   // Coptic
            new UnicodeRangeBlock(9, 0x0400, 0x04FF),   // Cyrillic
            new UnicodeRangeBlock(9, 0x0500, 0x052F),   // Cyrillic Supplement
            new UnicodeRangeBlock(9, 0x2DE0, 0x2DFF),   // Cyrillic Extended-A
            new UnicodeRangeBlock(9, 0xA640, 0xA69F),   // Cyrillic Extended-B
            new UnicodeRangeBlock(10, 0x0530, 0x058F),  // Armenian
            new UnicodeRangeBlock(11, 0x0590, 0x05FF),  // Hebrew
            new UnicodeRangeBlock(12, 0xA500, 0xA63F),  // Vai
            new UnicodeRangeBlock(13, 0x0600, 0x06FF),  // Arabic
            new UnicodeRangeBlock(13, 0x0750, 0x077F),  // Arabic Supplement
            new UnicodeRangeBlock(14, 0x07C0, 0x07FF),  // NKo
            new UnicodeRangeBlock(15, 0x0900, 0x097F),  // Devanagari
            new UnicodeRangeBlock(16, 0x0980, 0x09FF),  // Bengali
            new UnicodeRangeBlock(17, 0x0A00, 0x0A7F),  // Gurmukhi
            new UnicodeRangeBlock(18, 0x0A80, 0x0AFF),  // Gujarati
            new UnicodeRangeBlock(19, 0x0B00, 0x0B7F),  // Oriya
            new UnicodeRangeBlock(20, 0x0B80, 0x0BFF),  // Tamil
            new UnicodeRangeBlock(21, 0x0C00, 0x0C7F),  // Telugu
            new UnicodeRangeBlock(22, 0x0C80, 0x0CFF),  // Kannada
            new UnicodeRangeBlock(23, 0x0D00, 0x0D7F),  // Malayalam
            new UnicodeRangeBlock(24, 0x0E00, 0x0E7F),  // Thai
            new UnicodeRangeBlock(25, 0x0E80, 0x0EFF),  // Lao
            new UnicodeRangeBlock(26, 0x10A0, 0x10FF),  // Georgian
            new UnicodeRangeBlock(26, 0x2D00, 0x2D2F),  // Georgian Supplement
            new UnicodeRangeBlock(27, 0x1B00, 0x1B7F),  // Balinese
            new UnicodeRangeBlock(28, 0x1100, 0x11FF),  // Hangul Jamo
            new UnicodeRangeBlock(29, 0x1E00, 0x1EFF),  // Latin Extended Additional
            new UnicodeRangeBlock(29, 0x2C60, 0x2C7F),  // Latin Extended-C
            new UnicodeRangeBlock(29, 0xA720, 0xA7FF),  // Latin Extended-D
            new UnicodeRangeBlock(30, 0x1F00, 0x1FFF),  // Greek Extended
            new UnicodeRangeBlock(31, 0x2000, 0x206F),  // General Punctuation
            new UnicodeRangeBlock(31, 0x2E00, 0x2E7F),  // Supplemental Punctuation
            new UnicodeRangeBlock(32, 0x2070, 0x209F),  // Superscripts And Subscripts
            new UnicodeRangeBlock(33, 0x20A0, 0x20CF),  // Currency Symbols
            new UnicodeRangeBlock(34, 0x20D0, 0x20FF),  // Combining Diacritical Marks For Symbols
            new UnicodeRangeBlock(35, 0x2100, 0x214F),  // Letterlike Symbols
            new UnicodeRangeBlock(36, 0x2150, 0x218F),  // Number Forms
            new UnicodeRangeBlock(37, 0x2190, 0x21FF),  // Arrows
            new UnicodeRangeBlock(37, 0x27F0, 0x27FF),  // Supplemental Arrows-A
            new UnicodeRangeBlock(37, 0x2900, 0x297F),  // Supplemental Arrows-B
            new UnicodeRangeBlock(38, 0x2200, 0x22FF),  // Mathematical Operators
            new UnicodeRangeBlock(38, 0x2A00, 0x2AFF),  // Supplemental Mathematical Operators
            new UnicodeRangeBlock(39, 0x2300, 0x23FF),  // Miscellaneous Technical
            new UnicodeRangeBlock(40, 0x2400, 0x243F),  // Control Pictures
            new UnicodeRangeBlock(41, 0x2440, 0x245F),  // Optical Character Recognition
            new UnicodeRangeBlock(42, 0x2460, 0x24FF),  // Enclosed Alphanumerics
            new UnicodeRangeBlock(43, 0x2500, 0x257F),  // Box Drawing
            new UnicodeRangeBlock(44, 0x2580, 0x259F),  // Block Elements
            new UnicodeRangeBlock(45, 0x25A0, 0x25FF),  // Geometric Shapes
            new UnicodeRangeBlock(46, 0x2600, 0x26FF),  // Miscellaneous Symbols
            new UnicodeRangeBlock(46, 0x2B00, 0x2BFF),  // Miscellaneous Symbols and Arrows
            new UnicodeRangeBlock(47, 0x2700, 0x27BF),  // Dingbats
            new UnicodeRangeBlock(47, 0x27C0, 0x27EF),  // Miscellaneous Mathematical Symbols-A
            new UnicodeRangeBlock(47, 0x2980, 0x29FF),  // Miscellaneous Mathematical Symbols-B
            new UnicodeRangeBlock(48, 0x3000, 0x303F),  // CJK Symbols And Punctuation
            new UnicodeRangeBlock(48, 0x2E80, 0x2EFF),  // CJK Radicals Supplement
            new UnicodeRangeBlock(48, 0x2F00, 0x2FDF),  // Kangxi Radicals
            new UnicodeRangeBlock(48, 0x2FF0, 0x2FFF),  // Ideographic Description Characters
            new UnicodeRangeBlock(49, 0x3040, 0x309F),  // Hiragana
            new UnicodeRangeBlock(50, 0x30A0, 0x30FF),  // Katakana
            new UnicodeRangeBlock(50, 0x31F0, 0x31FF),  // Katakana Phonetic Extensions
            new UnicodeRangeBlock(51, 0x3100, 0x312F),  // Bopomofo
            new UnicodeRangeBlock(51, 0x31A0, 0x31BF),  // Bopomofo Extended
            new UnicodeRangeBlock(52, 0x3130, 0x318F),  // Hangul Compatibility Jamo
            new UnicodeRangeBlock(53, 0xA840, 0xA87F),  // Phags-pa
            new UnicodeRangeBlock(54, 0x3200, 0x32FF),  // Enclosed CJK Letters And Months
            new UnicodeRangeBlock(55, 0x3300, 0x33FF),  // CJK Compatibility
            new UnicodeRangeBlock(55, 0xF900, 0xFAFF),  // CJK Compatibility Ideographs
            new UnicodeRangeBlock(55, 0xFE30, 0xFE4F),  // CJK Compatibility Forms
            new UnicodeRangeBlock(56, 0xAC00, 0xD7AF),  // Hangul Syllables
            new UnicodeRangeBlock(59, 0x4E00, 0x9FFF),  // CJK Unified Ideographs
            new UnicodeRangeBlock(59, 0x3400, 0x4DBF),  // CJK Unified Ideographs Extension A
            new UnicodeRangeBlock(60, 0xE000, 0xF8FF),  // Private Use Area (plane 0)
            new UnicodeRangeBlock(61, 0x31C0, 0x31EF),  // CJK Strokes
            new UnicodeRangeBlock(61, 0x3190, 0x319F),  // Kanbun
            new UnicodeRangeBlock(62, 0xFB00, 0xFB4F),  // Alphabetic Presentation Forms
            new UnicodeRangeBlock(63, 0xFB50, 0xFDFF),  // Arabic Presentation Forms-A
            new UnicodeRangeBlock(64, 0xFE20, 0xFE2F),  // Combining Half Marks
            new UnicodeRangeBlock(65, 0xFE10, 0xFE1F),  // Vertical Forms
            new UnicodeRangeBlock(66, 0xFE50, 0xFE6F),  // Small Form Variants
            new UnicodeRangeBlock(67, 0xFE70, 0xFEFF),  // Arabic Presentation Forms-B
            new UnicodeRangeBlock(68, 0xFF00, 0xFFEF),  // Halfwidth And Fullwidth Forms
            new UnicodeRangeBlock(69, 0xFFF0, 0xFFFF),  // Specials
            new UnicodeRangeBlock(70, 0x0F00, 0x0FFF),  // Tibetan
            new UnicodeRangeBlock(71, 0x0700, 0x074F),  // Syriac
            new UnicodeRangeBlock(72, 0x0780, 0x07BF),  // Thaana
            new UnicodeRangeBlock(73, 0x0D80, 0x0DFF),  // Sinhala
            new UnicodeRangeBlock(74, 0x1000, 0x109F),  // Myanmar
            new UnicodeRangeBlock(75, 0x1200, 0x137F),  // Ethiopic
            new UnicodeRangeBlock(75, 0x1380, 0x139F),  // Ethiopic Supplement
            new UnicodeRangeBlock(75, 0x2D80, 0x2DDF),  // Ethiopic Extended
            new UnicodeRangeBlock(76, 0x13A0, 0x13FF),  // Cherokee
            new UnicodeRangeBlock(77, 0x1400, 0x167F),  // Unified Canadian Aboriginal Syllabics
            new UnicodeRangeBlock(78, 0x1680, 0x169F),  // Ogham
            new UnicodeRangeBlock(79, 0x16A0, 0x16FF),  // Runic
            new UnicodeRangeBlock(80, 0x1780, 0x17FF),  // Khmer
            new UnicodeRangeBlock(80, 0x19E0, 0x19FF),  // Khmer Symbols
            new UnicodeRangeBlock(81, 0x1800, 0x18AF),  // Mongolian
            new UnicodeRangeBlock(82, 0x2800, 0x28FF),  // Braille Patterns
            new UnicodeRangeBlock(83, 0xA000, 0xA48F),  // Yi Syllables
            new UnicodeRangeBlock(83, 0xA490, 0xA4CF),  // Yi Radicals
            new UnicodeRangeBlock(84, 0x1700, 0x171F),  // Tagalog
            new UnicodeRangeBlock(84, 0x1720, 0x173F),  // Hanunoo
            new UnicodeRangeBlock(84, 0x1740, 0x175F),  // Buhid
            new UnicodeRangeBlock(84, 0x1760, 0x177F),  // Tagbanwa
            new UnicodeRangeBlock(91, 0xFE00, 0xFE0F),  // Variation Selectors
            new UnicodeRangeBlock(93, 0x1900, 0x194F),  // Limbu
            new UnicodeRangeBlock(94, 0x1950, 0x197F),  // Tai Le
            new UnicodeRangeBlock(95, 0x1980, 0x19DF),  // New Tai Lue
            new UnicodeRangeBlock(96, 0x1A00, 0x1A1F),  // Buginese
            new UnicodeRangeBlock(97, 0x2C00, 0x2C5F),  // Glagolitic
            new UnicodeRangeBlock(98, 0x2D30, 0x2D7F),  // Tifinagh
            new UnicodeRangeBlock(99, 0x4DC0, 0x4DFF),  // Yijing Hexagram Symbols
            new UnicodeRangeBlock(100, 0xA800, 0xA82F), // Syloti Nagri
            new UnicodeRangeBlock(112, 0x1B80, 0x1BBF), // Sundanese
            new UnicodeRangeBlock(113, 0x1C00, 0x1C4F), // Lepcha
            new UnicodeRangeBlock(114, 0x1C50, 0x1C7F), // Ol Chiki
            new UnicodeRangeBlock(115, 0xA880, 0xA8DF), // Saurashtra
            new UnicodeRangeBlock(116, 0xA900, 0xA92F), // Kayah Li
            new UnicodeRangeBlock(117, 0xA930, 0xA95F), // Rejang
            new UnicodeRangeBlock(118, 0xAA00, 0xAA5F), // Cham
        };

        /// <summary>
        /// 문자 코드 목록에 해당하는 ulUnicodeRange 비트를 계산합니다.
        /// cmap에 매핑된 문자 코드가 포함된 Unicode 블록의 비트를 설정합니다.
        /// </summary>
        /// <param name="charCodes">cmap에 매핑된 유니코드 코드포인트 목록입니다.</param>
        /// <returns>ulUnicodeRange1~4 값 배열입니다. (4개 요소)</returns>
        internal static uint[] ComputeUnicodeRanges(IReadOnlyList<int> charCodes)
        {
            uint range1 = 0, range2 = 0, range3 = 0, range4 = 0;
            foreach (int code in charCodes)
            {
                foreach (var block in UnicodeRangeBlocks)
                {
                    if (code >= block.Start && code <= block.End)
                    {
                        if (block.Bit < 32) range1 |= 1u << block.Bit;
                        else if (block.Bit < 64) range2 |= 1u << (block.Bit - 32);
                        else if (block.Bit < 96) range3 |= 1u << (block.Bit - 64);
                        else range4 |= 1u << (block.Bit - 96);
                        break;
                    }
                }
            }
            return new uint[] { range1, range2, range3, range4 };
        }
    }
}
