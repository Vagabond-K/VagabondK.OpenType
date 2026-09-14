using System;
using System.Collections.Generic;
using System.IO;
using VagabondK.OpenType.CFF;
using VagabondK.OpenType.Geometry;
using VagabondK.OpenType.Tables;

namespace VagabondK.OpenType
{
    /// <summary>
    /// sfnt 형식 폰트 파일을 역파싱하여 <see cref="FontBase"/>로 재구성하는 클래스입니다.
    /// sfnt 헤더/테이블 디렉터리를 읽고, 공통 테이블과 윤곽 테이블(glyf/loca 또는 CFF)을 파싱한 후,
    /// cmap 매핑을 사용해 글리프를 재구성합니다.
    /// 이 구현은 이 라이브러리가 생성한 폰트(셀프 리더)만 지원합니다.
    /// </summary>
    static class FontReader
    {
        /// <summary>
        /// sfnt 형식 폰트 파일을 스트림에서 읽어 <see cref="FontBase"/>로 재구성합니다.
        /// sfnt version 필드로 폰트 타입을 자동 감지합니다:
        /// 0x00010000 → <see cref="TtfFont"/>, 0x4F54544F('OTTO') → <see cref="OtfFont"/>.
        /// </summary>
        /// <param name="stream">읽을 스트림입니다. 읽기(read)와 위치 지정(seek)이 가능해야 합니다.</param>
        /// <returns>재구성된 폰트입니다. (<see cref="TtfFont"/> 또는 <see cref="OtfFont"/>)</returns>
        public static FontBase Load(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var reader = new OpenTypeBinaryReader(stream);

            // sfnt 헤더
            uint sfntVersion = reader.ReadUInt32();
            int numTables = reader.ReadUInt16();
            reader.ReadUInt16(); // searchRange
            reader.ReadUInt16(); // entrySelector
            reader.ReadUInt16(); // rangeShift

            // 테이블 디렉터리 (tag → offset)
            var tableOffsets = new Dictionary<string, int>();
            for (int i = 0; i < numTables; i++)
            {
                string tag = reader.ReadTag();
                reader.ReadUInt32(); // checksum
                int offset = (int)reader.ReadUInt32();
                reader.ReadUInt32(); // length
                tableOffsets[tag] = offset;
            }

            // 폰트 타입 감지
            FontBase font;
            if (sfntVersion == 0x00010000)
                font = new TtfFont();
            else if (sfntVersion == 0x4F54544F)
                font = new OtfFont();
            else
                throw new NotSupportedException($"Unsupported sfnt version: 0x{sfntVersion:X8}.");

            // 공통 테이블 파싱
            if (tableOffsets.TryGetValue("head", out int headOffset))
                font.Head = HeadTable.FromBytes(Seek(reader, headOffset));
            if (tableOffsets.TryGetValue("hhea", out int hheaOffset))
                font.Hhea = HheaTable.FromBytes(Seek(reader, hheaOffset));
            if (tableOffsets.TryGetValue("name", out int nameOffset))
                font.Name = NameTable.FromBytes(Seek(reader, nameOffset));
            if (tableOffsets.TryGetValue("OS/2", out int os2Offset))
                font.Os2 = Os2Table.FromBytes(Seek(reader, os2Offset));
            if (tableOffsets.TryGetValue("post", out int postOffset))
                font.Post = PostTable.FromBytes(Seek(reader, postOffset));

            // maxp (numGlyphs)
            int numGlyphs = 0;
            if (tableOffsets.TryGetValue("maxp", out int maxpOffset))
                numGlyphs = MaxpTable.FromBytes(Seek(reader, maxpOffset)).NumGlyphs;
            if (numGlyphs == 0)
                throw new InvalidOperationException("The font has no glyphs.");

            // hmtx (advanceWidth, leftSideBearing)
            var advanceWidths = new int[numGlyphs];
            var leftSideBearings = new int[numGlyphs];
            if (tableOffsets.TryGetValue("hmtx", out int hmtxOffset))
            {
                var hmtx = HmtxTable.FromBytes(Seek(reader, hmtxOffset), numGlyphs);
                for (int i = 0; i < numGlyphs; i++)
                {
                    advanceWidths[i] = hmtx.AdvanceWidths[i];
                    leftSideBearings[i] = hmtx.LeftSideBearings[i];
                }
            }

            // cmap (glyphId → charCode 역매핑)
            var glyphToChar = new Dictionary<int, int>();
            if (tableOffsets.TryGetValue("cmap", out int cmapOffset))
            {
                var cmap = CmapTable.FromBytes(Seek(reader, cmapOffset));
                foreach (var mapping in cmap.Mappings)
                {
                    if (mapping.GlyphId > 0 && !glyphToChar.ContainsKey(mapping.GlyphId))
                        glyphToChar[mapping.GlyphId] = mapping.CharCode;
                }
            }

            // 윤곽 테이블 파싱
            IReadOnlyList<GlyphOutline> outlines;
            if (sfntVersion == 0x00010000)
            {
                // TTF: glyf + loca
                if (!tableOffsets.TryGetValue("glyf", out int glyfOffset) || !tableOffsets.TryGetValue("loca", out int locaOffset))
                    throw new InvalidOperationException("TTF font requires glyf and loca tables.");
                reader.Position = locaOffset;
                int[] locaOffsets = LocaTable.ReadOffsets(reader, numGlyphs, font.Head.UseLongLoca);
                outlines = GlyfTable.FromBytes(reader, glyfOffset, locaOffsets, numGlyphs).Glyphs;
            }
            else
            {
                // OTF: CFF
                if (!tableOffsets.TryGetValue("CFF ", out int cffOffset))
                    throw new InvalidOperationException("OTF font requires CFF table.");
                var cffData = CffReader.Read(reader, cffOffset);
                outlines = cffData.Outlines;
                var otf = (OtfFont)font;
                otf.CFF.TopDict = cffData.TopDict;
                otf.CFF.Private = cffData.Private;
            }

            // 글리프 재구성 (glyphId 순서로, cmap 역매핑 사용)
            font.Notdef = new Glyph(outlines[0], advanceWidths[0], leftSideBearings[0]);
            for (int glyphId = 1; glyphId < numGlyphs; glyphId++)
            {
                if (glyphToChar.TryGetValue(glyphId, out int charCode))
                    font.AddGlyph(charCode, new Glyph(outlines[glyphId], advanceWidths[glyphId], leftSideBearings[glyphId]));
            }

            return font;
        }

        /// <summary>
        /// 리더의 위치를 지정된 오프셋으로 이동합니다.
        /// </summary>
        private static OpenTypeBinaryReader Seek(OpenTypeBinaryReader reader, int offset)
        {
            reader.Position = offset;
            return reader;
        }
    }
}
