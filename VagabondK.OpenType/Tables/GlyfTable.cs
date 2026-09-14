using System;
using System.Collections.Generic;
using VagabondK.OpenType.Geometry;

namespace VagabondK.OpenType.Tables
{
    /// <summary>
    /// glyf 테이블(글리프 데이터)입니다.
    /// TrueType 윤곽 형식으로 각 글리프를 설명합니다.
    /// glyf 테이블 자체에는 테이블 헤더나 글리프 오프셋이 없으며,
    /// loca 테이블이 glyph ID 순서의 오프셋 배열을 제공합니다.
    /// 각 글리프는 simple glyph(직접 윤곽) 또는 composite glyph(다른 글리프 참조) 형식이며,
    /// 이 구현은 simple glyph(좌표 델타 + 플래그 압축)만 지원합니다.
    /// <see cref="FromBytes"/>는 <see cref="ToBytes"/>의 역으로 glyf 테이블을 윤곽 목록으로 역파싱합니다.
    /// </summary>
    class GlyfTable : OpenTypeTable
    {
        private readonly List<GlyphOutline> glyphs;
        private int[] offsets;

        /// <summary>
        /// <see cref="GlyfTable"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="glyphs">글리프 윤곽 목록입니다. (글리프 0은 .notdef)</param>
        public GlyfTable(IReadOnlyList<GlyphOutline> glyphs)
        {
            this.glyphs = new List<GlyphOutline>(glyphs);
        }

        /// <summary>
        /// 글리프 윤곽 목록을 반환합니다. (글리프 0은 .notdef)
        /// </summary>
        public IReadOnlyList<GlyphOutline> Glyphs => glyphs;

        /// <summary>
        /// 각 글리프의 glyf 테이블 내 오프셋 배열을 반환합니다. (글리프 개수 + 1)
        /// loca 테이블에 사용됩니다. <see cref="ToBytes"/>를 호출한 후에 사용할 수 있습니다.
        /// </summary>
        internal int[] Offsets => offsets;

        /// <inheritdoc />
        internal override string Tag => "glyf";

        /// <inheritdoc />
        internal override byte[] ToBytes()
        {
            var writer = new OpenTypeBinaryWriter();
            offsets = new int[glyphs.Count + 1];
            for (int i = 0; i < glyphs.Count; i++)
            {
                offsets[i] = writer.Length;
                WriteGlyph(writer, glyphs[i]);
                int glyphLength = writer.Length - offsets[i];
                if (glyphLength % 2 != 0)
                    writer.WriteUInt8(0); // glyf의 각 글리프 데이터는 2바이트 정렬되어야 합니다.
            }
            offsets[glyphs.Count] = writer.Length;
            return writer.ToArray();
        }

        /// <summary>
        /// glyf 테이블을 역파싱하여 <see cref="GlyfTable"/>을 생성합니다.
        /// <see cref="ToBytes"/>의 역으로, loca 오프셋을 사용해 각 simple glyph를
        /// 좌표 델타 + 플래그 압축에서 on-curve/off-curve 점 배열로 복원하고,
        /// 이를 MoveTo/LineTo/QuadTo 커맨드로 재구성합니다.
        /// composite glyph(numberOfContours &lt; 0)는 지원하지 않습니다.
        /// </summary>
        /// <param name="reader">glyf 테이블 데이터를 읽는 리더입니다.</param>
        /// <param name="glyfOffset">glyf 테이블의 sfnt 내 시작 오프셋입니다. (loca 오프셋은 glyf 시작 기준)</param>
        /// <param name="locaOffsets">loca에서 읽은 글리프 오프셋 배열입니다. (글리프 개수 + 1)</param>
        /// <param name="numGlyphs">글리프 개수입니다.</param>
        internal static GlyfTable FromBytes(OpenTypeBinaryReader reader, int glyfOffset, int[] locaOffsets, int numGlyphs)
        {
            var outlines = new List<GlyphOutline>(numGlyphs);
            for (int i = 0; i < numGlyphs; i++)
            {
                int start = locaOffsets[i];
                int end = locaOffsets[i + 1];
                if (start == end)
                {
                    outlines.Add(new GlyphOutline()); // 빈 윤곽
                    continue;
                }
                reader.Position = glyfOffset + start;
                outlines.Add(ReadGlyph(reader));
            }
            return new GlyfTable(outlines);
        }

        /// <summary>
        /// simple glyph 하나를 역파싱하여 <see cref="GlyphOutline"/>을 생성합니다.
        /// </summary>
        private static GlyphOutline ReadGlyph(OpenTypeBinaryReader reader)
        {
            var outline = new GlyphOutline();
            int numContours = reader.ReadInt16();
            if (numContours < 0)
                throw new NotSupportedException("Composite glyphs are not supported by the self-reader.");
            if (numContours == 0)
                return outline; // 빈 윤곽

            // xMin, yMin, xMax, yMax (읽되 윤곽 재구성에 사용되지 않음 — ToBytes에서 재계산)
            reader.ReadInt16();
            reader.ReadInt16();
            reader.ReadInt16();
            reader.ReadInt16();

            int[] endPts = new int[numContours];
            for (int i = 0; i < numContours; i++)
                endPts[i] = reader.ReadUInt16();

            int instructionLength = reader.ReadUInt16();
            if (instructionLength > 0)
                reader.ReadBytes(instructionLength); // instructions는 무시

            int numPoints = endPts[numContours - 1] + 1;
            byte[] flags = reader.ReadBytes(numPoints);

            // X 좌표 복원 (이전 점 기준 누적 델타)
            int[] xs = new int[numPoints];
            int x = 0;
            for (int i = 0; i < numPoints; i++)
            {
                byte flag = flags[i];
                if ((flag & 0x02) != 0) // X_SHORT_VECTOR
                {
                    int v = reader.ReadUInt8();
                    if ((flag & 0x10) == 0) v = -v; // X_IS_SAME_OR_POSITIVE
                    x += v;
                }
                else if ((flag & 0x10) == 0) // X_IS_SAME 아님
                {
                    x += reader.ReadInt16();
                }
                xs[i] = x;
            }

            // Y 좌표 복원 (이전 점 기준 누적 델타)
            int[] ys = new int[numPoints];
            int y = 0;
            for (int i = 0; i < numPoints; i++)
            {
                byte flag = flags[i];
                if ((flag & 0x04) != 0) // Y_SHORT_VECTOR
                {
                    int v = reader.ReadUInt8();
                    if ((flag & 0x20) == 0) v = -v; // Y_IS_SAME_OR_POSITIVE
                    y += v;
                }
                else if ((flag & 0x20) == 0) // Y_IS_SAME 아님
                {
                    y += reader.ReadInt16();
                }
                ys[i] = y;
            }

            // contour별 커맨드 재구성
            // 라이터(EnumerateTtfPoints)는 on-curve 점 사이에 off-curve 점을 최대 1개만 생성하므로:
            //   첫 점(on-curve) → MoveTo, on-curve → LineTo, off-curve → QuadTo(다음 on-curve까지)
            int contourStart = 0;
            for (int c = 0; c < numContours; c++)
            {
                int contourEnd = endPts[c];
                var contour = outline.BeginContour();
                contour.MoveTo(xs[contourStart], ys[contourStart]);
                int i = contourStart + 1;
                while (i <= contourEnd)
                {
                    if ((flags[i] & 0x01) != 0) // ON_CURVE_POINT
                    {
                        contour.LineTo(xs[i], ys[i]);
                        i++;
                    }
                    else
                    {
                        // off-curve: 제어점, 다음 점은 on-curve 종점
                        contour.QuadTo(xs[i], ys[i], xs[i + 1], ys[i + 1]);
                        i += 2;
                    }
                }
                contourStart = contourEnd + 1;
            }

            return outline;
        }

        private void WriteGlyph(OpenTypeBinaryWriter writer, GlyphOutline glyph)
        {
            var allPoints = new List<GlyphPoint>();
            var endPts = new List<int>();
            foreach (var contour in glyph.Contours)
            {
                allPoints.AddRange(contour.EnumerateTtfPoints());
                endPts.Add(allPoints.Count - 1);
            }

            int numContours = glyph.Contours.Count;
            int numPoints = allPoints.Count;

            // TTF glyf는 정수 전용이므로 좌표를 반올림합니다.
            int[] xs = new int[numPoints];
            int[] ys = new int[numPoints];
            for (int i = 0; i < numPoints; i++)
            {
                xs[i] = (int)Math.Round(allPoints[i].X);
                ys[i] = (int)Math.Round(allPoints[i].Y);
            }

            int xMin = 0, yMin = 0, xMax = 0, yMax = 0;
            if (numPoints > 0)
            {
                xMin = int.MaxValue; yMin = int.MaxValue;
                xMax = int.MinValue; yMax = int.MinValue;
                for (int i = 0; i < numPoints; i++)
                {
                    if (xs[i] < xMin) xMin = xs[i];
                    if (xs[i] > xMax) xMax = xs[i];
                    if (ys[i] < yMin) yMin = ys[i];
                    if (ys[i] > yMax) yMax = ys[i];
                }
            }

            // Glyph Header: numberOfContours(0 이상이면 simple glyph), xMin, yMin, xMax, yMax
            writer.WriteInt16((short)numContours);
            writer.WriteInt16((short)xMin);
            writer.WriteInt16((short)yMin);
            writer.WriteInt16((short)xMax);
            writer.WriteInt16((short)yMax);

            // contour가 0이면 헤더 이후 추가 데이터가 필요 없습니다.
            if (numContours == 0)
                return;

            // endPtsOfContours: 각 contour의 마지막 점 인덱스(0-based, 증가 순서)
            foreach (int end in endPts)
                writer.WriteUInt16((ushort)end);

            writer.WriteUInt16(0); // instructionLength (0 = instructions 없음)
            WriteCoordinates(writer, allPoints, xs, ys);
        }

        /// <summary>
        /// 좌표를 flags 배열과 x/y 좌표 배열로 직렬화합니다.
        /// glyf의 simple glyph는 점을 절대 좌표가 아닌 이전 점에 대한 델타로 저장하며,
        /// flags 비트에 따라 좌표가 1바이트(짧은 벡터) 또는 2바이트(부호 있는 델타)로 저장됩니다.
        /// </summary>
        private void WriteCoordinates(OpenTypeBinaryWriter writer, List<GlyphPoint> points, int[] xs, int[] ys)
        {
            int n = points.Count;
            int[] dx = new int[n];
            int[] dy = new int[n];
            for (int i = 0; i < n; i++)
            {
                // 첫 점은 (0,0) 기준, 나머지 점은 이전 점 기준 델타
                dx[i] = xs[i] - (i > 0 ? xs[i - 1] : 0);
                dy[i] = ys[i] - (i > 0 ? ys[i - 1] : 0);
            }

            byte[] flags = new byte[n];
            for (int i = 0; i < n; i++)
            {
                byte flag = 0;
                if (points[i].IsOnCurve) flag |= 0x01; // ON_CURVE_POINT
                if (dx[i] == 0)
                    flag |= 0x10; // X_IS_SAME (이전 점과 x 좌표 동일)
                else if (dx[i] >= -255 && dx[i] <= 255)
                {
                    flag |= 0x02; // X_SHORT_VECTOR (1바이트)
                    if (dx[i] > 0) flag |= 0x10; // X_IS_SAME_OR_POSITIVE (양수 부호)
                }
                if (dy[i] == 0)
                    flag |= 0x20; // Y_IS_SAME (이전 점과 y 좌표 동일)
                else if (dy[i] >= -255 && dy[i] <= 255)
                {
                    flag |= 0x04; // Y_SHORT_VECTOR (1바이트)
                    if (dy[i] > 0) flag |= 0x20; // Y_IS_SAME_OR_POSITIVE (양수 부호)
                }
                flags[i] = flag;
            }

            // flags 배열 기록
            foreach (byte flag in flags)
                writer.WriteUInt8(flag);

            // x 좌표 기록: X_SHORT_VECTOR이면 1바이트(절대값), X_IS_SAME가 아니면 2바이트(부호 있는 델타)
            for (int j = 0; j < n; j++)
            {
                byte flag = flags[j];
                if ((flag & 0x02) != 0)
                    writer.WriteUInt8((byte)(dx[j] > 0 ? dx[j] : -dx[j]));
                else if ((flag & 0x10) == 0)
                    writer.WriteInt16((short)dx[j]);
            }

            // y 좌표 기록: Y_SHORT_VECTOR이면 1바이트(절대값), Y_IS_SAME가 아니면 2바이트(부호 있는 델타)
            for (int j = 0; j < n; j++)
            {
                byte flag = flags[j];
                if ((flag & 0x04) != 0)
                    writer.WriteUInt8((byte)(dy[j] > 0 ? dy[j] : -dy[j]));
                else if ((flag & 0x20) == 0)
                    writer.WriteInt16((short)dy[j]);
            }
        }

        /// <summary>
        /// TTF(glyf) 직렬화 시의 점 수를 계산합니다.
        /// 3차 베지어 세그먼트는 2개의 2차 베지어로 변환되어 점 수가 늘어납니다.
        /// maxp의 maxPoints 계산에 사용됩니다.
        /// </summary>
        internal static int CountTtfPoints(GlyphOutline outline)
        {
            int count = 0;
            foreach (var contour in outline.Contours)
            {
                count += contour.EnumerateTtfPoints().Count;
            }
            return count;
        }
    }
}
