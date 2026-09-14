using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VagabondK.OpenType.CFF;
using VagabondK.OpenType.Geometry;
using VagabondK.OpenType.Tables;

namespace VagabondK.OpenType
{
    /// <summary>
    /// 폰트를 생성하는 빌더입니다. 옵션을 플루언트 메서드로 설정한 후
    /// <see cref="Build{T}"/>를 호출하면 폰트 객체를 반환합니다.
    /// <see cref="Build{T}"/>의 형식 매개변수로 TTF(<see cref="TtfFont"/>) 또는
    /// OTF(<see cref="OtfFont"/>)를 지정합니다.
    /// </summary>
    /// <remarks>
    /// metrics/italic/fixedPitch/caretSlope는 <see cref="Build{T}"/> 시
    /// hhea/OS/2/post/head(CFF Top DICT 포함)에 일관되게 cascade됩니다.
    /// 비트 플래그(fsSelection/macStyle)는 Italic/Regular 비트만 설정하고,
    /// 나머지 비트(UseTypoMetrics, Bold 등)는 보존합니다.
    /// </remarks>
    public class FontBuilder
    {
        // cascade 옵션 (Build 시 테이블에 적용)
        private FontMetrics metrics;
        private double italicAngle;
        private bool fixedPitch;
        private CaretSlope caretSlope;

        // head
        private int? unitsPerEm;

        // OS/2
        private Weight? weightClass;
        private int? xAvgCharWidth;
        private int? breakChar;
        private bool useTypoMetrics;

        // name (기본 언어)
        private string copyright;
        private string family;
        // subfamily(nameID 2)는 Windows 폰트 뷰어가 요구하는 필수 name이다.
        // 미설정 시 "Regular" 기본값을 사용한다.
        private string subfamily = "Regular";
        private string uniqueId;
        private string version;
        private string trademark;
        private string manufacturer;
        private string designer;
        private string description;
        private string license;

        // name (추가 언어)
        private readonly List<LanguageEntry> languages = new List<LanguageEntry>();

        // glyphs (문자 코드 → 글리프)
        private readonly List<KeyValuePair<int, Glyph>> glyphs = new List<KeyValuePair<int, Glyph>>();

        private struct LanguageEntry
        {
            public int LanguageId { get; }
            public Action<NameLanguage> Configure { get; }

            public LanguageEntry(int languageId, Action<NameLanguage> configure)
            {
                LanguageId = languageId;
                Configure = configure;
            }
        }

        /// <summary>head unitsPerEm을 설정합니다.</summary>
        public FontBuilder UnitsPerEm(int unitsPerEm)
        {
            this.unitsPerEm = unitsPerEm;
            return this;
        }

        /// <summary>OS/2 usWeightClass를 설정합니다.</summary>
        public FontBuilder WeightClass(Weight weightClass)
        {
            this.weightClass = weightClass;
            return this;
        }

        /// <summary>OS/2 usXAvgCharWidth를 설정합니다.</summary>
        public FontBuilder XAvgCharWidth(int xAvgCharWidth)
        {
            this.xAvgCharWidth = xAvgCharWidth;
            return this;
        }

        /// <summary>OS/2 usBreakChar를 설정합니다.</summary>
        public FontBuilder BreakChar(int charCode)
        {
            this.breakChar = charCode;
            return this;
        }

        /// <summary>OS/2 fsSelection의 UseTypoMetrics 비트를 설정합니다.</summary>
        public FontBuilder UseTypoMetrics()
        {
            useTypoMetrics = true;
            return this;
        }

        /// <summary>name copyright(nameID 0)를 설정합니다.</summary>
        public FontBuilder Copyright(string copyright)
        {
            this.copyright = copyright;
            return this;
        }

        /// <summary>name family(nameID 1)를 설정합니다.</summary>
        public FontBuilder Family(string family)
        {
            this.family = family;
            return this;
        }

        /// <summary>name subfamily(nameID 2)를 설정합니다. null이면 "Regular" 기본값을 사용합니다.</summary>
        public FontBuilder Subfamily(string subfamily)
        {
            this.subfamily = subfamily ?? "Regular";
            return this;
        }

        /// <summary>name uniqueId(nameID 3)를 설정합니다.</summary>
        public FontBuilder UniqueId(string uniqueId)
        {
            this.uniqueId = uniqueId;
            return this;
        }

        /// <summary>name version(nameID 5)를 설정합니다.</summary>
        public FontBuilder Version(string version)
        {
            this.version = version;
            return this;
        }

        /// <summary>name trademark(nameID 7)를 설정합니다.</summary>
        public FontBuilder Trademark(string trademark)
        {
            this.trademark = trademark;
            return this;
        }

        /// <summary>name manufacturer(nameID 8)를 설정합니다.</summary>
        public FontBuilder Manufacturer(string manufacturer)
        {
            this.manufacturer = manufacturer;
            return this;
        }

        /// <summary>name designer(nameID 9)를 설정합니다.</summary>
        public FontBuilder Designer(string designer)
        {
            this.designer = designer;
            return this;
        }

        /// <summary>name description(nameID 10)를 설정합니다.</summary>
        public FontBuilder Description(string description)
        {
            this.description = description;
            return this;
        }

        /// <summary>name license(nameID 13)를 설정합니다.</summary>
        public FontBuilder License(string license)
        {
            this.license = license;
            return this;
        }

        /// <summary>
        /// 추가 언어 항목을 등록합니다.
        /// 기본 언어 값을 상속받은 후 <paramref name="configure"/>로 특정 필드만 수정합니다.
        /// </summary>
        /// <param name="languageId">languageID입니다. Windows LCID를 사용합니다. (예: 0x0412=ko-KR)</param>
        /// <param name="configure">추가 언어 항목을 수정하는 콜백입니다.</param>
        public FontBuilder AddLanguage(int languageId, Action<NameLanguage> configure)
        {
            languages.Add(new LanguageEntry(languageId, configure));
            return this;
        }

        /// <summary>글리프를 추가합니다.</summary>
        /// <param name="charCode">문자 코드입니다. (예: '1', 0x20, 0x1F600)</param>
        /// <param name="glyph">글리프입니다.</param>
        /// <exception cref="ArgumentException">문자 코드에 이미 글리프가 정의된 경우.</exception>
        /// <exception cref="ArgumentOutOfRangeException">문자 코드가 0x0000–0x10FFFF 범위를 벗어난 경우.</exception>
        public FontBuilder AddGlyph(int charCode, Glyph glyph)
        {
            if (charCode < 0 || charCode > 0x10FFFF)
                throw new ArgumentOutOfRangeException(nameof(charCode), $"The character code U+{charCode:X8} is out of range (0x0000-0x10FFFF).");
            if (glyphs.Any(g => g.Key == charCode))
                throw new ArgumentException($"The glyph for character U+{charCode:X4} is already defined.", nameof(charCode));
            glyphs.Add(new KeyValuePair<int, Glyph>(charCode, glyph));
            return this;
        }

        /// <summary>글리프를 일괄 추가합니다.</summary>
        /// <param name="glyphs">문자 코드와 글리프의 쌍 목록입니다. (Dictionary&lt;int, Glyph&gt;, List&lt;KeyValuePair&lt;int, Glyph&gt;&gt; 등)</param>
        /// <exception cref="ArgumentException">문자 코드에 이미 글리프가 정의된 경우.</exception>
        public FontBuilder AddGlyphs(IEnumerable<KeyValuePair<int, Glyph>> glyphs)
        {
            foreach (var g in glyphs)
                AddGlyph(g.Key, g.Value);
            return this;
        }

        /// <summary>수직 metrics를 설정합니다. hhea/OS/2에 cascade됩니다.</summary>
        public FontBuilder Metrics(FontMetrics metrics)
        {
            this.metrics = metrics;
            return this;
        }

        /// <summary>
        /// 기울기 각도(도)를 설정합니다. 0이면 수직, 우측 기울임은 음수입니다.
        /// post italicAngle, head macStyle, OS/2 fsSelection, hhea caretSlope, CFF ItalicAngle에 cascade됩니다.
        /// </summary>
        public FontBuilder ItalicAngle(double italicAngle)
        {
            this.italicAngle = italicAngle;
            return this;
        }

        /// <summary>고정 피치(모노스페이스) 여부를 설정합니다. post isFixedPitch, CFF IsFixedPitch에 cascade됩니다.</summary>
        public FontBuilder FixedPitch(bool fixedPitch)
        {
            this.fixedPitch = fixedPitch;
            return this;
        }

        /// <summary>
        /// caret 기울기(rise/run)를 설정합니다. null이면 ItalicAngle에서 계산합니다.
        /// hhea caretSlopeRise/caretSlopeRun에 cascade됩니다.
        /// </summary>
        public FontBuilder CaretSlope(CaretSlope caretSlope)
        {
            this.caretSlope = caretSlope;
            return this;
        }

        /// <summary>
        /// 설정된 옵션으로 폰트 객체를 생성하고 반환합니다.
        /// </summary>
        /// <typeparam name="T">생성할 폰트 타입입니다. <see cref="TtfFont"/> 또는 <see cref="OtfFont"/>.</typeparam>
        public T Build<T>() where T : FontBase, new()
        {
            var font = new T();

            // head
            if (unitsPerEm.HasValue) font.Head.UnitsPerEm = unitsPerEm.Value;

            // OS/2
            if (weightClass.HasValue) font.Os2.WeightClass = weightClass.Value;
            if (xAvgCharWidth.HasValue) font.Os2.XAvgCharWidth = xAvgCharWidth.Value;
            if (breakChar.HasValue) font.Os2.BreakChar = breakChar.Value;
            if (useTypoMetrics) font.Os2.FsSelection |= FontSelection.UseTypoMetrics;

            // name (기본 언어)
            if (copyright != null) font.Name.Copyright = copyright;
            if (family != null) font.Name.Family = family;
            if (subfamily != null) font.Name.Subfamily = subfamily;
            if (uniqueId != null) font.Name.UniqueId = uniqueId;
            if (version != null) font.Name.Version = version;
            if (trademark != null) font.Name.Trademark = trademark;
            if (manufacturer != null) font.Name.Manufacturer = manufacturer;
            if (designer != null) font.Name.Designer = designer;
            if (description != null) font.Name.Description = description;
            if (license != null) font.Name.License = license;

            // name (추가 언어) — 기본 언어 설정 이후에 추가해야 상속이 정확합니다.
            foreach (var lang in languages)
            {
                var nameLanguage = font.Name.AddLanguage(lang.LanguageId);
                lang.Configure(nameLanguage);
            }

            // glyphs
            foreach (var g in glyphs)
                font.AddGlyph(g.Key, g.Value);

            // cascade: metrics/italic/fixedPitch/caretSlope → 테이블
            ApplyCascade(font);

            return font;
        }

        /// <summary>
        /// 설정된 옵션으로 폰트 객체를 생성하고 스트림에 저장합니다.
        /// <see cref="Build{T}"/> 후 <see cref="FontBase.Save(Stream)"/>을 호출하는 것과 동일합니다.
        /// 스트림의 소유권은 호출자에게 있으며, 이 메서드는 스트림을 닫지 않습니다.
        /// </summary>
        /// <typeparam name="T">생성할 폰트 타입입니다. <see cref="TtfFont"/> 또는 <see cref="OtfFont"/>.</typeparam>
        /// <param name="stream">기록할 스트림입니다.</param>
        public void Save<T>(Stream stream) where T : FontBase, new() => Build<T>().Save(stream);

        /// <summary>
        /// 설정된 옵션으로 폰트 객체를 생성하고 파일로 저장합니다.
        /// <see cref="Build{T}"/> 후 <see cref="FontBase.Save(string)"/>을 호출하는 것과 동일합니다.
        /// </summary>
        /// <typeparam name="T">생성할 폰트 타입입니다. <see cref="TtfFont"/> 또는 <see cref="OtfFont"/>.</typeparam>
        /// <param name="path">파일 경로입니다.</param>
        public void Save<T>(string path) where T : FontBase, new() => Build<T>().Save(path);

        /// <summary>
        /// 설정된 옵션으로 폰트 객체를 생성하고 파일로 저장합니다.
        /// 파일 확장자로 폰트 타입을 결정합니다: .otf → <see cref="OtfFont"/>, .ttf → <see cref="TtfFont"/>.
        /// </summary>
        /// <param name="path">파일 경로입니다. 확장자가 .otf 또는 .ttf여야 합니다.</param>
        public void Save(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            string extension = Path.GetExtension(path);
            if (string.Equals(extension, ".otf", StringComparison.OrdinalIgnoreCase))
                Save<OtfFont>(path);
            else if (string.Equals(extension, ".ttf", StringComparison.OrdinalIgnoreCase))
                Save<TtfFont>(path);
            else
                throw new ArgumentException("The file extension must be .otf or .ttf.", nameof(path));
        }

        /// <summary>
        /// metrics/italic/fixedPitch/caretSlope를 테이블에 cascade합니다.
        /// </summary>
        private void ApplyCascade(FontBase font)
        {
            // metrics: hhea/OS/2
            if (metrics != null)
            {
                int ascender = metrics.Ascender ?? 750;
                int descender = metrics.Descender ?? -250;
                int lineGap = metrics.LineGap ?? 0;
                int winAscent = metrics.WinAscent ?? ascender;
                int winDescent = metrics.WinDescent ?? -descender;

                font.Hhea.Ascender = ascender;
                font.Hhea.Descender = descender;
                font.Hhea.LineGap = lineGap;
                font.Os2.Ascender = ascender;
                font.Os2.Descender = descender;
                font.Os2.TypoLineGap = lineGap;
                font.Os2.WinAscent = winAscent;
                font.Os2.WinDescent = winDescent;
            }

            // italic: post/head/OS/2/hhea
            font.Post.ItalicAngle = italicAngle;
            font.Head.MacStyle = SetItalicBit(font.Head.MacStyle, italicAngle != 0);
            font.Os2.FsSelection = SetItalicBit(font.Os2.FsSelection, italicAngle != 0);
            font.Hhea.CaretSlopeRise = caretSlope?.Rise ?? 10000;
            font.Hhea.CaretSlopeRun = caretSlope?.Run ?? (int)Math.Round(Math.Tan(-italicAngle * Math.PI / 180) * 10000);

            // fixed pitch: post
            font.Post.IsFixedPitch = fixedPitch;

            // CFF TopDict (OTF만)
            if (font is OtfFont otf)
            {
                otf.CFF.TopDict.FullName = NameTable.JoinNonEmpty(font.Name.Family, font.Name.Subfamily, " ");
                otf.CFF.TopDict.FamilyName = font.Name.Family;
                otf.CFF.TopDict.Weight = (CffWeight)font.Os2.WeightClass; // 두 enum의 값이 일치(100~900). Normal(400)→Regular(400)
                otf.CFF.TopDict.Version = font.Name.Version?.Replace("Version ", "");
                otf.CFF.TopDict.IsFixedPitch = fixedPitch ? 1 : 0;
                otf.CFF.TopDict.ItalicAngle = italicAngle != 0 ? italicAngle : null;
            }
        }

        /// <summary>
        /// macStyle의 Italic 비트를 설정하고, 나머지 비트는 보존합니다.
        /// </summary>
        private static MacStyleFlag SetItalicBit(MacStyleFlag flags, bool italic)
        {
            flags &= ~MacStyleFlag.Italic;
            if (italic) flags |= MacStyleFlag.Italic;
            return flags;
        }

        /// <summary>
        /// fsSelection의 Italic/Regular 비트를 설정하고, 나머지 비트는 보존합니다.
        /// Italic(bit 0)과 Regular(bit 6)은 상호 배타적입니다.
        /// </summary>
        private static FontSelection SetItalicBit(FontSelection flags, bool italic)
        {
            flags &= ~(FontSelection.Italic | FontSelection.Regular);
            flags |= italic ? FontSelection.Italic : FontSelection.Regular;
            return flags;
        }
    }
}
