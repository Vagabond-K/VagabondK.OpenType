using System.Collections.Generic;

namespace VagabondK.OpenType.Tables
{
    /// <summary>
    /// hmtx 테이블(수평 메트릭)입니다.
    /// 글리프별 advance width와 left side bearing을 glyph ID 순서로 저장합니다.
    /// advance width는 항상 이 테이블에서 가져오며, right side bearing은
    /// advance width - left side bearing - (xMax - xMin)으로 계산됩니다.
    /// numberOfHMetrics가 글리프 수보다 작으면 마지막 레코드의 advance width가
    /// 나머지 글리프에 적용되고, left side bearing만 추가 배열로 저장됩니다.
    /// </summary>
    class HmtxTable : OpenTypeTable
    {
        private readonly List<int> advanceWidths;
        private readonly List<int> leftSideBearings;

        /// <summary>
        /// 글리프별 advance width 목록입니다. (glyph ID 순서)
        /// </summary>
        public IReadOnlyList<int> AdvanceWidths => advanceWidths;

        /// <summary>
        /// 글리프별 left side bearing 목록입니다. (glyph ID 순서)
        /// </summary>
        public IReadOnlyList<int> LeftSideBearings => leftSideBearings;

        /// <summary>
        /// <see cref="HmtxTable"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="advanceWidths">글리프별 advance width 목록입니다. (glyph ID 순서)</param>
        /// <param name="leftSideBearings">글리프별 left side bearing 목록입니다. (glyph ID 순서)</param>
        public HmtxTable(IReadOnlyList<int> advanceWidths, IReadOnlyList<int> leftSideBearings)
        {
            this.advanceWidths = new List<int>(advanceWidths);
            this.leftSideBearings = new List<int>(leftSideBearings);
        }

        /// <inheritdoc />
        internal override string Tag => "hmtx";

        /// <inheritdoc />
        internal override byte[] ToBytes()
        {
            var writer = new OpenTypeBinaryWriter();
            for (int i = 0; i < advanceWidths.Count; i++)
            {
                writer.WriteUInt16((ushort)advanceWidths[i]);
                writer.WriteInt16((short)leftSideBearings[i]);
            }
            return writer.ToArray();
        }

        /// <summary>
        /// hmtx 테이블 바이트를 파싱하여 <see cref="HmtxTable"/>을 생성합니다. <see cref="ToBytes"/>의 역입니다.
        /// </summary>
        /// <param name="reader">테이블 데이터를 읽는 리더입니다. (테이블 시작 위치)</param>
        /// <param name="numGlyphs">글리프 개수입니다. (advance/lsb 쌍의 수)</param>
        internal static HmtxTable FromBytes(OpenTypeBinaryReader reader, int numGlyphs)
        {
            var advanceWidths = new List<int>(numGlyphs);
            var leftSideBearings = new List<int>(numGlyphs);
            for (int i = 0; i < numGlyphs; i++)
            {
                advanceWidths.Add(reader.ReadUInt16());
                leftSideBearings.Add(reader.ReadInt16());
            }
            return new HmtxTable(advanceWidths, leftSideBearings);
        }
    }
}
