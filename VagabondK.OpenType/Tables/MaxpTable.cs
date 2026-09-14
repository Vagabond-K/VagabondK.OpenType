namespace VagabondK.OpenType.Tables
{
    /// <summary>
    /// maxp 테이블(maximum profile)입니다.
    /// 이 폰트의 메모리 요구 사항을 정의합니다.
    /// TrueType 윤곽의 폰트는 version 1.0을 사용하며 모든 데이터가 필요합니다.
    /// CFF 윤곽의 폰트는 version 0.5를 사용하며 numGlyphs만 저장합니다.
    /// numGlyphs는 glyf 테이블의 글리프 수를 지정합니다.
    /// </summary>
    class MaxpTable : OpenTypeTable
    {
        private readonly int numGlyphs;
        private readonly int maxPoints;
        private readonly int maxContours;
        private readonly bool isCff;

        /// <summary>
        /// 글리프 개수입니다.
        /// </summary>
        public int NumGlyphs => numGlyphs;

        /// <summary>
        /// <see cref="MaxpTable"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="numGlyphs">글리프 개수입니다.</param>
        /// <param name="maxPoints">non-composite 글리프의 최대 점 개수입니다.</param>
        /// <param name="maxContours">non-composite 글리프의 최대 contour 개수입니다.</param>
        public MaxpTable(int numGlyphs, int maxPoints, int maxContours)
            : this(numGlyphs, maxPoints, maxContours, false)
        {
        }

        /// <summary>
        /// <see cref="MaxpTable"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="numGlyphs">글리프 개수입니다.</param>
        /// <param name="maxPoints">non-composite 글리프의 최대 점 개수입니다.</param>
        /// <param name="maxContours">non-composite 글리프의 최대 contour 개수입니다.</param>
        /// <param name="isCff">CFF 윤곽 폰트 여부입니다. <c>true</c>이면 version 0.5(numGlyphs만)로 직렬화됩니다.</param>
        public MaxpTable(int numGlyphs, int maxPoints, int maxContours, bool isCff)
        {
            this.numGlyphs = numGlyphs;
            this.maxPoints = maxPoints;
            this.maxContours = maxContours;
            this.isCff = isCff;
        }

        /// <inheritdoc />
        internal override string Tag => "maxp";

        /// <inheritdoc />
        internal override byte[] ToBytes()
        {
            var writer = new OpenTypeBinaryWriter();
            if (isCff)
            {
                writer.WriteUInt32(0x00005000); // tableVersion 0.5 (CFF)
                writer.WriteUInt16((ushort)numGlyphs);
                return writer.ToArray();
            }

            writer.WriteUInt32(0x00010000); // tableVersion 1.0
            writer.WriteUInt16((ushort)numGlyphs);
            writer.WriteUInt16((ushort)maxPoints);
            writer.WriteUInt16((ushort)maxContours);
            writer.WriteUInt16(0); // maxCompositePoints
            writer.WriteUInt16(0); // maxCompositeContours
            writer.WriteUInt16(2); // maxZones
            writer.WriteUInt16(0); // maxTwilightPoints
            writer.WriteUInt16(0); // maxStorage
            writer.WriteUInt16(0); // maxFunctionDefs
            writer.WriteUInt16(0); // maxInstructionDefs
            writer.WriteUInt16(0); // maxStackElements
            writer.WriteUInt16(0); // maxSizeOfInstructions
            writer.WriteUInt16(0); // maxComponentElements
            writer.WriteUInt16(0); // maxComponentDepth
            return writer.ToArray();
        }

        /// <summary>
        /// maxp 테이블 바이트를 파싱하여 <see cref="MaxpTable"/>을 생성합니다. <see cref="ToBytes"/>의 역입니다.
        /// version 0.5(CFF, numGlyphs만)와 version 1.0(TrueType)을 모두 지원합니다.
        /// </summary>
        /// <param name="reader">테이블 데이터를 읽는 리더입니다. (테이블 시작 위치)</param>
        internal static MaxpTable FromBytes(OpenTypeBinaryReader reader)
        {
            uint version = reader.ReadUInt32(); // tableVersion
            int numGlyphs = reader.ReadUInt16();
            if (version == 0x00005000)
            {
                // CFF: numGlyphs만
                return new MaxpTable(numGlyphs, 0, 0, true);
            }

            // TrueType version 1.0
            int maxPoints = reader.ReadUInt16();
            int maxContours = reader.ReadUInt16();
            reader.ReadUInt16(); // maxCompositePoints
            reader.ReadUInt16(); // maxCompositeContours
            reader.ReadUInt16(); // maxZones
            reader.ReadUInt16(); // maxTwilightPoints
            reader.ReadUInt16(); // maxStorage
            reader.ReadUInt16(); // maxFunctionDefs
            reader.ReadUInt16(); // maxInstructionDefs
            reader.ReadUInt16(); // maxStackElements
            reader.ReadUInt16(); // maxSizeOfInstructions
            reader.ReadUInt16(); // maxComponentElements
            reader.ReadUInt16(); // maxComponentDepth
            return new MaxpTable(numGlyphs, maxPoints, maxContours, false);
        }
    }
}
