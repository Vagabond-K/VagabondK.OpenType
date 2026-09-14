using System.Collections.Generic;
using System.Linq;
using VagabondK.OpenType.CFF;
using VagabondK.OpenType.Tables;

namespace VagabondK.OpenType
{
    /// <summary>
    /// OpenType(CFF 윤곽, OTF) 폰트 파일을 생성하는 클래스입니다.
    /// 글리프와 메타데이터를 추가한 후 <see cref="FontBase.ToBytes"/>를 호출하여
    /// sfnt 형식의 OTF 파일 바이트 배열을 생성합니다.
    /// sfnt 파일은 테이블 디렉터리와 각 테이블 데이터로 구성되며,
    /// CFF/hmtx/cmap/maxp 테이블은 글리프에서 자동 생성됩니다.
    /// TTF와 달리 glyf/loca 테이블 대신 CFF 테이블을 사용하고,
    /// sfnt version은 'OTTO'(0x4F54544F)를 사용합니다.
    /// </summary>
    public class OtfFont : FontBase
    {
        /// <summary>
        /// CFF 테이블입니다. Top DICT/Private DICT 옵션을 조작할 수 있습니다.
        /// </summary>
        public CffTable CFF { get; }

        /// <summary>
        /// <see cref="OtfFont"/>의 새 인스턴스를 생성합니다.
        /// 글리프 0은 .notdef(테두리만 있는 사각형) 글리프로 자동 생성됩니다.
        /// </summary>
        public OtfFont()
        {
            CFF = new CffTable();
        }

        /// <inheritdoc />
        protected override uint SfntVersion => 0x4F54544F; // sfnt version 'OTTO' (CFF)

        /// <inheritdoc />
        protected override List<OpenTypeTable> CreateOutlineTables(int xMin, int yMin, int xMax, int yMax)
        {
            // CFF 테이블용 글리프 이름 생성: .notdef, standard name(ASCII/Latin-1) 또는 uniXXXX (glyph ID 순서)
            var glyphNames = new List<string>();
            glyphNames.Add(".notdef");
            for (int i = 1; i < Glyphs.Count; i++)
            {
                int charCode = 0;
                foreach (var kv in CharToGlyph)
                {
                    if (kv.Value == i)
                    {
                        charCode = kv.Key;
                        break;
                    }
                }
                string name;
                if (!CffBuilder.TryGetStandardName(charCode, out name))
                    name = "uni" + charCode.ToString("X4");
                glyphNames.Add(name);
            }

            // Family/Subfamily가 null/빈 값일 수 있으므로 JoinNonEmpty로 안전하게 결합합니다.
            // psName이 null/빈 값이면 CffTable.ToBytes()에서 예외가 발생합니다.
            CFF.SetProperties(ComputePsName(), glyphNames, Glyphs.ToList());
            CFF.TopDict.FontBBox = new double[] { xMin, yMin, xMax, yMax };

            // Top DICT의 name 필드를 테이블 상태에서 자동 채웁니다.
            // FontBuilder 경로는 ApplyCascade가 이미 설정하므로 null 체크에서 스킵됩니다.
            // 직접 생성(new OtfFont()) 시에도 Name/Os2/Post만 설정하면 Top DICT가 완성됩니다.
            if (CFF.TopDict.FullName == null)
                CFF.TopDict.FullName = NameTable.JoinNonEmpty(Name.Family, Name.Subfamily, " ");
            if (CFF.TopDict.FamilyName == null)
                CFF.TopDict.FamilyName = Name.Family;
            if (CFF.TopDict.Weight == null)
                CFF.TopDict.Weight = (CffWeight)Os2.WeightClass; // 두 enum의 값이 일치(100~900). Normal(400)→Regular(400)
            if (CFF.TopDict.Version == null)
                CFF.TopDict.Version = Name.Version?.Replace("Version ", "");
            if (CFF.TopDict.IsFixedPitch == null)
                CFF.TopDict.IsFixedPitch = Post.IsFixedPitch ? 1 : 0;
            if (CFF.TopDict.ItalicAngle == null)
                CFF.TopDict.ItalicAngle = Post.ItalicAngle != 0 ? Post.ItalicAngle : null;

            return new List<OpenTypeTable> { CFF };
        }

        /// <summary>
        /// CFF 폰트는 maxp version 0.5(numGlyphs만)를 사용합니다.
        /// </summary>
        internal override MaxpTable CreateMaxpTable(int numGlyphs, int maxPoints, int maxContours)
            => new MaxpTable(numGlyphs, 0, 0, true);

        /// <summary>
        /// CFF Name INDEX의 psName을 계산합니다.
        /// Family/Subfamily를 '-'로 결합하고 공백을 제거합니다.
        /// </summary>
        private string ComputePsName()
        {
            string psName = NameTable.JoinNonEmpty(Name.Family, Name.Subfamily, "-");
            if (psName != null) psName = psName.Replace(" ", "");
            return psName;
        }

        /// <inheritdoc />
        protected override void ValidateOutline(List<ValidationResult> results)
        {
            // CFF Weight <-> OS/2 WeightClass
            if (CFF.TopDict.Weight.HasValue && (int)CFF.TopDict.Weight.Value != (int)Os2.WeightClass)
                results.Add(new ValidationResult(ValidationLevel.Warning,
                    $"CFF Top DICT Weight ({CFF.TopDict.Weight.Value}) does not match OS/2 usWeightClass ({Os2.WeightClass})."));

            // psName 형식
            string psName = ComputePsName();
            if (!string.IsNullOrEmpty(psName) && !IsValidPostScriptName(psName))
                results.Add(new ValidationResult(ValidationLevel.Warning,
                    $"PostScript name '{psName}' contains invalid characters; only letters, digits, hyphen, and apostrophe are allowed."));
        }

        /// <summary>
        /// PostScript name 규칙(영문/숫자/하이픈/아포스트리프만 허용)을 검사합니다.
        /// </summary>
        private static bool IsValidPostScriptName(string name)
        {
            if (name.Length == 0) return false;
            foreach (char c in name)
            {
                bool valid = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')
                    || (c >= '0' && c <= '9') || c == '-' || c == '\'';
                if (!valid) return false;
            }
            return true;
        }
    }
}
