namespace VagabondK.OpenType.Tables
{
    /// <summary>
    /// post 테이블(포스트스크립트)입니다. version 3.0을 사용합니다.
    /// version 3.0은 글리프의 PostScript 이름을 제공하지 않으며,
    /// TrueType 윤곽의 OpenType 폰트에 사용할 수 있습니다.
    /// Windows는 이 테이블의 italicAngle 값을 사용합니다.
    /// </summary>
    public class PostTable : OpenTypeTable
    {
        /// <summary>
        /// italicAngle입니다 (16.16 고정 소수점). 수직선 기준 반시계 방향 각도(도)입니다.
        /// 수직 텍스트는 0, 오른쪽으로 기울어진 텍스트는 음수입니다.
        /// Windows는 이 값을 caret(텍스트 커서) 기울임에 사용합니다.
        /// 윤곽에 물리 shear를 적용했더라도 실제 기울임 각도를 설정해야 합니다.
        /// 주류 렌더러(GDI/DirectWrite/FreeType)는 이 값을 보고 윤곽에 합성 shear를 적용하지 않고,
        /// 커서 기울임과 스타일 식별 메타데이터로만 사용하므로 이중 기울임이 발생하지 않습니다.
        /// (Arial Italic도 윤곽을 물리적으로 기울이면서 italicAngle=-12를 설정하는 동일한 패턴)
        /// </summary>
        public double ItalicAngle { get; set; }

        /// <summary>
        /// underlinePosition입니다.
        /// baseline 아래로 내려가는 값이므로 일반적으로 음수입니다.
        /// </summary>
        public int UnderlinePosition { get; set; } = -100;

        /// <summary>
        /// underlineThickness입니다.
        /// 일반적으로 underscore(U+005F) 글리프의 두께와 OS/2의 yStrikeoutSize와 일치해야 합니다.
        /// </summary>
        public int UnderlineThickness { get; set; } = 50;

        /// <summary>
        /// isFixedPitch입니다. 0이면 비례(proportional) 폰트, 0이 아니면 고정폭(monospaced) 폰트입니다.
        /// 고정폭 폰트는 모든 글리프의 advance width가 동일해야 합니다.
        /// </summary>
        public bool IsFixedPitch { get; set; }

        /// <inheritdoc />
        internal override string Tag => "post";

        /// <inheritdoc />
        internal override byte[] ToBytes()
        {
            var writer = new OpenTypeBinaryWriter();
            writer.WriteUInt32(0x00030000); // format 3.0
            writer.WriteFixed(ItalicAngle); // italicAngle
            writer.WriteInt16((short)UnderlinePosition); // underlinePosition
            writer.WriteInt16((short)UnderlineThickness); // underlineThickness
            writer.WriteUInt32(IsFixedPitch ? 1u : 0u); // isFixedPitch
            writer.WriteUInt32(0); // minMemType42
            writer.WriteUInt32(0); // maxMemType42
            writer.WriteUInt32(0); // minMemType1
            writer.WriteUInt32(0); // maxMemType1
            return writer.ToArray();
        }

        /// <summary>
        /// post 테이블 바이트를 파싱하여 <see cref="PostTable"/>을 생성합니다. <see cref="ToBytes"/>의 역입니다.
        /// </summary>
        /// <param name="reader">테이블 데이터를 읽는 리더입니다. (테이블 시작 위치)</param>
        internal static PostTable FromBytes(OpenTypeBinaryReader reader)
        {
            var table = new PostTable();
            reader.ReadUInt32(); // format
            table.ItalicAngle = reader.ReadFixed(); // italicAngle
            table.UnderlinePosition = reader.ReadInt16(); // underlinePosition
            table.UnderlineThickness = reader.ReadInt16(); // underlineThickness
            table.IsFixedPitch = reader.ReadUInt32() != 0; // isFixedPitch
            reader.ReadUInt32(); // minMemType42
            reader.ReadUInt32(); // maxMemType42
            reader.ReadUInt32(); // minMemType1
            reader.ReadUInt32(); // maxMemType1
            return table;
        }
    }
}
