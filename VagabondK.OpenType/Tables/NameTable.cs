using System;
using System.Collections.Generic;

namespace VagabondK.OpenType.Tables
{
    /// <summary>
    /// name 테이블(폰트 이름 및 메타데이터)입니다.
    /// 폰트에 다국어 문자열(저작권, family 이름, 스타일 이름 등)을 연관시킵니다.
    /// 각 문자열은 platformID, encodingID, languageID, nameID의 4개 요소 키로 식별됩니다.
    /// 이 구현은 Windows 플랫폼(platformID 3, encodingID 1, UTF-16BE) 문자열을 저장하며,
    /// <see cref="Languages"/> 목록으로 여러 언어를 동시에 포함할 수 있습니다.
    /// 기본 언어(en-US, 0x0409)는 private 멤버로 관리되며 빌드 시 항상 포함됩니다.
    /// </summary>
    public class NameTable : OpenTypeTable
    {
        private class NameRecord
        {
            public int LanguageId;
            public int NameId;
            public string Value;
        }

        private readonly NameLanguage defaultLanguage = new NameLanguage();
        private readonly List<NameLanguage> languages = new List<NameLanguage>();

        /// <summary>
        /// 추가 언어 항목 목록입니다. (기본 언어 제외)
        /// 기본 언어는 private 멤버로 관리되며 빌드 시 항상 포함됩니다.
        /// 아래 편의 속성(Copyright, Family 등)은 기본 언어를 읽기/쓰기합니다.
        /// </summary>
        public List<NameLanguage> Languages => languages;

        /// <summary>
        /// <see cref="NameTable"/>의 새 인스턴스를 생성합니다.
        /// 기본 언어(en-US, 0x0409)는 빌드 시 항상 포함됩니다.
        /// </summary>
        public NameTable()
        {
        }

        /// <summary>
        /// 새 언어 항목을 추가하고 반환합니다.
        /// 새 항목은 기본 언어의 문자열을 복사한 상태로 시작하므로, 다른 부분만 수정하면 됩니다.
        /// </summary>
        /// <param name="languageId">추가할 항목의 languageID입니다. Windows LCID를 사용합니다. (예: 0x0412=ko-KR, 0x0411=ja-JP)</param>
        public NameLanguage AddLanguage(int languageId)
        {
            var source = defaultLanguage;
            var language = new NameLanguage
            {
                LanguageId = languageId,
                Copyright = source.Copyright,
                Family = source.Family,
                Subfamily = source.Subfamily,
                UniqueId = source.UniqueId,
                Version = source.Version,
                Trademark = source.Trademark,
                Manufacturer = source.Manufacturer,
                Designer = source.Designer,
                Description = source.Description,
                License = source.License
            };
            languages.Add(language);
            return language;
        }

        /// <summary>
        /// 기본 언어의 copyright notice(nameID 0)입니다.
        /// </summary>
        public string Copyright { get => defaultLanguage.Copyright; set => defaultLanguage.Copyright = value; }

        /// <summary>
        /// 기본 언어의 Font Family name(nameID 1)입니다.
        /// </summary>
        public string Family { get => defaultLanguage.Family; set => defaultLanguage.Family = value; }

        /// <summary>
        /// 기본 언어의 Font Subfamily name(nameID 2)입니다.
        /// </summary>
        public string Subfamily { get => defaultLanguage.Subfamily; set => defaultLanguage.Subfamily = value; }

        /// <summary>
        /// 기본 언어의 Unique font identifier(nameID 3)입니다.
        /// </summary>
        public string UniqueId { get => defaultLanguage.UniqueId; set => defaultLanguage.UniqueId = value; }

        /// <summary>
        /// 기본 언어의 Version string(nameID 5)입니다.
        /// </summary>
        public string Version { get => defaultLanguage.Version; set => defaultLanguage.Version = value; }

        /// <summary>
        /// 기본 언어의 Trademark(nameID 7)입니다.
        /// </summary>
        public string Trademark { get => defaultLanguage.Trademark; set => defaultLanguage.Trademark = value; }

        /// <summary>
        /// 기본 언어의 Manufacturer Name(nameID 8)입니다.
        /// </summary>
        public string Manufacturer { get => defaultLanguage.Manufacturer; set => defaultLanguage.Manufacturer = value; }

        /// <summary>
        /// 기본 언어의 Designer(nameID 9)입니다.
        /// </summary>
        public string Designer { get => defaultLanguage.Designer; set => defaultLanguage.Designer = value; }

        /// <summary>
        /// 기본 언어의 Description(nameID 10)입니다.
        /// </summary>
        public string Description { get => defaultLanguage.Description; set => defaultLanguage.Description = value; }

        /// <summary>
        /// 기본 언어의 License Description(nameID 13)입니다.
        /// </summary>
        public string License { get => defaultLanguage.License; set => defaultLanguage.License = value; }

        /// <inheritdoc />
        internal override string Tag => "name";

        /// <inheritdoc />
        internal override byte[] ToBytes()
        {
            // GDI 필수 name 검증: family(1), subfamily(2), uniqueId(3)이 없으면 폰트 뷰어에서 열리지 않는다.
            if (string.IsNullOrEmpty(defaultLanguage.Family))
                throw new InvalidOperationException("nameID 1 (Family) is required.");
            if (string.IsNullOrEmpty(defaultLanguage.Subfamily))
                throw new InvalidOperationException("nameID 2 (Subfamily) is required.");
            if (string.IsNullOrEmpty(defaultLanguage.UniqueId))
                throw new InvalidOperationException("nameID 3 (UniqueId) is required.");

            // nameID 4(Full font name), 6(PostScript name), 16(Typographic Family),
            // 17(Typographic Subfamily)은 Family/Subfamily에서 자동 파생됩니다.
            var records = new List<NameRecord>();
            AddLanguageRecords(records, defaultLanguage);
            foreach (var lang in languages)
                AddLanguageRecords(records, lang);

            var stringData = new OpenTypeBinaryWriter();
            var stringOffsets = new int[records.Count];
            for (int i = 0; i < records.Count; i++)
            {
                stringOffsets[i] = stringData.Length;
                stringData.WriteUtf16BigEndian(records[i].Value);
            }

            int count = records.Count;
            int stringOffset = 6 + count * 12;

            var writer = new OpenTypeBinaryWriter();
            writer.WriteUInt16(0); // version (0)
            writer.WriteUInt16((ushort)count); // count (name record 개수)
            writer.WriteUInt16((ushort)stringOffset); // storageOffset (문자열 저장 영역 오프셋)
            for (int i = 0; i < count; i++)
            {
                writer.WriteUInt16(3); // platformID (3 = Windows)
                writer.WriteUInt16(1); // encodingID (1 = Unicode BMP)
                writer.WriteUInt16((ushort)records[i].LanguageId); // languageID
                writer.WriteUInt16((ushort)records[i].NameId); // nameID
                writer.WriteUInt16((ushort)(records[i].Value.Length * 2)); // length (바이트 단위)
                writer.WriteUInt16((ushort)stringOffsets[i]); // stringOffset (저장 영역 내 오프셋)
            }
            writer.WriteBytes(stringData.ToArray());
            return writer.ToArray();
        }

        private static void AddLanguageRecords(List<NameRecord> records, NameLanguage lang)
        {
            // Family/Subfamily가 모두 비어 있으면 파생 값도 비워(레코드 생략) 처리합니다.
            string fullName = JoinNonEmpty(lang.Family, lang.Subfamily, " ");
            string psName = JoinNonEmpty(lang.Family, lang.Subfamily, "-");
            if (psName != null) psName = psName.Replace(" ", "");

            AddRecord(records, lang, 0, lang.Copyright);
            AddRecord(records, lang, 1, lang.Family);
            AddRecord(records, lang, 2, lang.Subfamily);
            AddRecord(records, lang, 3, lang.UniqueId);
            AddRecord(records, lang, 4, fullName);
            AddRecord(records, lang, 5, lang.Version);
            AddRecord(records, lang, 6, psName);
            AddRecord(records, lang, 7, lang.Trademark);
            AddRecord(records, lang, 8, lang.Manufacturer);
            AddRecord(records, lang, 9, lang.Designer);
            AddRecord(records, lang, 10, lang.Description);
            AddRecord(records, lang, 13, lang.License);
            AddRecord(records, lang, 16, lang.Family);
            AddRecord(records, lang, 17, lang.Subfamily);
        }

        private static void AddRecord(List<NameRecord> records, NameLanguage lang, int nameId, string value)
        {
            if (!string.IsNullOrEmpty(value))
                records.Add(new NameRecord { LanguageId = lang.LanguageId, NameId = nameId, Value = value });
        }

        internal static string JoinNonEmpty(string a, string b, string separator)
        {
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return null;
            if (string.IsNullOrEmpty(a)) return b;
            if (string.IsNullOrEmpty(b)) return a;
            return a + separator + b;
        }

        /// <summary>
        /// name 테이블 바이트를 파싱하여 <see cref="NameTable"/>을 생성합니다. <see cref="ToBytes"/>의 역입니다.
        /// Windows 플랫폼(platformID 3, encodingID 1, UTF-16BE) 레코드를 읽습니다.
        /// 파생 nameID(4, 6, 16, 17)는 저장 시 자동 재계산되므로 읽지 않습니다.
        /// </summary>
        /// <param name="reader">테이블 데이터를 읽는 리더입니다. (테이블 시작 위치)</param>
        internal static NameTable FromBytes(OpenTypeBinaryReader reader)
        {
            var table = new NameTable();
            long tableStart = reader.Position;
            reader.ReadUInt16(); // version
            int count = reader.ReadUInt16();
            int stringOffset = reader.ReadUInt16();
            var langIds = new int[count];
            var nameIds = new int[count];
            var lens = new int[count];
            var offs = new int[count];
            for (int i = 0; i < count; i++)
            {
                reader.ReadUInt16(); // platformID
                reader.ReadUInt16(); // encodingID
                langIds[i] = reader.ReadUInt16();
                nameIds[i] = reader.ReadUInt16();
                lens[i] = reader.ReadUInt16();
                offs[i] = reader.ReadUInt16();
            }
            for (int i = 0; i < count; i++)
            {
                // 파생 nameID는 저장 시 재계산되므로 스킵
                if (nameIds[i] == 4 || nameIds[i] == 6 || nameIds[i] == 16 || nameIds[i] == 17)
                    continue;
                reader.Position = tableStart + stringOffset + offs[i];
                string value = reader.ReadUtf16BigEndian(lens[i] / 2);
                if (langIds[i] == 0x0409)
                    ApplyName(table, nameIds[i], value);
                else
                    ApplyName(FindOrAddLanguage(table, langIds[i]), nameIds[i], value);
            }
            return table;
        }

        /// <summary>
        /// languageID에 해당하는 언어 항목을 찾거나 새로 추가합니다.
        /// </summary>
        private static NameLanguage FindOrAddLanguage(NameTable table, int langId)
        {
            foreach (var lang in table.Languages)
                if (lang.LanguageId == langId)
                    return lang;
            var newLang = new NameLanguage { LanguageId = langId };
            table.Languages.Add(newLang);
            return newLang;
        }

        /// <summary>
        /// nameID에 해당하는 기본 언어 문자열을 설정합니다.
        /// </summary>
        private static void ApplyName(NameTable table, int nameId, string value)
        {
            switch (nameId)
            {
                case 0: table.Copyright = value; break;
                case 1: table.Family = value; break;
                case 2: table.Subfamily = value; break;
                case 3: table.UniqueId = value; break;
                case 5: table.Version = value; break;
                case 7: table.Trademark = value; break;
                case 8: table.Manufacturer = value; break;
                case 9: table.Designer = value; break;
                case 10: table.Description = value; break;
                case 13: table.License = value; break;
            }
        }

        /// <summary>
        /// nameID에 해당하는 언어 문자열을 설정합니다.
        /// </summary>
        private static void ApplyName(NameLanguage lang, int nameId, string value)
        {
            switch (nameId)
            {
                case 0: lang.Copyright = value; break;
                case 1: lang.Family = value; break;
                case 2: lang.Subfamily = value; break;
                case 3: lang.UniqueId = value; break;
                case 5: lang.Version = value; break;
                case 7: lang.Trademark = value; break;
                case 8: lang.Manufacturer = value; break;
                case 9: lang.Designer = value; break;
                case 10: lang.Description = value; break;
                case 13: lang.License = value; break;
            }
        }
    }

    /// <summary>
    /// name 테이블의 단일 언어 항목입니다.
    /// languageID와 해당 언어의 name 문자열을 담고 있습니다.
    /// </summary>
    public class NameLanguage
    {
        /// <summary>
        /// languageID입니다. 문자열이 작성된 언어를 식별하는 플랫폼별 값입니다.
        /// Windows 플랫폼(platformID 3)에서는 Windows LCID를 사용합니다.
        /// 자주 쓰는 값: 0x0409=en-US, 0x0412=ko-KR, 0x0411=ja-JP, 0x0804=zh-CN, 0x040C=fr-FR.
        /// </summary>
        public int LanguageId { get; set; } = 0x0409;

        /// <summary>
        /// copyright notice(nameID 0)입니다.
        /// 폰트 관리자 등에서 표시됩니다.
        /// 예: "Copyright (c) 2026 VagabondK"
        /// </summary>
        public string Copyright { get; set; }

        /// <summary>
        /// Font Family name(nameID 1)입니다.
        /// Font Subfamily name(nameID 2)과 함께 사용되며, 굵기나 스타일(italic/oblique)만
        /// 다른 최대 4개의 폰트(regular, italic, bold, bold italic) 사이에 공유되어야 합니다.
        /// 예: "VagabondK"
        /// </summary>
        public string Family { get; set; }

        /// <summary>
        /// Font Subfamily name(nameID 2)입니다.
        /// 같은 Font Family name을 가진 폰트 그룹에서 굵기와 스타일(italic/oblique) 변형을 구분합니다.
        /// 예: "Regular"
        /// </summary>
        public string Subfamily { get; set; }

        /// <summary>
        /// Unique font identifier(nameID 3)입니다.
        /// 같은 family의 다른 스타일/리비전과 구별되도록 설정해야 합니다.
        /// 예: "VagabondK: 1.0"
        /// </summary>
        public string UniqueId { get; set; }

        /// <summary>
        /// Version string(nameID 5)입니다.
        /// "Version &lt;숫자&gt;.&lt;숫자&gt;" 패턴으로 시작해야 합니다.
        /// Windows는 이 문자열을 폰트 버전 판정에 사용합니다.
        /// 예: "Version 1.0"
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Trademark(nameID 7)입니다.
        /// copyright와 명확히 구분되는 상표 고지/정보입니다.
        /// 예: "VagabondK is a trademark of VagabondK"
        /// </summary>
        public string Trademark { get; set; }

        /// <summary>
        /// Manufacturer Name(nameID 8)입니다.
        /// 예: "VagabondK"
        /// </summary>
        public string Manufacturer { get; set; }

        /// <summary>
        /// Designer(nameID 9)입니다.
        /// 예: "VagabondK"
        /// </summary>
        public string Designer { get; set; }

        /// <summary>
        /// Description(nameID 10)입니다.
        /// 개정 정보, 사용 권장 사항, 역사, feature 등을 포함할 수 있습니다.
        /// 예: "VagabondK OpenType generated font"
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// License Description(nameID 13)입니다.
        /// 예: "Free for use"
        /// </summary>
        public string License { get; set; }
    }
}
