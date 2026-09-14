using System;
using System.Collections.Generic;
using VagabondK.OpenType.CFF;
using VagabondK.OpenType.Geometry;

namespace VagabondK.OpenType.Tables
{
    /// <summary>
    /// CFF 테이블입니다. OTF(CFF) 폰트의 글리프 윤곽을 PostScript CFF v1 형식으로 저장합니다.
    /// 바이너리 생성은 <see cref="CffBuilder"/>가 담당합니다.
    /// </summary>
    public class CffTable : OpenTypeTable
    {
        /// <summary>
        /// Top DICT 옵션입니다. null인 항목은 Top DICT에 포함하지 않습니다.
        /// </summary>
        public TopDict TopDict { get; set; } = new TopDict();

        /// <summary>
        /// Private DICT 옵션입니다. null인 항목은 Private DICT에 포함하지 않습니다.
        /// </summary>
        public PrivateDict Private { get; set; } = new PrivateDict();

        private string psName;
        private List<string> glyphNames;
        private List<Glyph> glyphs;

        /// <summary>
        /// <see cref="CffTable"/>의 새 인스턴스를 생성합니다.
        /// Top DICT/Private DICT 옵션은 <see cref="TopDict"/>/<see cref="Private"/> 속성으로 설정합니다.
        /// </summary>
        public CffTable()
        {
        }

        /// <inheritdoc />
        internal override string Tag => "CFF ";

        internal void SetProperties(string psName, List<string> glyphNames, List<Glyph> glyphs)
        {
            this.psName = psName;
            this.glyphNames = glyphNames;
            this.glyphs = glyphs;
        }

        /// <inheritdoc />
        internal override byte[] ToBytes()
        {
            if (string.IsNullOrEmpty(psName))
                throw new InvalidOperationException("PsName is required. (Name INDEX)");

            return CffBuilder.Build(psName, TopDict, Private, glyphNames, glyphs);
        }
    }
}
