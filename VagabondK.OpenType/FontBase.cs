using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VagabondK.OpenType.Geometry;
using VagabondK.OpenType.Tables;

namespace VagabondK.OpenType
{
    /// <summary>
    /// sfnt 형식 폰트 파일 생성의 공통 로직을 제공하는 추상 베이스 클래스입니다.
    /// TTF(glyf/loca)와 OTF(CFF)는 sfnt 디렉터리 조립, checksum 계산,
    /// head/hhea/OS/2 메트릭 자동 계산 등이 동일하므로 이 클래스에서 공유합니다.
    /// 윤곽 테이블(glyf/loca 또는 CFF), maxp 버전, sfnt version은
    /// 파생 클래스가 구현하는 추상 멤버로 구분됩니다.
    /// 문자 코드에서 글리프로의 매핑을 dictionary처럼 접근할 수 있습니다:
    /// 인덱서(<c>font[charCode]</c>), <see cref="ContainsGlyph"/>, <see cref="TryGetGlyph"/>,
    /// <see cref="RemoveGlyph"/>, 그리고 <c>foreach</c>로 전체 매핑을 열거할 수 있습니다.
    /// </summary>
    public abstract class FontBase : IEnumerable<KeyValuePair<int, Glyph>>
    {
        private const uint CheckSumTarget = 0xB1B0AFBA;

        private Glyph notdef = CreateDefaultNotdef();
        private readonly List<Glyph> glyphs;
        private readonly Dictionary<int, int> charToGlyph;

        /// <summary>
        /// head 테이블입니다. 글리프에서 자동 계산되는 바운딩 박스 값을 제외하고 속성을 조작할 수 있습니다.
        /// </summary>
        public HeadTable Head { get; set; }

        /// <summary>
        /// hhea 테이블입니다. 글리프에서 자동 계산되는 메트릭 값을 제외하고 속성을 조작할 수 있습니다.
        /// </summary>
        public HheaTable Hhea { get; set; }

        /// <summary>
        /// OS/2 테이블입니다. 글리프에서 자동 계산되는 문자 범위 값을 제외하고 속성을 조작할 수 있습니다.
        /// </summary>
        public Os2Table Os2 { get; set; }

        /// <summary>
        /// name 테이블입니다. 폰트 이름 및 메타데이터 속성을 조작할 수 있습니다.
        /// </summary>
        public NameTable Name { get; set; }

        /// <summary>
        /// post 테이블입니다. 밑줄 및 이탤릭 각도 속성을 조작할 수 있습니다.
        /// </summary>
        public PostTable Post { get; set; }

        /// <summary>
        /// .notdef 글리프(glyph 0)입니다.
        /// .notdef은 "not defined"의 약어로, cmap에 매핑되지 않은 문자가
        /// 렌더링될 때 사용되는 글리프입니다. 매핑되지 않은 문자는 전부
        /// 이 글리프로 표시되므로, advance width가 0이면 문자들이 같은
        /// 위치에 겹쳐 표시됩니다.
        /// 기본값은 missing glyph로 통용되는 테두리만 있는 사각형입니다.
        /// </summary>
        public Glyph Notdef
        {
            get => notdef;
            set => notdef = value;
        }

        /// <summary>
        /// 기본 .notdef 글리프를 생성합니다.
        /// missing glyph로 통용되는 테두리만 있는 사각형(hollow box)입니다.
        /// 바깥 contour(CCW)와 안쪽 contour(CW) 2개로, nonzero fill rule에
        /// 의해 테두리만 채워집니다. 박스 (100,0)~(400,700), 테두리 두께 25,
        /// advance width 500입니다.
        /// </summary>
        private static Glyph CreateDefaultNotdef()
        {
            var outline = new GlyphOutline();
            // 바깥 contour (CCW)
            var outer = outline.BeginContour();
            outer.MoveTo(100, 0);
            outer.LineTo(400, 0);
            outer.LineTo(400, 700);
            outer.LineTo(100, 700);
            // 안쪽 contour (CW) → nonzero fill rule로 구멍
            var inner = outline.BeginContour();
            inner.MoveTo(125, 25);
            inner.LineTo(125, 675);
            inner.LineTo(375, 675);
            inner.LineTo(375, 25);
            return new Glyph(outline, 500);
        }

        /// <summary>
        /// 전체 글리프 목록입니다. (glyph ID 순서, 0=.notdef)
        /// .notdef와 추가된 글리프를 결합하여 반환합니다.
        /// 반환된 <see cref="Glyph"/>는 실제 인스턴스이므로, <see cref="Glyph.AdvanceWidth"/>/
        /// <see cref="Glyph.LeftSideBearing"/> 또는 <see cref="Glyph.Outline"/>을 수정하면
        /// 다음 <see cref="ToBytes"/>에 반영됩니다.
        /// </summary>
        public IReadOnlyList<Glyph> Glyphs
        {
            get
            {
                var list = new List<Glyph>(glyphs.Count + 1);
                list.Add(notdef);
                list.AddRange(glyphs);
                return list;
            }
        }

        /// <summary>
        /// 문자 코드에서 glyph ID로의 매핑입니다.
        /// </summary>
        protected IReadOnlyDictionary<int, int> CharToGlyph => charToGlyph;

        /// <summary>
        /// <see cref="FontBase"/>의 새 인스턴스를 생성합니다.
        /// 글리프 0은 .notdef(테두리만 있는 사각형) 글리프로 자동 생성됩니다.
        /// </summary>
        protected FontBase()
        {
            glyphs = new List<Glyph>();
            charToGlyph = new Dictionary<int, int>();
            Head = new HeadTable();
            Hhea = new HheaTable();
            Os2 = new Os2Table();
            Name = new NameTable();
            Post = new PostTable();
        }

        /// <summary>
        /// sfnt version 필드에 기록할 값입니다.
        /// TTF는 0x00010000, OTF는 0x4F54544F('OTTO')입니다.
        /// </summary>
        protected abstract uint SfntVersion { get; }

        /// <summary>
        /// 윤곽 테이블을 생성합니다.
        /// TTF는 glyf/loca, OTF는 CFF 테이블을 반환합니다.
        /// </summary>
        /// <param name="xMin">전체 폰트 바운딩 박스의 최소 X 좌표입니다.</param>
        /// <param name="yMin">전체 폰트 바운딩 박스의 최소 Y 좌표입니다.</param>
        /// <param name="xMax">전체 폰트 바운딩 박스의 최대 X 좌표입니다.</param>
        /// <param name="yMax">전체 폰트 바운딩 박스의 최대 Y 좌표입니다.</param>
        /// <returns>sfnt에 포함할 윤곽 테이블 목록입니다.</returns>
        protected abstract List<OpenTypeTable> CreateOutlineTables(int xMin, int yMin, int xMax, int yMax);

        /// <summary>
        /// maxp 테이블을 생성합니다.
        /// TTF는 version 1.0(maxPoints/maxContours 포함), OTF는 version 0.5(numGlyphs만)입니다.
        /// </summary>
        internal abstract MaxpTable CreateMaxpTable(int numGlyphs, int maxPoints, int maxContours);

        /// <summary>
        /// 문자 코드에 해당하는 글리프를 추가합니다.
        /// 글리프 0은 .notdef(누락 문자 글리프)로 자동 생성되며,
        /// cmap에 매핑되지 않은 문자는 이 .notdef 글리프로 렌더링됩니다.
        /// </summary>
        /// <param name="charCode">유니코드 코드포인트입니다. (0x0000–0x10FFFF)</param>
        /// <param name="glyph">추가할 글리프입니다.</param>
        /// <exception cref="ArgumentException">문자 코드에 이미 글리프가 정의된 경우.</exception>
        /// <exception cref="ArgumentOutOfRangeException">문자 코드가 0x0000–0x10FFFF 범위를 벗어난 경우.</exception>
        public void AddGlyph(int charCode, Glyph glyph)
        {
            if (charCode < 0 || charCode > 0x10FFFF)
                throw new ArgumentOutOfRangeException(nameof(charCode), $"The character code U+{charCode:X8} is out of range (0x0000-0x10FFFF).");
            if (charToGlyph.ContainsKey(charCode))
                throw new ArgumentException($"The glyph for character U+{charCode:X4} is already defined.", nameof(charCode));
            glyphs.Add(glyph);
            charToGlyph[charCode] = glyphs.Count;
        }

        /// <summary>
        /// 문자 코드에 해당하는 글리프를 가져오거나 설정합니다.
        /// get: 해당 글리프를 반환하며, 매핑되지 않은 문자는 null입니다.
        /// set: 글리프를 추가하거나 교체합니다(upsert). 이미 매핑된 문자 코드는
        /// 그 자리에서 글리프만 교체되어 glyph ID가 유지되고, 새로운 문자 코드는
        /// 마지막 glyph ID로 추가됩니다.
        /// 반환된 <see cref="Glyph"/>는 실제 인스턴스이므로, <see cref="Glyph.AdvanceWidth"/>/
        /// <see cref="Glyph.LeftSideBearing"/> 또는 <see cref="Glyph.Outline"/>을 수정하면
        /// 다음 <see cref="ToBytes"/>에 반영됩니다.
        /// </summary>
        /// <param name="charCode">유니코드 코드포인트입니다. (0x0000–0x10FFFF)</param>
        /// <exception cref="ArgumentOutOfRangeException">set 시 문자 코드가 0x0000–0x10FFFF 범위를 벗어난 경우.</exception>
        /// <exception cref="ArgumentNullException">set 시 glyph가 null인 경우.</exception>
        public Glyph this[int charCode]
        {
            get
            {
                if (charToGlyph.TryGetValue(charCode, out int glyphId))
                    return glyphs[glyphId - 1];
                return null;
            }
            set
            {
                if (charCode < 0 || charCode > 0x10FFFF)
                    throw new ArgumentOutOfRangeException(nameof(charCode), $"The character code U+{charCode:X8} is out of range (0x0000-0x10FFFF).");
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (charToGlyph.TryGetValue(charCode, out int glyphId))
                    glyphs[glyphId - 1] = value;
                else
                {
                    glyphs.Add(value);
                    charToGlyph[charCode] = glyphs.Count;
                }
            }
        }

        /// <summary>
        /// 문자 코드가 글리프에 매핑되어 있는지 확인합니다.
        /// </summary>
        /// <param name="charCode">유니코드 코드포인트입니다.</param>
        /// <returns>매핑되어 있으면 true, 아니면 false입니다.</returns>
        public bool ContainsGlyph(int charCode) => charToGlyph.ContainsKey(charCode);

        /// <summary>
        /// 문자 코드에 해당하는 글리프를 시도하여 가져옵니다.
        /// </summary>
        /// <param name="charCode">유니코드 코드포인트입니다.</param>
        /// <param name="glyph">매핑된 글리프입니다. 매핑되지 않으면 null입니다.</param>
        /// <returns>매핑되어 있으면 true, 아니면 false입니다.</returns>
        public bool TryGetGlyph(int charCode, out Glyph glyph)
        {
            if (charToGlyph.TryGetValue(charCode, out int glyphId))
            {
                glyph = glyphs[glyphId - 1];
                return true;
            }
            glyph = null;
            return false;
        }

        /// <summary>
        /// 문자 코드에 해당하는 글리프를 제거합니다.
        /// glyph ID는 0..n-1로 연속해야 하므로, 제거된 글리프 이후의
        /// glyph ID는 1씩 감소합니다.
        /// </summary>
        /// <param name="charCode">유니코드 코드포인트입니다.</param>
        /// <returns>제거된 글리프가 있으면 true, 매핑이 없으면 false입니다.</returns>
        public bool RemoveGlyph(int charCode)
        {
            if (!charToGlyph.TryGetValue(charCode, out int glyphId))
                return false;
            glyphs.RemoveAt(glyphId - 1);
            charToGlyph.Remove(charCode);
            // 제거된 글리프 이후의 glyph ID를 1씩 재조정합니다.
            var updates = new List<KeyValuePair<int, int>>();
            foreach (var kv in charToGlyph)
                if (kv.Value > glyphId)
                    updates.Add(new KeyValuePair<int, int>(kv.Key, kv.Value - 1));
            foreach (var update in updates)
                charToGlyph[update.Key] = update.Value;
            return true;
        }

        /// <summary>
        /// 문자 코드에서 글리프로의 전체 매핑을 charCode 오름차순으로 열거합니다.
        /// </summary>
        public IEnumerator<KeyValuePair<int, Glyph>> GetEnumerator()
        {
            foreach (var kv in charToGlyph.OrderBy(k => k.Key))
                yield return new KeyValuePair<int, Glyph>(kv.Key, glyphs[kv.Value - 1]);
        }

        /// <summary>
        /// <see cref="IEnumerable"/>의 비제네릭 열거자를 반환합니다.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// sfnt 형식 폰트 파일 바이트 배열을 생성합니다.
        /// 윤곽/hmtx/cmap/maxp 테이블을 생성하고, 글리프에서 자동 계산된 값을
        /// head/hhea/OS/2 테이블에 반영한 후, sfnt 테이블 디렉터리와 함께 조립합니다.
        /// head의 checkSumAdjustment는 0xB1B0AFBA - 전체 폰트 checksum으로 계산됩니다.
        /// </summary>
        /// <returns>sfnt 형식 폰트 파일 바이트 배열입니다.</returns>
        public byte[] ToBytes()
        {
            var allGlyphs = Glyphs;
            int numGlyphs = allGlyphs.Count;
            int xMin = 0, yMin = 0, xMax = 0, yMax = 0;
            int maxPoints = 0, maxContours = 0;
            int advanceWidthMax = 0, minLeftSideBearing = 0, minRightSideBearing = 0, xMaxExtent = 0;
            bool first = true;

            var advanceWidths = new List<int>();
            var leftSideBearings = new List<int>();

            foreach (var glyph in allGlyphs)
            {
                int glyphPoints = 0;
                int glyphContours = glyph.Outline.Contours.Count;
                double gxMin = 0, gyMin = 0, gxMax = 0, gyMax = 0;
                bool hasPoint = false;

                foreach (var contour in glyph.Outline.Contours)
                {
                    // glyf에 실제로 저장되는 점 수(3차 베지어는 2차로 확장)
                    glyphPoints += contour.EnumerateTtfPoints().Count;
                    // 바운딩 박스: 곡선 자체의 극값(도함수 0인 t에서 평가)으로 정확히 계산
                    if (contour.ComputeBoundingBox(out double cxMin, out double cyMin, out double cxMax, out double cyMax))
                    {
                        if (!hasPoint)
                        {
                            gxMin = cxMin; gyMin = cyMin;
                            gxMax = cxMax; gyMax = cyMax;
                            hasPoint = true;
                        }
                        else
                        {
                            if (cxMin < gxMin) gxMin = cxMin;
                            if (cyMin < gyMin) gyMin = cyMin;
                            if (cxMax > gxMax) gxMax = cxMax;
                            if (cyMax > gyMax) gyMax = cyMax;
                        }
                    }
                }

                // head의 바운딩 박스는 FWORD(16비트 정수)이므로,
                // min은 내림(Floor), max는 올림(Ceiling)하여 모든 곡선 극값이 박스에 포함되도록 합니다.
                if (hasPoint)
                {
                    int igxMin = (int)Math.Floor(gxMin);
                    int igyMin = (int)Math.Floor(gyMin);
                    int igxMax = (int)Math.Ceiling(gxMax);
                    int igyMax = (int)Math.Ceiling(gyMax);

                    if (first)
                    {
                        xMin = igxMin; yMin = igyMin; xMax = igxMax; yMax = igyMax;
                        first = false;
                    }
                    else
                    {
                        if (igxMin < xMin) xMin = igxMin;
                        if (igyMin < yMin) yMin = igyMin;
                        if (igxMax > xMax) xMax = igxMax;
                        if (igyMax > yMax) yMax = igyMax;
                    }
                }

                maxPoints = Math.Max(maxPoints, glyphPoints);
                maxContours = Math.Max(maxContours, glyphContours);

                advanceWidths.Add(glyph.AdvanceWidth);
                leftSideBearings.Add(glyph.LeftSideBearing);
                advanceWidthMax = Math.Max(advanceWidthMax, glyph.AdvanceWidth);

                if (hasPoint)
                {
                    int width = (int)Math.Round(gxMax - gxMin);
                    int rightSideBearing = glyph.AdvanceWidth - glyph.LeftSideBearing - width;
                    minLeftSideBearing = Math.Min(minLeftSideBearing, glyph.LeftSideBearing);
                    minRightSideBearing = Math.Min(minRightSideBearing, rightSideBearing);
                    xMaxExtent = Math.Max(xMaxExtent, glyph.LeftSideBearing + width);
                }
            }

            if (first)
            {
                xMin = 0; yMin = 0; xMax = 0; yMax = 0;
            }

            // 글리프에서 자동 계산된 값을 설정 테이블에 반영합니다.
            Head.XMin = xMin;
            Head.YMin = yMin;
            Head.XMax = xMax;
            Head.YMax = yMax;

            Hhea.AdvanceWidthMax = advanceWidthMax;
            Hhea.MinLeftSideBearing = minLeftSideBearing;
            Hhea.MinRightSideBearing = minRightSideBearing;
            Hhea.XMaxExtent = xMaxExtent;
            Hhea.NumberOfHMetrics = numGlyphs;

            var maxpTable = CreateMaxpTable(numGlyphs, maxPoints, maxContours);
            var hmtxTable = new HmtxTable(advanceWidths, leftSideBearings);

            var mappings = charToGlyph
                .Select(kv => new CmapTable.CmapMapping(kv.Key, kv.Value))
                .ToList();
            var cmapTable = new CmapTable(mappings);

            // usFirstCharIndex/usLastCharIndex는 uint16 필드로 BMP(U+0000–U+FFFF)만 표현 가능.
            // non-BMP 매핑은 format 12 subtable로만 표현되므로 BMP 매핑 기준으로 계산한다.
            // BMP 매핑이 없으면 0xFFFF sentinel을 사용한다 (NotoColorEmoji 관례).
            var bmpMappings = mappings.Where(m => m.CharCode <= 0xFFFF).ToList();
            Os2.UsFirstCharIndex = bmpMappings.Count > 0 ? bmpMappings.Min(m => m.CharCode) : 0xFFFF;
            Os2.UsLastCharIndex = bmpMappings.Count > 0 ? bmpMappings.Max(m => m.CharCode) : 0xFFFF;

            uint[] unicodeRanges = Os2Table.ComputeUnicodeRanges(mappings.Select(m => m.CharCode).ToList());
            Os2.UnicodeRange1 = unicodeRanges[0];
            Os2.UnicodeRange2 = unicodeRanges[1];
            Os2.UnicodeRange3 = unicodeRanges[2];
            Os2.UnicodeRange4 = unicodeRanges[3];

            var tables = new List<OpenTypeTable>
            {
                Head, Hhea, maxpTable, hmtxTable, cmapTable,
                Name, Os2, Post
            };
            tables.AddRange(CreateOutlineTables(xMin, yMin, xMax, yMax));

            // sfnt 테이블 디렉터리는 태그(tag)의 ASCII 순서로 정렬되어야 합니다.
            tables.Sort((a, b) => string.CompareOrdinal(a.Tag, b.Tag));
            int headIndex = tables.FindIndex(t => t is HeadTable);

            var tableBytes = new byte[tables.Count][];
            for (int i = 0; i < tables.Count; i++)
                tableBytes[i] = tables[i].ToBytes();

            var checksums = new uint[tables.Count];
            uint total = 0;
            for (int i = 0; i < tables.Count; i++)
            {
                checksums[i] = ComputeChecksum(tableBytes[i]);
                total += checksums[i];
            }

            int numTables = tables.Count;
            int searchRange = 1;
            int entrySelector = 0;
            while (searchRange * 2 <= numTables)
            {
                searchRange *= 2;
                entrySelector++;
            }
            searchRange *= 16; // 바이트 단위
            int rangeShift = numTables * 16 - searchRange;

            int dataOffset = 12 + numTables * 16;
            var tableOffsets = new int[numTables];
            int offset = dataOffset;
            for (int i = 0; i < numTables; i++)
            {
                tableOffsets[i] = offset;
                offset += (tableBytes[i].Length + 3) & ~3;
            }

            // sfnt 헤더 + 테이블 디렉터리 바이트를 구성합니다.
            // head의 checkSumAdjustment는 아직 0이므로, 디렉터리의 head checksum은 0 기준입니다.
            var headerWriter = new OpenTypeBinaryWriter(12 + numTables * 16);
            headerWriter.WriteUInt32(SfntVersion);
            headerWriter.WriteUInt16((ushort)numTables);
            headerWriter.WriteUInt16((ushort)searchRange);
            headerWriter.WriteUInt16((ushort)entrySelector);
            headerWriter.WriteUInt16((ushort)rangeShift);
            for (int i = 0; i < numTables; i++)
            {
                headerWriter.WriteTag(tables[i].Tag);
                headerWriter.WriteUInt32(checksums[i]);
                headerWriter.WriteUInt32((uint)tableOffsets[i]);
                headerWriter.WriteUInt32((uint)tableBytes[i].Length);
            }
            byte[] headerAndDirectory = headerWriter.ToArray();

            // 전체 checksum = 테이블 checksum 합 + (sfnt 헤더 + 디렉터리) checksum
            total += ComputeChecksum(headerAndDirectory);

            Head.CheckSumAdjustment = (int)(CheckSumTarget - total);
            tableBytes[headIndex] = Head.ToBytes();
            // checksums[headIndex]는 0 기준 head checksum을 유지합니다 (디렉터리 entry용)

            var writer = new OpenTypeBinaryWriter(offset);
            writer.WriteBytes(headerAndDirectory);

            foreach (byte[] tableData in tableBytes)
            {
                writer.WriteBytes(tableData);
                int padding = (4 - (tableData.Length % 4)) % 4;
                for (int j = 0; j < padding; j++)
                    writer.WriteUInt8(0);
            }

            return writer.ToArray();
        }

        /// <summary>
        /// sfnt 형식 폰트 파일을 스트림에 저장합니다.
        /// <see cref="ToBytes"/>와 동일한 바이트를 기록합니다.
        /// 스트림의 소유권은 호출자에게 있으며, 이 메서드는 스트림을 닫지 않습니다.
        /// </summary>
        /// <param name="stream">기록할 스트림입니다.</param>
        public void Save(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            byte[] bytes = ToBytes();
            stream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// sfnt 형식 폰트 파일을 파일로 저장합니다.
        /// </summary>
        /// <param name="path">파일 경로입니다.</param>
        public void Save(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            byte[] bytes = ToBytes();
            File.WriteAllBytes(path, bytes);
        }

        /// <summary>
        /// sfnt 형식 폰트 파일을 스트림에서 읽어 <see cref="FontBase"/>로 재구성합니다.
        /// sfnt version 필드로 폰트 타입을 자동 감지합니다:
        /// 0x00010000 → <see cref="TtfFont"/>, 0x4F54544F('OTTO') → <see cref="OtfFont"/>.
        /// 이 구현은 이 라이브러리가 생성한 폰트(셀프 리더)만 지원합니다.
        /// </summary>
        /// <param name="stream">읽을 스트림입니다. 읽기(read)와 위치 지정(seek)이 가능해야 합니다.</param>
        /// <returns>재구성된 폰트입니다. (<see cref="TtfFont"/> 또는 <see cref="OtfFont"/>)</returns>
        public static FontBase Load(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            return FontReader.Load(stream);
        }

        /// <summary>
        /// sfnt 형식 폰트 파일을 파일에서 읽어 <see cref="FontBase"/>로 재구성합니다.
        /// <see cref="Load(Stream)"/>과 동일하며, 파일 확장자가 아닌 sfnt version 필드로 타입을 감지합니다.
        /// </summary>
        /// <param name="path">파일 경로입니다.</param>
        /// <returns>재구성된 폰트입니다. (<see cref="TtfFont"/> 또는 <see cref="OtfFont"/>)</returns>
        public static FontBase Load(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            using (var stream = File.OpenRead(path))
            {
                return FontReader.Load(stream);
            }
        }

        /// <summary>
        /// 폰트 상태를 검사하여 오류와 경고 목록을 반환합니다.
        /// 글리프/테이블 설정이 모두 완료된 후(상태 단위 불변식)에 호출해야 합니다.
        /// 값 단위(설정 시점) 검사는 이미 생성자/속성에서 fail-fast로 처리됩니다.
        /// 이 메서드는 파일을 수정하지 않으며, 검사만 수행합니다.
        /// </summary>
        /// <returns>검사 결과 목록입니다. 빈 목록이면 오류/경고 없음.</returns>
        public IReadOnlyList<ValidationResult> Validate()
        {
            var results = new List<ValidationResult>();
            var allGlyphs = Glyphs;
            int numGlyphs = allGlyphs.Count;

            // glyph ID -> char code 역참조
            var glyphToChar = new Dictionary<int, int>();
            foreach (var kv in CharToGlyph)
                glyphToChar[kv.Value] = kv.Key;

            double fontYMax = 0, fontYMin = 0;
            bool hasAnyPoint = false;
            int firstAdvance = 0;
            bool advanceSet = false;
            bool advanceUniform = true;

            for (int i = 0; i < numGlyphs; i++)
            {
                var glyph = allGlyphs[i];
                string name = GetGlyphDisplayName(i, glyphToChar);

                // advance width >= 0
                if (glyph.AdvanceWidth < 0)
                    results.Add(new ValidationResult(ValidationLevel.Warning,
                        $"Glyph {name} (id {i}) has a negative advance width ({glyph.AdvanceWidth})."));

                // advance 균일성 (isFixedPitch 검사용)
                if (!advanceSet) { firstAdvance = glyph.AdvanceWidth; advanceSet = true; }
                else if (glyph.AdvanceWidth != firstAdvance) advanceUniform = false;

                // lsb = xMin 일치 + 전체 바운딩 박스
                if (ComputeGlyphBoundingBox(glyph, out double gxMin, out double gyMin, out _, out double gyMax))
                {
                    int expectedLsb = (int)Math.Round(gxMin);
                    if (glyph.LeftSideBearing != expectedLsb)
                        results.Add(new ValidationResult(ValidationLevel.Error,
                            $"Glyph {name} (id {i}): left side bearing ({glyph.LeftSideBearing}) does not match outline xMin ({expectedLsb})."));

                    if (!hasAnyPoint) { fontYMax = gyMax; fontYMin = gyMin; hasAnyPoint = true; }
                    else
                    {
                        if (gyMax > fontYMax) fontYMax = gyMax;
                        if (gyMin < fontYMin) fontYMin = gyMin;
                    }
                }
            }

            // WinAscent >= yMax
            if (hasAnyPoint && Os2.WinAscent < (int)Math.Ceiling(fontYMax))
                results.Add(new ValidationResult(ValidationLevel.Warning,
                    $"usWinAscent ({Os2.WinAscent}) is less than the maximum glyph yMax ({(int)Math.Ceiling(fontYMax)}); glyphs may be clipped on Windows."));

            // WinDescent >= |yMin|
            if (hasAnyPoint && Os2.WinDescent < (int)Math.Ceiling(-fontYMin))
                results.Add(new ValidationResult(ValidationLevel.Warning,
                    $"usWinDescent ({Os2.WinDescent}) is less than the maximum glyph descent ({(int)Math.Ceiling(-fontYMin)}); glyphs may be clipped on Windows."));

            // fsSelection Italic/Regular 상호 배타
            bool fsItalic = (Os2.FsSelection & FontSelection.Italic) != 0;
            bool fsRegular = (Os2.FsSelection & FontSelection.Regular) != 0;
            if (fsItalic && fsRegular)
                results.Add(new ValidationResult(ValidationLevel.Error,
                    "fsSelection has both Italic and Regular bits set; they are mutually exclusive."));

            // macStyle Italic <-> fsSelection Italic 일치
            bool macItalic = (Head.MacStyle & MacStyleFlag.Italic) != 0;
            if (macItalic != fsItalic)
                results.Add(new ValidationResult(ValidationLevel.Warning,
                    $"head macStyle Italic bit ({(macItalic ? 1 : 0)}) does not match OS/2 fsSelection Italic bit ({(fsItalic ? 1 : 0)})."));

            // isFixedPitch <-> advance 균일
            if (Post.IsFixedPitch && !advanceUniform)
                results.Add(new ValidationResult(ValidationLevel.Error,
                    "post isFixedPitch is set but advance widths are not uniform across glyphs."));

            // hhea <-> OS/2 typo metrics
            if (Hhea.Ascender != Os2.Ascender || Hhea.Descender != Os2.Descender || Hhea.LineGap != Os2.TypoLineGap)
                results.Add(new ValidationResult(ValidationLevel.Warning,
                    $"hhea ascender/descender/lineGap ({Hhea.Ascender}/{Hhea.Descender}/{Hhea.LineGap}) do not match OS/2 sTypoAscender/sTypoDescender/sTypoLineGap ({Os2.Ascender}/{Os2.Descender}/{Os2.TypoLineGap})."));

            // ItalicAngle 범위
            if (Post.ItalicAngle < -90 || Post.ItalicAngle > 90)
                results.Add(new ValidationResult(ValidationLevel.Warning,
                    $"post italicAngle ({Post.ItalicAngle}) is outside the typical range (-90..90)."));

            // Version 형식
            if (!string.IsNullOrEmpty(Name.Version) && !Name.Version.StartsWith("Version "))
                results.Add(new ValidationResult(ValidationLevel.Warning,
                    $"name version '{Name.Version}' does not follow the 'Version x.y' convention."));

            // caretSlopeRise > 0
            if (Hhea.CaretSlopeRise <= 0)
                results.Add(new ValidationResult(ValidationLevel.Warning,
                    $"hhea caretSlopeRise ({Hhea.CaretSlopeRise}) must be greater than 0."));

            // 1글리프 폰트
            if (numGlyphs == 1)
                results.Add(new ValidationResult(ValidationLevel.Warning,
                    "Font contains only the .notdef glyph; some validators (e.g. ots-sanitize) may fail to parse it."));

            // 윤곽 테이블별 추가 검사 (OTF는 CFF 검사)
            ValidateOutline(results);

            return results;
        }

        /// <summary>
        /// 윤곽 테이블(glyf/loca 또는 CFF)에 대한 추가 검사를 수행합니다.
        /// 기본 구현은 아무것도 하지 않으며, OTF(CFF)는 CFF 전용 검사를 오버라이드합니다.
        /// </summary>
        /// <param name="results">검사 결과를 추가할 목록입니다.</param>
        protected virtual void ValidateOutline(List<ValidationResult> results)
        {
        }

        /// <summary>
        /// 글리프의 바운딩 박스(contour별 극값의 min/max)를 계산합니다.
        /// 빈 윤곽이면 false를 반환합니다.
        /// </summary>
        private static bool ComputeGlyphBoundingBox(Glyph glyph, out double xMin, out double yMin, out double xMax, out double yMax)
        {
            xMin = double.MaxValue; yMin = double.MaxValue;
            xMax = double.MinValue; yMax = double.MinValue;
            bool hasPoint = false;
            foreach (var contour in glyph.Outline.Contours)
            {
                if (contour.ComputeBoundingBox(out double cxMin, out double cyMin, out double cxMax, out double cyMax))
                {
                    if (!hasPoint) { xMin = cxMin; yMin = cyMin; xMax = cxMax; yMax = cyMax; hasPoint = true; }
                    else
                    {
                        if (cxMin < xMin) xMin = cxMin;
                        if (cyMin < yMin) yMin = cyMin;
                        if (cxMax > xMax) xMax = cxMax;
                        if (cyMax > yMax) yMax = cyMax;
                    }
                }
            }
            return hasPoint;
        }

        /// <summary>
        /// 글리프 ID를 표시 이름으로 변환합니다.
        /// 0은 .notdef, 매핑된 글리프는 U+XXXX, 그 외는 glyph ID입니다.
        /// </summary>
        private static string GetGlyphDisplayName(int glyphId, Dictionary<int, int> glyphToChar)
        {
            if (glyphId == 0) return ".notdef";
            if (glyphToChar.TryGetValue(glyphId, out int charCode))
                return "U+" + charCode.ToString("X4");
            return "glyph " + glyphId;
        }

        /// <summary>
        /// OpenType checksum을 계산합니다.
        /// 데이터를 4바이트 단위 빅 엔디안 uint32로 해석하여 합산합니다.
        /// </summary>
        private static uint ComputeChecksum(byte[] data)
        {
            uint sum = 0;
            for (int i = 0; i < data.Length; i += 4)
            {
                uint value = 0;
                for (int j = 0; j < 4; j++)
                {
                    value <<= 8;
                    if (i + j < data.Length)
                        value |= data[i + j];
                }
                sum += value;
            }
            return sum;
        }
    }
}
