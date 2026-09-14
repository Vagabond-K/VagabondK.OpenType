using System;
using System.Collections.Generic;

namespace VagabondK.OpenType.Tables
{
    /// <summary>
    /// cmap 테이블(문자 코드 → 글리프 인덱스 매핑)입니다.
    /// format 4 서브테이블(Windows 플랫폼, platformID 3, encodingID 1, Unicode BMP U+0000–U+FFFF)과
    /// format 12 서브테이블(platformID 3, encodingID 10, Unicode 전체 범위 U+0000–U+10FFFF)을 함께 생성합니다.
    /// Windows GDI는 format 12 단독 구성을 거부하므로 format 4 서브테이블이 필요합니다.
    /// 글리프로 매핑되지 않는 문자 코드는 glyph index 0(.notdef)으로 매핑되어야 합니다.
    /// </summary>
    class CmapTable : OpenTypeTable
    {
        /// <summary>
        /// 유니코드 코드포인트와 글리프 인덱스의 매핑 쌍입니다.
        /// </summary>
        public struct CmapMapping
        {
            /// <summary>유니코드 코드포인트입니다.</summary>
            public int CharCode { get; }

            /// <summary>글리프 인덱스입니다.</summary>
            public int GlyphId { get; }

            /// <summary>
            /// <see cref="CmapMapping"/>의 새 인스턴스를 생성합니다.
            /// </summary>
            /// <param name="charCode">유니코드 코드포인트입니다.</param>
            /// <param name="glyphId">글리프 인덱스입니다.</param>
            public CmapMapping(int charCode, int glyphId)
            {
                CharCode = charCode;
                GlyphId = glyphId;
            }
        }

        private struct Group
        {
            public int StartCode { get; }
            public int EndCode { get; }
            public int StartGlyphId { get; }

            public Group(int startCode, int endCode, int startGlyphId)
            {
                StartCode = startCode;
                EndCode = endCode;
                StartGlyphId = startGlyphId;
            }
        }

        private readonly List<CmapMapping> mappings;

        /// <summary>
        /// 유니코드 → 글리프 매핑 목록입니다.
        /// </summary>
        public IReadOnlyList<CmapMapping> Mappings => mappings;

        /// <summary>
        /// <see cref="CmapTable"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="mappings">유니코드 → 글리프 매핑 목록입니다.</param>
        public CmapTable(IReadOnlyList<CmapMapping> mappings)
        {
            this.mappings = new List<CmapMapping>(mappings);
        }

        /// <inheritdoc />
        internal override string Tag => "cmap";

        /// <inheritdoc />
        internal override byte[] ToBytes()
        {
            var sorted = new List<CmapMapping>(mappings);
            sorted.Sort((a, b) => a.CharCode.CompareTo(b.CharCode));

            // group 병합: 연속 charCode + glyphId가 1씩 증가하는 구간을 하나의 group으로
            var groups = new List<Group>();
            foreach (var mapping in sorted)
            {
                if (groups.Count > 0)
                {
                    var last = groups[groups.Count - 1];
                    if (mapping.CharCode == last.EndCode + 1 &&
                        mapping.GlyphId == last.StartGlyphId + (mapping.CharCode - last.StartCode))
                    {
                        groups[groups.Count - 1] = new Group(last.StartCode, mapping.CharCode, last.StartGlyphId);
                        continue;
                    }
                }
                groups.Add(new Group(mapping.CharCode, mapping.CharCode, mapping.GlyphId));
            }

            // format 4는 BMP(U+0000–U+FFFF) 매핑만 사용. 경계를 넘는 group은 BMP 쪽으로 잘라낸다.
            // 마지막 0xFFFE까지로 제한 — 마지막 mandatory segment [0xFFFF, 0xFFFF]와 중첩을 피한다.
            var bmpGroups = new List<Group>();
            foreach (var group in groups)
            {
                if (group.StartCode > 0xFFFE)
                    break;
                bmpGroups.Add(group.EndCode > 0xFFFE
                    ? new Group(group.StartCode, 0xFFFE, group.StartGlyphId)
                    : group);
            }

            byte[] format4 = BuildFormat4(bmpGroups);

            var writer = new OpenTypeBinaryWriter();
            writer.WriteUInt16(0); // version (0)

            if (groups.Count == 0)
            {
                // 매핑 없음: format 4(mandatory segment)만. format 12는 non-BMP 매핑이 있을 때만 생성.
                writer.WriteUInt16(1); // numTables
                writer.WriteUInt16(3); // platformID (3 = Windows)
                writer.WriteUInt16(1); // encodingID (1 = Unicode BMP)
                writer.WriteUInt32(12); // subtableOffset (헤더 12바이트 이후)
                writer.WriteBytes(format4);
            }
            else
            {
                byte[] format12 = BuildFormat12(groups);
                writer.WriteUInt16(2); // numTables (encoding record 개수)
                writer.WriteUInt16(3); // platformID (3 = Windows)
                writer.WriteUInt16(1); // encodingID (1 = Unicode BMP)
                writer.WriteUInt32(20); // subtableOffset (format 4, 헤더 20바이트 이후)
                writer.WriteUInt16(3); // platformID (3 = Windows)
                writer.WriteUInt16(10); // encodingID (10 = Unicode full repertoire)
                writer.WriteUInt32(20u + (uint)format4.Length); // subtableOffset (format 12)
                writer.WriteBytes(format4);
                writer.WriteBytes(format12);
            }
            return writer.ToArray();

            byte[] BuildFormat4(List<Group> segments)
            {
                int segCount = segments.Count + 1; // 마지막 0xFFFF segment 포함

                int powerOf2 = 1;
                while (powerOf2 * 2 <= segCount)
                    powerOf2 *= 2;
                int searchRange = powerOf2 * 2;
                int entrySelector = 0;
                for (int p = powerOf2; p > 1; p >>= 1)
                    entrySelector++;
                int rangeShift = segCount * 2 - searchRange;

                int length = 16 + segCount * 8;
                var w = new OpenTypeBinaryWriter(length);
                w.WriteUInt16(4); // format
                w.WriteUInt16((ushort)length); // length
                w.WriteUInt16(0); // language
                w.WriteUInt16((ushort)(segCount * 2)); // segCountX2
                w.WriteUInt16((ushort)searchRange);
                w.WriteUInt16((ushort)entrySelector);
                w.WriteUInt16((ushort)rangeShift);

                foreach (var segment in segments)
                    w.WriteUInt16((ushort)segment.EndCode);
                w.WriteUInt16(0xFFFF); // endCode[] 마지막

                w.WriteUInt16(0); // reservedPad

                foreach (var segment in segments)
                    w.WriteUInt16((ushort)segment.StartCode);
                w.WriteUInt16(0xFFFF); // startCode[] 마지막

                foreach (var segment in segments)
                    w.WriteUInt16((ushort)((segment.StartGlyphId - segment.StartCode) & 0xFFFF));
                w.WriteUInt16(1); // idDelta[] 마지막

                for (int i = 0; i < segCount; i++)
                    w.WriteUInt16(0); // idRangeOffset[] (모두 0)

                return w.ToArray();
            }

            byte[] BuildFormat12(List<Group> allGroups)
            {
                int numGroups = allGroups.Count;
                int length = 16 + numGroups * 12;
                var w = new OpenTypeBinaryWriter(length);
                w.WriteUInt16(12); // format
                w.WriteUInt16(0); // reserved
                w.WriteUInt32((uint)length); // length
                w.WriteUInt32(0); // language
                w.WriteUInt32((uint)numGroups); // numGroups
                foreach (var group in allGroups)
                {
                    w.WriteUInt32((uint)group.StartCode); // startCharCode[]
                    w.WriteUInt32((uint)group.EndCode); // endCharCode[]
                    w.WriteUInt32((uint)group.StartGlyphId); // startGlyphID[]
                }
                return w.ToArray();
            }
        }

        /// <summary>
        /// cmap 테이블 바이트를 파싱하여 <see cref="CmapTable"/>을 생성합니다. <see cref="ToBytes"/>의 역입니다.
        /// format 4(BMP)와 format 12(전체 범위) 서브테이블을 모두 파싱해 매핑을 재구성합니다.
        /// </summary>
        /// <param name="reader">테이블 데이터를 읽는 리더입니다. (테이블 시작 위치)</param>
        internal static CmapTable FromBytes(OpenTypeBinaryReader reader)
        {
            var mappings = new Dictionary<int, int>(); // charCode -> glyphId
            long tableStart = reader.Position;
            reader.ReadUInt16(); // version
            int numTables = reader.ReadUInt16();
            var subtableOffsets = new int[numTables];
            for (int i = 0; i < numTables; i++)
            {
                reader.ReadUInt16(); // platformID
                reader.ReadUInt16(); // encodingID
                subtableOffsets[i] = (int)reader.ReadUInt32(); // subtableOffset
            }
            for (int i = 0; i < numTables; i++)
            {
                reader.Position = tableStart + subtableOffsets[i];
                int format = reader.ReadUInt16();
                if (format == 4)
                    ParseFormat4(reader, mappings);
                else if (format == 12)
                    ParseFormat12(reader, mappings);
            }
            var list = new List<CmapMapping>(mappings.Count);
            foreach (var kv in mappings)
                list.Add(new CmapMapping(kv.Key, kv.Value));
            return new CmapTable(list);
        }

        /// <summary>
        /// format 4 서브테이블을 파싱해 매핑을 수집합니다.
        /// </summary>
        private static void ParseFormat4(OpenTypeBinaryReader reader, Dictionary<int, int> mappings)
        {
            // format은 FromBytes에서 이미 읽음
            reader.ReadUInt16(); // length
            reader.ReadUInt16(); // language
            int segCount = reader.ReadUInt16() / 2; // segCountX2
            reader.ReadUInt16(); // searchRange
            reader.ReadUInt16(); // entrySelector
            reader.ReadUInt16(); // rangeShift
            var endCode = new int[segCount];
            for (int i = 0; i < segCount; i++)
                endCode[i] = reader.ReadUInt16();
            reader.ReadUInt16(); // reservedPad
            var startCode = new int[segCount];
            for (int i = 0; i < segCount; i++)
                startCode[i] = reader.ReadUInt16();
            var idDelta = new int[segCount];
            for (int i = 0; i < segCount; i++)
                idDelta[i] = reader.ReadUInt16();
            var idRangeOffset = new int[segCount];
            for (int i = 0; i < segCount; i++)
                idRangeOffset[i] = reader.ReadUInt16();
            for (int i = 0; i < segCount; i++)
            {
                if (startCode[i] == 0xFFFF)
                    continue; // notdef sentinel segment
                if (idRangeOffset[i] != 0)
                    throw new NotSupportedException("cmap format 4 with non-zero idRangeOffset is not supported.");
                for (int c = startCode[i]; c <= endCode[i]; c++)
                    mappings[c] = (c + idDelta[i]) & 0xFFFF;
            }
        }

        /// <summary>
        /// format 12 서브테이블을 파싱해 매핑을 수집합니다.
        /// </summary>
        private static void ParseFormat12(OpenTypeBinaryReader reader, Dictionary<int, int> mappings)
        {
            // format은 FromBytes에서 이미 읽음
            reader.ReadUInt16(); // reserved
            reader.ReadUInt32(); // length
            reader.ReadUInt32(); // language
            int numGroups = (int)reader.ReadUInt32();
            for (int i = 0; i < numGroups; i++)
            {
                int startCharCode = (int)reader.ReadUInt32();
                int endCharCode = (int)reader.ReadUInt32();
                int startGlyphId = (int)reader.ReadUInt32();
                for (int c = startCharCode; c <= endCharCode; c++)
                    mappings[c] = startGlyphId + (c - startCharCode);
            }
        }
    }
}
