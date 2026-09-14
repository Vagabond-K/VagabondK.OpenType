namespace VagabondK.OpenType.Tables
{
    /// <summary>
    /// hhea 테이블(수평 헤더)입니다.
    /// 수평 레이아웃에 필요한 정보를 포함합니다.
    /// minRightSideBearing, minLeftSideBearing, xMaxExtent는 contour가 있는 글리프만
    /// 사용하여 계산하며, contour가 없는 글리프는 무시합니다.
    /// ascender/descender/lineGap은 Apple 플랫폼 전용이며, Windows에서는
    /// OS/2 테이블의 sTypoAscender/sTypoDescender/sTypoLineGap가 사용됩니다.
    /// </summary>
    public class HheaTable : OpenTypeTable
    {
        /// <summary>
        /// ascender입니다.
        /// Apple 플랫폼에서 사용되며, Windows에서는 OS/2의 sTypoAscender가 사용됩니다.
        /// ascender - descender + lineGap가 기본 줄 높이(line height)를 결정합니다.
        /// </summary>
        public int Ascender { get; set; } = 750;

        /// <summary>
        /// descender입니다.
        /// baseline 아래로 내려가는 값이므로 일반적으로 음수입니다.
        /// </summary>
        public int Descender { get; set; } = -250;

        /// <summary>
        /// lineGap입니다.
        /// 일부 레거시 플랫폼 구현에서는 음수 lineGap를 0으로 처리합니다.
        /// </summary>
        public int LineGap { get; set; }

        /// <summary>
        /// advanceWidthMax입니다. hmtx 테이블의 advance width 중 최대 값입니다. 글리프에서 자동 계산됩니다.
        /// </summary>
        internal int AdvanceWidthMax { get; set; }

        /// <summary>
        /// minLeftSideBearing입니다. contour가 있는 글리프의 hmtx left side bearing 중 최소 값입니다. 글리프에서 자동 계산됩니다.
        /// </summary>
        internal int MinLeftSideBearing { get; set; }

        /// <summary>
        /// minRightSideBearing입니다. contour가 있는 글리프의 min(aw - (lsb + xMax - xMin)) 값입니다. 글리프에서 자동 계산됩니다.
        /// </summary>
        internal int MinRightSideBearing { get; set; }

        /// <summary>
        /// xMaxExtent입니다. contour가 있는 글리프의 max(lsb + (xMax - xMin)) 값입니다. 글리프에서 자동 계산됩니다.
        /// </summary>
        internal int XMaxExtent { get; set; }

        /// <summary>
        /// numberOfHMetrics입니다. hmtx 테이블의 hMetric 레코드 개수입니다. 글리프에서 자동 계산됩니다.
        /// </summary>
        internal int NumberOfHMetrics { get; set; }


        /// <summary>
        /// caretSlopeRise입니다. 권장 caret 기울기의 분자입니다.
        /// caret 기울기는 caretSlopeRise/caretSlopeRun으로 계산됩니다.
        /// </summary>
        public int CaretSlopeRise { get; set; } = 1;

        /// <summary>
        /// caretSlopeRun입니다. 권장 caret 기울기의 분모입니다.
        /// caret 기울기는 caretSlopeRise/caretSlopeRun으로 계산됩니다.
        /// </summary>
        public int CaretSlopeRun { get; set; }

        /// <summary>
        /// caretOffset입니다. caret 끝에서 글리프의 left side bearing 선까지의 거리입니다.
        /// </summary>
        public int CaretOffset { get; set; }

        /// <inheritdoc />
        internal override string Tag => "hhea";

        /// <inheritdoc />
        internal override byte[] ToBytes()
        {
            var writer = new OpenTypeBinaryWriter();
            writer.WriteUInt32(0x00010000); // version 1.0
            writer.WriteInt16((short)Ascender);
            writer.WriteInt16((short)Descender);
            writer.WriteInt16((short)LineGap);
            writer.WriteUInt16((ushort)AdvanceWidthMax);
            writer.WriteInt16((short)MinLeftSideBearing);
            writer.WriteInt16((short)MinRightSideBearing);
            writer.WriteInt16((short)XMaxExtent);
            writer.WriteInt16((short)CaretSlopeRise);
            writer.WriteInt16((short)CaretSlopeRun);
            writer.WriteInt16((short)CaretOffset);
            writer.WriteInt16(0); // reserved
            writer.WriteInt16(0); // reserved
            writer.WriteInt16(0); // reserved
            writer.WriteInt16(0); // reserved
            writer.WriteInt16(0); // metricDataFormat
            writer.WriteUInt16((ushort)NumberOfHMetrics);
            return writer.ToArray();
        }

        /// <summary>
        /// hhea 테이블 바이트를 파싱하여 <see cref="HheaTable"/>을 생성합니다. <see cref="ToBytes"/>의 역입니다.
        /// </summary>
        /// <param name="reader">테이블 데이터를 읽는 리더입니다. (테이블 시작 위치)</param>
        internal static HheaTable FromBytes(OpenTypeBinaryReader reader)
        {
            var table = new HheaTable();
            reader.ReadUInt32(); // version
            table.Ascender = reader.ReadInt16();
            table.Descender = reader.ReadInt16();
            table.LineGap = reader.ReadInt16();
            table.AdvanceWidthMax = reader.ReadUInt16();
            table.MinLeftSideBearing = reader.ReadInt16();
            table.MinRightSideBearing = reader.ReadInt16();
            table.XMaxExtent = reader.ReadInt16();
            table.CaretSlopeRise = reader.ReadInt16();
            table.CaretSlopeRun = reader.ReadInt16();
            table.CaretOffset = reader.ReadInt16();
            reader.ReadInt16(); // reserved
            reader.ReadInt16(); // reserved
            reader.ReadInt16(); // reserved
            reader.ReadInt16(); // reserved
            reader.ReadInt16(); // metricDataFormat
            table.NumberOfHMetrics = reader.ReadUInt16();
            return table;
        }
    }
}
