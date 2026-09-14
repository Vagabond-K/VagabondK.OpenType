namespace VagabondK.OpenType.Tables
{
    /// <summary>
    /// loca 테이블(index to location)입니다.
    /// glyf 테이블 내 각 글리프 설명의 오프셋을 glyf 테이블 시작 기준 오프셋으로 저장합니다.
    /// 오프셋은 오름차순이어야 하며, 각 글리프 설명의 길이는 연속된 두 항목의 차이
    /// (loca[n+1] - loca[n])로 결정됩니다. 마지막 글리프의 길이를 계산하기 위해
    /// 오프셋 배열의 요소 수는 numGlyphs + 1입니다.
    /// 형식은 head 테이블의 indexToLocFormat으로 지정됩니다.
    /// </summary>
    class LocaTable : OpenTypeTable
    {
        private readonly int[] offsets;
        private readonly bool useLongFormat;

        /// <summary>
        /// <see cref="LocaTable"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="offsets">글리프 오프셋 배열입니다. (글리프 개수 + 1)</param>
        /// <param name="useLongFormat">long 형식(Offset32, 4바이트)을 사용할지 여부입니다. <c>false</c>이면 short 형식(Offset16, 2바이트, 오프셋을 2로 나눈 값 저장)입니다.</param>
        public LocaTable(int[] offsets, bool useLongFormat)
        {
            this.offsets = offsets;
            this.useLongFormat = useLongFormat;
        }

        /// <inheritdoc />
        internal override string Tag => "loca";

        /// <inheritdoc />
        internal override byte[] ToBytes()
        {
            var writer = new OpenTypeBinaryWriter();
            if (useLongFormat)
                foreach (int offset in offsets)
                    writer.WriteUInt32((uint)offset);
            else
                foreach (int offset in offsets)
                    writer.WriteUInt16((ushort)(offset / 2));
            return writer.ToArray();
        }

        /// <summary>
        /// loca 테이블 바이트에서 글리프 오프셋 배열을 읽습니다. <see cref="ToBytes"/>의 역입니다.
        /// </summary>
        /// <param name="reader">테이블 데이터를 읽는 리더입니다. (테이블 시작 위치)</param>
        /// <param name="numGlyphs">글리프 개수입니다. (오프셋 수 = numGlyphs + 1)</param>
        /// <param name="useLongFormat">long 형식(Offset32) 여부입니다. head의 indexToLocFormat에서 가져옵니다.</param>
        /// <returns>글리프 오프셋 배열입니다. (글리프 개수 + 1)</returns>
        internal static int[] ReadOffsets(OpenTypeBinaryReader reader, int numGlyphs, bool useLongFormat)
        {
            var result = new int[numGlyphs + 1];
            if (useLongFormat)
            {
                for (int i = 0; i <= numGlyphs; i++)
                    result[i] = (int)reader.ReadUInt32();
            }
            else
            {
                for (int i = 0; i <= numGlyphs; i++)
                    result[i] = reader.ReadUInt16() * 2;
            }
            return result;
        }
    }
}
