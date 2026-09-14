using System;
using System.Collections.Generic;
using System.Linq;
using VagabondK.OpenType.Geometry;

namespace VagabondK.OpenType.CFF
{
    /// <summary>
    /// CFF 바이너리를 생성하는 클래스입니다.
    /// 구조: Header → Name Index → Top DICT → String Index → Global Subr Index → Charset → CharStrings → Private DICT → Subrs Index
    /// 여러 글리프가 같은 <see cref="GlyphContour"/> 인스턴스를 참조하면 서브루틴으로 추출됩니다.
    /// </summary>
    static class CffBuilder
    {
        /// <summary>
        /// CFF 바이너리를 생성합니다.
        /// </summary>
        /// <param name="psName">PostScript 이름입니다. (Name INDEX, 예: "VagabondK-Regular")</param>
        /// <param name="topDict">Top DICT 옵션입니다. null인 항목은 Top DICT에 포함하지 않습니다.</param>
        /// <param name="privateDict">Private DICT 옵션입니다. null인 항목은 Private DICT에 포함하지 않습니다.</param>
        /// <param name="glyphNames">글리프 이름 목록입니다. (glyph ID 순서, 0=.notdef)</param>
        /// <param name="glyphs">글리프 목록입니다. (glyph ID 순서)</param>
        public static byte[] Build(string psName, TopDict topDict, PrivateDict privateDict,
            List<string> glyphNames, List<Glyph> glyphs)
        {
            int numGlyphs = glyphs.Count;

            // 서브루틴 감지 (인스턴스 동일성: 같은 contour 인스턴스를 2회 이상 참조하면 서브루틴)
            var subrContours = new List<GlyphContour>();
            var subrMap = new Dictionary<GlyphContour, int>();
            var refCount = new Dictionary<GlyphContour, int>();
            for (int i = 0; i < numGlyphs; i++)
            {
                foreach (var contour in glyphs[i].Outline.Contours)
                {
                    int count;
                    refCount.TryGetValue(contour, out count);
                    refCount[contour] = ++count;
                    if (count == 2)
                    {
                        subrMap[contour] = subrContours.Count;
                        subrContours.Add(contour);
                    }
                }
            }
            int subrBias = GetSubrBias(subrContours.Count);

            // Subrs INDEX 생성 (서브루틴 charstring: contour 모양 + return, width/endchar 없음)
            // Type 2 charstring 스펙: 서브루틴은 return(0x11)으로 종료해야 합니다.
            // 서브루틴은 moveto를 포함하지 않습니다. contour 시작점 기준 상대 모양만 기록하고,
            // moveto는 호출 측(WriteCharstring)이 현재 포인트 기준으로 기록합니다.
            var subrList = new List<byte[]>();
            foreach (var contour in subrContours)
            {
                var cs = new List<byte>();
                var start = contour.Commands[0];
                double curX = start.EndX, curY = start.EndY;
                WriteContour(cs, contour, ref curX, ref curY, writeMoveTo: false);
                WriteCharstringOp(cs, CharstringOperator.Return);
                subrList.Add(cs.ToArray());
            }
            var subrsIndex = BuildByteIndex(subrList);

            // CharStrings 생성 (INDEX 형식: count + offsetSize + offsets + data)
            // 공유 contour는 callsubr, 고유 contour는 인라인
            var charstringList = new List<byte[]>();
            for (int i = 0; i < numGlyphs; i++)
            {
                var cs = new List<byte>();
                WriteCharstring(cs, glyphs[i], subrMap, subrBias);
                charstringList.Add(cs.ToArray());
            }
            var charstringsIndex = BuildByteIndex(charstringList);

            // String Index 생성 (custom 문자열만, standard SID 0-390은 implicit)
            // glyph name이 standard string과 일치하면 standard SID를 재사용하고,
            // 아니면 custom 문자열로 String Index에 추가하여 SID(391+)를 할당합니다.
            var strings = new List<string>();
            var glyphSids = new int[numGlyphs];
            glyphSids[0] = 0; // .notdef → SID 0
            for (int i = 1; i < numGlyphs; i++)
                glyphSids[i] = ResolveSid(strings, glyphNames[i]);

            // Top DICT 문자열을 String Index에 등록 (SID 할당, 부수 효과: strings에 추가)
            ResolveSid(strings, topDict.Version);
            ResolveSid(strings, topDict.Notice);
            ResolveSid(strings, topDict.FullName);
            ResolveSid(strings, topDict.FamilyName);
            ResolveSid(strings, topDict.Weight?.ToString());
            ResolveSid(strings, topDict.Copyright);
            ResolveSid(strings, topDict.PostScript);
            ResolveSid(strings, topDict.BaseFontName);
            ResolveSid(strings, topDict.FontName);
            var stringIndex = BuildIndex(strings.ToArray());

            // Charset 생성 (glyph 1..n-1의 SID, format 0 또는 format 1/2)
            // glyph 0 (.notdef)은 SID 0을 암시적으로 사용
            var charsetSids = new int[numGlyphs - 1];
            Array.Copy(glyphSids, 1, charsetSids, 0, numGlyphs - 1);
            var charset = BuildCharset(charsetSids);

            // 크기 계산
            // Name INDEX: 2(count) + 1(offsetSize) + 2(offsets) + data
            int nameIndexSize = 5 + psName.Length;
            int stringIndexSize = stringIndex.Count;
            int charsetSize = charset.Count;
            int charstringsSize = charstringsIndex.Count;

            // 오프셋/사이즈 값은 op 29(int32, 5바이트) 고정 인코딩을 사용하므로
            // DICT 크기가 오프셋 값에 의존하지 않습니다. 2-pass로 계산합니다.
            // 1st pass: placeholder 오프셋(0)으로 Private DICT 크기 측정
            int privateDictSize = BuildPrivateDict(privateDict, subrContours.Count, 0).Count;

            // 오프셋 계산 (CFF 테이블 시작 기준)
            int topDictIndexSize = BuildByteIndex(new List<byte[]> { BuildTopDict(topDict, strings, 0, 0, 0, 0).ToArray() }).Count;
            int charsetOffset = 4 + nameIndexSize + topDictIndexSize + stringIndexSize + 2;
            int charstringsOffset = charsetOffset + charsetSize;
            int privateOffset = charstringsOffset + charstringsSize;
            // Subrs 오프셋은 Private DICT 시작 기준 (Subrs INDEX는 Private DICT 바로 뒤)
            int subrsOffset = privateDictSize;

            var privateDictBytes = BuildPrivateDict(privateDict, subrContours.Count, subrsOffset);

            // 조립
            var buf = new List<byte>();
            buf.Add(1); buf.Add(0); buf.Add(4); buf.Add(1); // Header
            WriteIndex(buf, new[] { psName }); // Name Index (PS 이름)

            // Top DICT INDEX (단일 항목, offsetSize 자동 계산)
            buf.AddRange(BuildByteIndex(new List<byte[]> { BuildTopDict(topDict, strings, charsetOffset, privateDictSize, privateOffset, charstringsOffset).ToArray() }));

            buf.AddRange(stringIndex);
            buf.Add(0); buf.Add(0); // Global Subr Index (빈 인덱스, count=0)
            buf.AddRange(charset);
            buf.AddRange(charstringsIndex);
            buf.AddRange(privateDictBytes);
            if (subrContours.Count > 0)
                buf.AddRange(subrsIndex);

            return buf.ToArray();
        }

        // ─────────────────────────────────────────────
        // Charstring
        // ─────────────────────────────────────────────

        /// <summary>
        /// 글리프의 charstring을 생성합니다.
        /// width + (callsubr | contour) + endchar
        /// </summary>
        private static void WriteCharstring(List<byte> buf, Glyph glyph, Dictionary<GlyphContour, int> subrMap, int subrBias)
        {
            WriteCffNumber(buf, glyph.AdvanceWidth);

            // 현재 포인트 추적 (moveto는 현재 포인트 기준 상대 오프셋)
            double curX = 0, curY = 0;
            foreach (var contour in glyph.Outline.Contours)
            {
                int subrIndex;
                if (subrMap.TryGetValue(contour, out subrIndex))
                {
                    // 서브루틴은 moveto를 포함하지 않으므로, 호출 측이 moveto로 현재 포인트를
                    // contour 시작점으로 이동한 뒤 callsubr로 모양을 그립니다.
                    var start = contour.Commands[0];
                    WriteMoveTo(buf, curX, curY, start.EndX, start.EndY);
                    curX = start.EndX;
                    curY = start.EndY;
                    // callsubr 인코딩값 = 실제 인덱스 - bias (해독 시 +bias로 복원)
                    WriteCharstringOp(buf, CharstringOperator.CallSubr, subrIndex - subrBias);
                    // 서브루틴 실행 후 현재 포인트는 서브루틴의 마지막 점
                    GetContourEndPoint(contour, out curX, out curY);
                }
                else
                {
                    WriteContour(buf, contour, ref curX, ref curY, writeMoveTo: true);
                }
            }

            WriteCharstringOp(buf, CharstringOperator.EndChar);
        }

        /// <summary>
        /// moveto 연산자를 기록합니다. (hmoveto/vmoveto/rmoveto, 현재 포인트 기준 상대 오프셋)
        /// </summary>
        private static void WriteMoveTo(List<byte> buf, double curX, double curY, double endX, double endY)
        {
            double dx = endX - curX;
            double dy = endY - curY;
            if (dy == 0)
                WriteCharstringOp(buf, CharstringOperator.HMoveTo, dx);
            else if (dx == 0)
                WriteCharstringOp(buf, CharstringOperator.VMoveTo, dy);
            else
                WriteCharstringOp(buf, CharstringOperator.RMoveTo, dx, dy);
        }

        /// <summary>
        /// contour의 charstring 명령을 생성합니다. (moveto + rlineto/rrcurveto)
        /// 2차 베지어는 quad→cubic 변환, 3차 베지어는 rrcurveto로 직접 기록합니다.
        /// 글리프 charstring의 인라인 contour와 서브루틴 charstring에 공용으로 사용됩니다.
        /// writeMoveTo가 false면 moveto를 기록하지 않고 contour 시작점부터 모양만 기록합니다
        /// (서브루틴용 — moveto는 호출 측에서 기록).
        /// </summary>
        private static void WriteContour(List<byte> buf, GlyphContour contour, ref double curX, ref double curY, bool writeMoveTo)
        {
            var commands = contour.Commands;
            if (commands.Count == 0)
                return;

            var start = commands[0];

            // moveto: 현재 포인트 기준 상대 오프셋 (서브루틴은 moveto를 호출 측에서 기록)
            if (writeMoveTo)
                WriteMoveTo(buf, curX, curY, start.EndX, start.EndY);
            curX = start.EndX;
            curY = start.EndY;

            for (int i = 1; i < commands.Count; i++)
            {
                var cmd = commands[i];
                if (cmd is LineToCommand line)
                {
                    WriteCharstringOp(buf, CharstringOperator.RLineTo, line.X - curX, line.Y - curY);
                    curX = line.X;
                    curY = line.Y;
                }
                else if (cmd is QuadraticBezierCommand quad)
                {
                    // quadratic → cubic 변환
                    double c1x = curX + 2.0 / 3.0 * (quad.ControlX - curX);
                    double c1y = curY + 2.0 / 3.0 * (quad.ControlY - curY);
                    double c2x = quad.X + 2.0 / 3.0 * (quad.ControlX - quad.X);
                    double c2y = quad.Y + 2.0 / 3.0 * (quad.ControlY - quad.Y);
                    // rrcurveto: 누적 상대 오프셋 (c1-start, c2-c1, end-c2)
                    WriteCharstringOp(buf, CharstringOperator.RRCurveTo,
                        c1x - curX, c1y - curY,
                        c2x - c1x, c2y - c1y,
                        quad.X - c2x, quad.Y - c2y);
                    curX = quad.X;
                    curY = quad.Y;
                }
                else if (cmd is CubicBezierCommand cubic)
                {
                    // rrcurveto: 누적 상대 오프셋 (c1-start, c2-c1, end-c2)
                    WriteCharstringOp(buf, CharstringOperator.RRCurveTo,
                        cubic.C1X - curX, cubic.C1Y - curY,
                        cubic.C2X - cubic.C1X, cubic.C2Y - cubic.C1Y,
                        cubic.X - cubic.C2X, cubic.Y - cubic.C2Y);
                    curX = cubic.X;
                    curY = cubic.Y;
                }
            }
        }

        /// <summary>
        /// contour 실행 후의 현재 포인트(마지막 커맨드의 종점)를 반환합니다.
        /// </summary>
        private static void GetContourEndPoint(GlyphContour contour, out double endX, out double endY)
        {
            var commands = contour.Commands;
            if (commands.Count == 0)
            {
                endX = 0;
                endY = 0;
                return;
            }
            var last = commands[commands.Count - 1];
            endX = last.EndX;
            endY = last.EndY;
        }

        /// <summary>
        /// 서브루틴 bias를 계산합니다. (local/global 동일)
        /// N &lt; 1240 → 107, 1240 ≤ N &lt; 33900 → 1131, N ≥ 33900 → 32768
        /// </summary>
        private static int GetSubrBias(int count)
        {
            if (count < 1240) return 107;
            if (count < 33900) return 1131;
            return 32768;
        }

        /// <summary>
        /// charstring 연산자를 기록합니다. (value들 → operator 순서)
        /// </summary>
        private static void WriteCharstringOp(List<byte> buf, CharstringOperator op, params double[] values)
        {
            foreach (var v in values)
                WriteCffNumber(buf, v);
            buf.Add((byte)op);
        }

        // ─────────────────────────────────────────────
        // DICT
        // ─────────────────────────────────────────────

        /// <summary>
        /// Top DICT 데이터를 생성합니다.
        /// charset/CharStrings/Private 오프셋은 CFF 테이블 시작 기준입니다.
        /// </summary>
        private static List<byte> BuildTopDict(TopDict top, List<string> strings,
            int charsetOffset, int privateDictSize, int privateOffset, int charstringsOffset)
        {
            var topDict = new List<byte>();
            int sid;
            if ((sid = FindSid(strings, top.Version)) >= 0)
                WriteDictEntry(topDict, DictOperator.Version, sid);
            if ((sid = FindSid(strings, top.Notice)) >= 0)
                WriteDictEntry(topDict, DictOperator.Notice, sid);
            if ((sid = FindSid(strings, top.Copyright)) >= 0)
                WriteDictEntry(topDict, DictOperator.Copyright, sid);
            if ((sid = FindSid(strings, top.FullName)) >= 0)
                WriteDictEntry(topDict, DictOperator.FullName, sid);
            if ((sid = FindSid(strings, top.FontName)) >= 0)
                WriteDictEntry(topDict, DictOperator.FontName, sid);
            if ((sid = FindSid(strings, top.FamilyName)) >= 0)
                WriteDictEntry(topDict, DictOperator.FamilyName, sid);
            if (top.Weight.HasValue && (sid = FindSid(strings, top.Weight.Value.ToString())) >= 0)
                WriteDictEntry(topDict, DictOperator.Weight, sid);
            if (top.IsFixedPitch.HasValue)
                WriteDictEntry(topDict, DictOperator.IsFixedPitch, top.IsFixedPitch.Value);
            if (top.ItalicAngle.HasValue)
                WriteDictEntry(topDict, DictOperator.ItalicAngle, top.ItalicAngle.Value);
            if (top.UnderlinePosition.HasValue)
                WriteDictEntry(topDict, DictOperator.UnderlinePosition, top.UnderlinePosition.Value);
            if (top.UnderlineThickness.HasValue)
                WriteDictEntry(topDict, DictOperator.UnderlineThickness, top.UnderlineThickness.Value);
            if (top.PaintType.HasValue)
                WriteDictEntry(topDict, DictOperator.PaintType, top.PaintType.Value);
            if (top.CharstringType.HasValue)
                WriteDictEntry(topDict, DictOperator.CharstringType, top.CharstringType.Value);
            if (top.FontMatrix.HasValue)
                WriteDictEntry(topDict, DictOperator.FontMatrix,
                    top.FontMatrix.Value.XX, top.FontMatrix.Value.XY, top.FontMatrix.Value.YX,
                    top.FontMatrix.Value.YY, top.FontMatrix.Value.X, top.FontMatrix.Value.Y);
            if (top.StrokeWidth.HasValue)
                WriteDictEntry(topDict, DictOperator.StrokeWidth, top.StrokeWidth.Value);
            if (top.UniqueID.HasValue)
                WriteDictEntry(topDict, DictOperator.UniqueID, top.UniqueID.Value);

            WriteDictEntry(topDict, DictOperator.FontBBox, top.FontBBox);

            if (top.XUID != null)
            {
                var vals = new double[top.XUID.Length];
                for (int i = 0; i < top.XUID.Length; i++)
                    vals[i] = top.XUID[i];
                WriteDictEntry(topDict, DictOperator.XUID, vals);
            }
            if ((sid = FindSid(strings, top.PostScript)) >= 0)
                WriteDictEntry(topDict, DictOperator.PostScript, sid);
            if ((sid = FindSid(strings, top.BaseFontName)) >= 0)
                WriteDictEntry(topDict, DictOperator.BaseFontName, sid);
            if (top.SyntheticBase.HasValue)
                WriteDictEntry(topDict, DictOperator.SyntheticBase, top.SyntheticBase.Value);
            if (top.MaxStack.HasValue)
                WriteDictEntry(topDict, DictOperator.MaxStack, top.MaxStack.Value);
            WriteDictEntryInt32(topDict, DictOperator.Charset, charsetOffset);
            WriteDictEntryInt32(topDict, DictOperator.Private, privateDictSize, privateOffset);
            WriteDictEntryInt32(topDict, DictOperator.CharStrings, charstringsOffset);
            return topDict;
        }

        /// <summary>
        /// Private DICT 데이터를 생성합니다.
        /// null인 항목은 포함하지 않습니다.
        /// 서브루틴이 있으면 Subrs entry(size, offset)를 기록합니다.
        /// </summary>
        /// <param name="p">Private DICT 옵션입니다.</param>
        /// <param name="subrsCount">서브루틴 개수입니다. 0이면 Subrs entry를 기록하지 않습니다.</param>
        /// <param name="subrsOffset">Subrs INDEX의 Private DICT 시작 기준 오프셋입니다.</param>
        private static List<byte> BuildPrivateDict(PrivateDict p, int subrsCount, int subrsOffset)
        {
            var buf = new List<byte>();
            if (p.StdHW.HasValue)
                WriteDictEntry(buf, DictOperator.StdHW, p.StdHW.Value);
            if (p.StdVW.HasValue)
                WriteDictEntry(buf, DictOperator.StdVW, p.StdVW.Value);
            if (p.BlueScale.HasValue)
                WriteDictEntry(buf, DictOperator.BlueScale, p.BlueScale.Value);
            if (p.BlueShift.HasValue)
                WriteDictEntry(buf, DictOperator.BlueShift, p.BlueShift.Value);
            if (p.BlueFuzz.HasValue)
                WriteDictEntry(buf, DictOperator.BlueFuzz, p.BlueFuzz.Value);
            if (p.ForceBold.HasValue)
                WriteDictEntry(buf, DictOperator.ForceBold, p.ForceBold.Value);
            if (p.LanguageGroup.HasValue)
                WriteDictEntry(buf, DictOperator.LanguageGroup, p.LanguageGroup.Value);
            if (p.ExpansionFactor.HasValue)
                WriteDictEntry(buf, DictOperator.ExpansionFactor, p.ExpansionFactor.Value);
            if (p.InitialRandomSeed.HasValue)
                WriteDictEntry(buf, DictOperator.InitialRandomSeed, p.InitialRandomSeed.Value);
            if (p.DefaultWidthX.HasValue)
                WriteDictEntry(buf, DictOperator.DefaultWidthX, p.DefaultWidthX.Value);
            if (p.NominalWidthX.HasValue)
                WriteDictEntry(buf, DictOperator.NominalWidthX, p.NominalWidthX.Value);
            if (p.VsIndex.HasValue)
                WriteDictEntry(buf, DictOperator.VsIndex, p.VsIndex.Value);
            if (p.BlueValues != null)
                WriteDictEntryDelta(buf, DictOperator.BlueValues, p.BlueValues);
            if (p.OtherBlues != null)
                WriteDictEntryDelta(buf, DictOperator.OtherBlues, p.OtherBlues);
            if (p.FamilyBlues != null)
                WriteDictEntryDelta(buf, DictOperator.FamilyBlues, p.FamilyBlues);
            if (p.FamilyOtherBlues != null)
                WriteDictEntryDelta(buf, DictOperator.FamilyOtherBlues, p.FamilyOtherBlues);
            if (p.StemSnapH != null)
                WriteDictEntryDelta(buf, DictOperator.StemSnapH, p.StemSnapH);
            if (p.StemSnapV != null)
                WriteDictEntryDelta(buf, DictOperator.StemSnapV, p.StemSnapV);
            if (subrsCount > 0)
                WriteDictEntryInt32(buf, DictOperator.Subrs, subrsOffset);
            return buf;
        }

        /// <summary>
        /// charset을 생성합니다. (glyph 1..n-1의 SID 배열)
        /// CFF 스펙의 charset은 첫 바이트(format 0/1/2)로 전체 형식이 결정됩니다.
        /// SID가 연속이면 format 1(firstCode + nLeft), 아니면 format 0(SID 목록)을 사용합니다.
        /// nLeft는 format 1에서 Card8(0-255), 255를 초과하면 format 2에서 Card16으로 기록합니다.
        /// </summary>
        private static List<byte> BuildCharset(int[] sids)
        {
            int count = sids.Length;
            if (count == 0)
            {
                var empty = new List<byte>();
                empty.Add(0); // format 0, empty
                return empty;
            }

            // format 1/2: 모든 SID가 연속이어야 함 (sids[i] == sids[i-1] + 1)
            bool allConsecutive = true;
            for (int i = 1; i < count; i++)
            {
                if (sids[i] != sids[i - 1] + 1)
                {
                    allConsecutive = false;
                    break;
                }
            }

            if (allConsecutive)
            {
                // format 1: format(1) + firstCode(Card16) + nLeft(Card8)
                // format 2: format(2) + firstCode(Card16) + nLeft(Card16)
                // nRanges 필드는 없음 (fontTools packCharset/parseCharset과 일치)
                int nLeft = count - 1;
                var buf = new List<byte>();
                buf.Add((byte)(nLeft <= 255 ? 1 : 2)); // format
                buf.Add((byte)(sids[0] >> 8)); // firstCode high
                buf.Add((byte)(sids[0] & 0xFF)); // firstCode low
                if (nLeft <= 255)
                    buf.Add((byte)nLeft); // nLeft Card8
                else
                {
                    buf.Add((byte)(nLeft >> 8)); // nLeft high
                    buf.Add((byte)(nLeft & 0xFF)); // nLeft low
                }
                return buf;
            }

            // format 0: 1 + 2*n
            var buf0 = new List<byte>();
            buf0.Add(0); // format 0
            for (int j = 0; j < count; j++)
            {
                buf0.Add((byte)(sids[j] >> 8));
                buf0.Add((byte)(sids[j] & 0xFF));
            }
            return buf0;
        }

        /// <summary>
        /// DICT entry를 기록합니다. (value들 → operator 순서)
        /// 2바이트 operator(0x0C00 이상)는 0x0C + 두 번째 바이트로 기록합니다.
        /// </summary>
        private static void WriteDictEntry(List<byte> buf, DictOperator op, params double[] values)
        {
            foreach (var v in values)
                WriteCffDictNumber(buf, v);
            WriteDictOperator(buf, op);
        }

        /// <summary>
        /// DICT entry를 op 29(int32, 5바이트) 고정 인코딩으로 기록합니다.
        /// 오프셋/사이즈 값에 사용합니다. 크기가 항상 5바이트 × N이므로
        /// DICT 크기가 값에 의존하지 않아 순환 의존을 피할 수 있습니다.
        /// </summary>
        private static void WriteDictEntryInt32(List<byte> buf, DictOperator op, params int[] values)
        {
            foreach (var v in values)
                WriteCffNumberInt32(buf, v);
            WriteDictOperator(buf, op);
        }

        /// <summary>
        /// DICT entry를 delta 배열로 기록합니다. (첫 값은 절대값, 이후는 이전 값과의 차이)
        /// </summary>
        private static void WriteDictEntryDelta(List<byte> buf, DictOperator op, int[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                int v = (i == 0) ? values[i] : values[i] - values[i - 1];
                WriteCffDictNumber(buf, v);
            }
            WriteDictOperator(buf, op);
        }

        /// <summary>
        /// DICT operator를 기록합니다.
        /// 2바이트 operator(0x0C00 이상)는 0x0C + 두 번째 바이트로 기록합니다.
        /// </summary>
        private static void WriteDictOperator(List<byte> buf, DictOperator op)
        {
            int code = (int)op;
            if (code >= 0x0C00)
            {
                buf.Add(0x0C);
                buf.Add((byte)(code & 0xFF));
            }
            else
            {
                buf.Add((byte)code);
            }
        }

        // ─────────────────────────────────────────────
        // Number 인코딩
        // ─────────────────────────────────────────────

        /// <summary>
        /// CFF number를 인코딩하여 기록합니다.
        /// </summary>
        private static void WriteCffNumber(List<byte> buf, double value)
        {
            if (value == Math.Floor(value) && Math.Abs(value) <= 32767)
            {
                int v = (int)value;
                if (v >= -107 && v <= 107)
                    buf.Add((byte)(v + 139));
                else if (v >= 108 && v <= 1131)
                {
                    buf.Add((byte)((v - 108) / 256 + 247));
                    buf.Add((byte)((v - 108) % 256));
                }
                else if (v >= -1131 && v <= -108)
                {
                    buf.Add((byte)(-(v + 108) / 256 + 251));
                    buf.Add((byte)(-(v + 108) % 256));
                }
                else if (v >= -32768 && v <= 32767)
                {
                    buf.Add(28);
                    buf.Add((byte)((v >> 8) & 0xFF));
                    buf.Add((byte)(v & 0xFF));
                }
                else
                {
                    buf.Add(29);
                    buf.Add((byte)((v >> 24) & 0xFF));
                    buf.Add((byte)((v >> 16) & 0xFF));
                    buf.Add((byte)((v >> 8) & 0xFF));
                    buf.Add((byte)(v & 0xFF));
                }
            }
            else
            {
                buf.Add(255);
                int fixedVal = (int)Math.Round(value * 65536.0);
                buf.Add((byte)((fixedVal >> 24) & 0xFF));
                buf.Add((byte)((fixedVal >> 16) & 0xFF));
                buf.Add((byte)((fixedVal >> 8) & 0xFF));
                buf.Add((byte)(fixedVal & 0xFF));
            }
        }

        /// <summary>
        /// CFF number를 op 29(int32, 5바이트)로 인코딩하여 기록합니다.
        /// </summary>
        private static void WriteCffNumberInt32(List<byte> buf, int value)
        {
            buf.Add(29);
            buf.Add((byte)((value >> 24) & 0xFF));
            buf.Add((byte)((value >> 16) & 0xFF));
            buf.Add((byte)((value >> 8) & 0xFF));
            buf.Add((byte)(value & 0xFF));
        }

        /// <summary>
        /// DICT number를 인코딩하여 기록합니다.
        /// 정수는 charstring과 동일, 실수는 op 30 + nibble 인코딩 (CFF real number).
        /// DICT에서는 255가 reserved이므로 charstring의 16.16 fixed point를 사용할 수 없습니다.
        /// </summary>
        private static void WriteCffDictNumber(List<byte> buf, double value)
        {
            if (value == Math.Floor(value))
            {
                if (Math.Abs(value) <= 32767)
                {
                    int v = (int)value;
                    if (v >= -107 && v <= 107)
                        buf.Add((byte)(v + 139));
                    else if (v >= 108 && v <= 1131)
                    {
                        buf.Add((byte)((v - 108) / 256 + 247));
                        buf.Add((byte)((v - 108) % 256));
                    }
                    else if (v >= -1131 && v <= -108)
                    {
                        buf.Add((byte)(-(v + 108) / 256 + 251));
                        buf.Add((byte)(-(v + 108) % 256));
                    }
                    else
                    {
                        buf.Add(28);
                        buf.Add((byte)((v >> 8) & 0xFF));
                        buf.Add((byte)(v & 0xFF));
                    }
                }
                else
                {
                    // shortint 범위를 벗어난 정수는 op 29(longint)로 인코딩
                    WriteCffNumberInt32(buf, (int)(long)value);
                }
            }
            else
            {
                buf.Add(30);
                string s = value.ToString("G8", System.Globalization.CultureInfo.InvariantCulture);

                // "0.xxx" → ".xxx"
                if (s.StartsWith("0."))
                    s = s.Substring(1);
                else if (s.StartsWith("-0."))
                    s = "-" + s.Substring(2);

                // ".0xxx" → "xxxE-n" (예: ".001" → "1E-3", ".039625" → "39625E-6")
                if (s.StartsWith(".0") || s.StartsWith("-.0"))
                {
                    int dotPos = s.IndexOf('.');
                    string sign = dotPos == 0 ? "" : "-";
                    string frac = s.Substring(dotPos + 1);
                    string digits = frac.TrimStart('0');
                    s = sign + digits + "E-" + frac.Length;
                }

                // nibble 인코딩: 0-9, '.'(10), 'E'(11), 'E-'(12), '-'(14), terminator(15)
                var nibbles = new List<int>();
                for (int i = 0; i < s.Length; i++)
                {
                    char c = s[i];
                    if (c >= '0' && c <= '9')
                        nibbles.Add(c - '0');
                    else if (c == '.')
                        nibbles.Add(10);
                    else if (c == 'E')
                    {
                        if (i + 1 < s.Length && s[i + 1] == '-')
                        {
                            nibbles.Add(12);
                            i++;
                        }
                        else if (i + 1 < s.Length && s[i + 1] == '+')
                        {
                            nibbles.Add(11);
                            i++;
                        }
                        else
                            nibbles.Add(11);
                    }
                    else if (c == '-')
                        nibbles.Add(14);
                }
                nibbles.Add(15); // terminator
                if (nibbles.Count % 2 != 0)
                    nibbles.Add(15); // pad
                for (int i = 0; i < nibbles.Count; i += 2)
                    buf.Add((byte)((nibbles[i] << 4) | nibbles[i + 1]));
            }
        }

        // ─────────────────────────────────────────────
        // SID (String ID)
        // ─────────────────────────────────────────────

        /// <summary>
        /// 문자열의 SID를 계산합니다.
        /// standard string과 일치하면 standard SID(0-390)를 반환하고,
        /// 아니면 custom 문자열로 String Index에 추가하여 SID(391+)를 반환합니다.
        /// null/빈 값은 -1을 반환합니다.
        /// </summary>
        private static int ResolveSid(List<string> strings, string value)
        {
            if (string.IsNullOrEmpty(value))
                return -1;
            for (int i = 0; i < StandardStrings.Length; i++)
            {
                if (StandardStrings[i] == value)
                    return i;
            }
            for (int i = 0; i < strings.Count; i++)
            {
                if (strings[i] == value)
                    return 391 + i;
            }
            int sid = 391 + strings.Count;
            strings.Add(value);
            return sid;
        }

        /// <summary>
        /// 문자열의 SID를 조회합니다. (부수 효과 없음, strings에 추가하지 않음)
        /// </summary>
        private static int FindSid(List<string> strings, string value)
        {
            if (string.IsNullOrEmpty(value))
                return -1;
            for (int i = 0; i < StandardStrings.Length; i++)
            {
                if (StandardStrings[i] == value)
                    return i;
            }
            for (int i = 0; i < strings.Count; i++)
            {
                if (strings[i] == value)
                    return 391 + i;
            }
            return -1;
        }

        /// <summary>
        /// CFF standard string입니다. (SID 0-390, Appendix A)
        /// </summary>
        internal static readonly string[] StandardStrings =
        {
            ".notdef", "space", "exclam", "quotedbl", "numbersign", "dollar", "percent",
            "ampersand", "quoteright", "parenleft", "parenright", "asterisk", "plus",
            "comma", "hyphen", "period", "slash", "zero", "one", "two", "three", "four",
            "five", "six", "seven", "eight", "nine", "colon", "semicolon", "less", "equal",
            "greater", "question", "at", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J",
            "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
            "bracketleft", "backslash", "bracketright", "asciicircum", "underscore", "quoteleft",
            "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p",
            "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "braceleft", "bar", "braceright",
            "asciitilde", "exclamdown", "cent", "sterling", "fraction", "yen", "florin", "section",
            "currency", "quotesingle", "quotedblleft", "guillemotleft", "guilsinglleft", "guilsinglright",
            "fi", "fl", "endash", "dagger", "daggerdbl", "periodcentered", "paragraph", "bullet",
            "quotesinglbase", "quotedblbase", "quotedblright", "guillemotright", "ellipsis", "perthousand",
            "questiondown", "grave", "acute", "circumflex", "tilde", "macron", "breve", "dotaccent",
            "dieresis", "ring", "cedilla", "hungarumlaut", "ogonek", "caron", "emdash", "AE",
            "ordfeminine", "Lslash", "Oslash", "OE", "ordmasculine", "ae", "dotlessi", "lslash",
            "oslash", "oe", "germandbls", "onesuperior", "logicalnot", "mu", "trademark", "Eth",
            "onehalf", "plusminus", "Thorn", "onequarter", "divide", "brokenbar", "degree", "thorn",
            "threequarters", "twosuperior", "registered", "minus", "eth", "multiply", "threesuperior",
            "copyright", "Aacute", "Acircumflex", "Adieresis", "Agrave", "Aring", "Atilde", "Ccedilla",
            "Eacute", "Ecircumflex", "Edieresis", "Egrave", "Iacute", "Icircumflex", "Idieresis", "Igrave",
            "Ntilde", "Oacute", "Ocircumflex", "Odieresis", "Ograve", "Otilde", "Scaron", "Uacute",
            "Ucircumflex", "Udieresis", "Ugrave", "Yacute", "Ydieresis", "Zcaron", "aacute", "acircumflex",
            "adieresis", "agrave", "aring", "atilde", "ccedilla", "eacute", "ecircumflex", "edieresis",
            "egrave", "iacute", "icircumflex", "idieresis", "igrave", "ntilde", "oacute", "ocircumflex",
            "odieresis", "ograve", "otilde", "scaron", "uacute", "ucircumflex", "udieresis", "ugrave",
            "yacute", "ydieresis", "zcaron", "exclamsmall", "Hungarumlautsmall", "dollaroldstyle",
            "dollarsuperior", "ampersandsmall", "Acutesmall", "parenleftsuperior", "parenrightsuperior",
            "twodotenleader", "onedotenleader", "zerooldstyle", "oneoldstyle", "twooldstyle", "threeoldstyle",
            "fouroldstyle", "fiveoldstyle", "sixoldstyle", "sevenoldstyle", "eightoldstyle", "nineoldstyle",
            "commasuperior", "threequartersemdash", "periodsuperior", "questionsmall", "asuperior", "bsuperior",
            "centsuperior", "dsuperior", "esuperior", "isuperior", "lsuperior", "msuperior", "nsuperior",
            "osuperior", "rsuperior", "ssuperior", "tsuperior", "ff", "ffi", "ffl", "parenleftinferior",
            "parenrightinferior", "Circumflexsmall", "hyphensuperior", "Gravesmall", "Asmall", "Bsmall",
            "Csmall", "Dsmall", "Esmall", "Fsmall", "Gsmall", "Hsmall", "Ismall", "Jsmall", "Ksmall",
            "Lsmall", "Msmall", "Nsmall", "Osmall", "Psmall", "Qsmall", "Rsmall", "Ssmall", "Tsmall",
            "Usmall", "Vsmall", "Wsmall", "Xsmall", "Ysmall", "Zsmall", "colonmonetary", "onefitted",
            "rupiah", "Tildesmall", "exclamdownsmall", "centoldstyle", "Lslashsmall", "Scaronsmall",
            "Zcaronsmall", "Dieresissmall", "Brevesmall", "Caronsmall", "Dotaccentsmall", "Macronsmall",
            "figuredash", "hypheninferior", "Ogoneksmall", "Ringsmall", "Cedillasmall", "questiondownsmall",
            "oneeighth", "threeeighths", "fiveeighths", "seveneighths", "onethird", "twothirds",
            "zerosuperior", "foursuperior", "fivesuperior", "sixsuperior", "sevensuperior", "eightsuperior",
            "ninesuperior", "zeroinferior", "oneinferior", "twoinferior", "threeinferior", "fourinferior",
            "fiveinferior", "sixinferior", "seveninferior", "eightinferior", "nineinferior", "centinferior",
            "dollarinferior", "periodinferior", "commainferior", "Agravesmall", "Aacutesmall", "Acircumflexsmall",
            "Atildesmall", "Adieresissmall", "Aringsmall", "AEsmall", "Ccedillasmall", "Egravesmall",
            "Eacutesmall", "Ecircumflexsmall", "Edieresissmall", "Igravesmall", "Iacutesmall", "Icircumflexsmall",
            "Idieresissmall", "Ethsmall", "Ntildesmall", "Ogravesmall", "Oacutesmall", "Ocircumflexsmall",
            "Otildesmall", "Odieresissmall", "OEsmall", "Oslashsmall", "Ugravesmall", "Uacutesmall",
            "Ucircumflexsmall", "Udieresissmall", "Yacutesmall", "Thornsmall", "Ydieresissmall", "001.000",
            "001.001", "001.002", "001.003", "Black", "Bold", "Book", "Light", "Medium", "Regular",
            "Roman", "Semibold"
        };

        /// <summary>
        /// charCode에 해당하는 CFF standard glyph name을 반환합니다.
        /// Adobe Standard Encoding(ASCII 0x20-0x7E, Latin-1 0xA0-0xFF) 기준입니다.
        /// 매칭되지 않으면 false를 반환합니다.
        /// </summary>
        public static bool TryGetStandardName(int charCode, out string name)
        {
            name = null;
            if (charCode >= 0x20 && charCode <= 0x7E)
            {
                name = AsciiNames[charCode - 0x20];
                return name != null;
            }
            if (charCode >= 0xA0 && charCode <= 0xFF)
            {
                name = Latin1Names[charCode - 0xA0];
                return name != null;
            }
            return false;
        }

        /// <summary>
        /// ASCII(0x20-0x7E)의 CFF standard glyph name입니다. (인덱스 = charCode - 0x20)
        /// </summary>
        private static readonly string[] AsciiNames =
        {
            "space", "exclam", "quotedbl", "numbersign", "dollar", "percent", "ampersand", "quoteright",
            "parenleft", "parenright", "asterisk", "plus", "comma", "hyphen", "period", "slash",
            "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
            "colon", "semicolon", "less", "equal", "greater", "question", "at",
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P",
            "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
            "bracketleft", "backslash", "bracketright", "asciicircum", "underscore", "grave",
            "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p",
            "q", "r", "s", "t", "u", "v", "w", "x", "y", "z",
            "braceleft", "bar", "braceright", "asciitilde"
        };

        /// <summary>
        /// Latin-1(0xA0-0xFF)의 CFF standard glyph name입니다. (인덱스 = charCode - 0xA0)
        /// null은 standard name이 없는 charCode입니다. (0xA0 no-break space, 0xAD soft hyphen)
        /// </summary>
        private static readonly string[] Latin1Names =
        {
            null, // 0xA0
            "exclamdown", "cent", "sterling", "currency", "yen", "brokenbar", "section",
            "dieresis", "copyright", "ordfeminine", "guillemotleft", "logicalnot", null, // 0xAD
            "registered", "macron", "degree", "plusminus", "twosuperior", "threesuperior",
            "acute", "mu", "paragraph", "periodcentered", "cedilla", "onesuperior", "ordmasculine",
            "guillemotright", "onequarter", "onehalf", "threequarters", "questiondown",
            "Agrave", "Aacute", "Acircumflex", "Atilde", "Adieresis", "Aring", "AE", "Ccedilla",
            "Egrave", "Eacute", "Ecircumflex", "Edieresis", "Igrave", "Iacute", "Icircumflex", "Idieresis",
            "Eth", "Ntilde", "Ograve", "Oacute", "Ocircumflex", "Otilde", "Odieresis", "multiply",
            "Oslash", "Ugrave", "Uacute", "Ucircumflex", "Udieresis", "Yacute", "Thorn", "germandbls",
            "agrave", "aacute", "acircumflex", "atilde", "adieresis", "aring", "ae", "ccedilla",
            "egrave", "eacute", "ecircumflex", "edieresis", "igrave", "iacute", "icircumflex", "idieresis",
            "eth", "ntilde", "ograve", "oacute", "ocircumflex", "otilde", "odieresis", "divide",
            "oslash", "ugrave", "uacute", "ucircumflex", "udieresis", "yacute", "thorn", "ydieresis"
        };

        // ─────────────────────────────────────────────
        // INDEX
        // ─────────────────────────────────────────────

        /// <summary>
        /// CFF INDEX의 offsetSize를 데이터 크기에 따라 반환합니다.
        /// </summary>
        private static int GetIndexOffsetSize(int dataSize)
        {
            // 마지막 offset = 1 + dataSize (1-based)
            int lastOffset = 1 + dataSize;
            if (lastOffset <= 255) return 1;
            if (lastOffset <= 65535) return 2;
            if (lastOffset <= 16777215) return 3;
            return 4;
        }

        /// <summary>
        /// CFF Index를 생성합니다.
        /// 형식: count(Card16) + [offsetSize(Card8) + offsets(count+1개) + data]
        /// count=0인 경우 count(2바이트)만 기록합니다.
        /// </summary>
        private static List<byte> BuildIndex(string[] items)
        {
            var buf = new List<byte>();
            int count = items.Length;
            buf.Add((byte)(count >> 8)); // count high
            buf.Add((byte)(count & 0xFF)); // count low
            if (count == 0)
                return buf;

            int totalDataSize = 0;
            foreach (var item in items)
                totalDataSize += item.Length;

            int offsetSize = GetIndexOffsetSize(totalDataSize);

            buf.Add((byte)offsetSize); // offsetSize

            int offset = 1;
            for (int i = 0; i <= count; i++)
            {
                if (i > 0)
                    offset += items[i - 1].Length;
                for (int j = offsetSize - 1; j >= 0; j--)
                    buf.Add((byte)((offset >> (j * 8)) & 0xFF));
            }

            foreach (var item in items)
            {
                foreach (char c in item)
                    buf.Add((byte)c);
            }
            return buf;
        }

        /// <summary>
        /// 바이트 배열 항목들의 CFF Index를 생성합니다. (CharStrings INDEX용)
        /// 형식: count(Card16) + [offsetSize(Card8) + offsets(count+1개) + data]
        /// </summary>
        private static List<byte> BuildByteIndex(List<byte[]> items)
        {
            var buf = new List<byte>();
            int count = items.Count;
            buf.Add((byte)(count >> 8)); // count high
            buf.Add((byte)(count & 0xFF)); // count low
            if (count == 0)
                return buf;

            int totalDataSize = 0;
            foreach (var item in items)
                totalDataSize += item.Length;

            int offsetSize = GetIndexOffsetSize(totalDataSize);

            buf.Add((byte)offsetSize); // offsetSize

            int offset = 1;
            for (int i = 0; i <= count; i++)
            {
                if (i > 0)
                    offset += items[i - 1].Length;
                for (int j = offsetSize - 1; j >= 0; j--)
                    buf.Add((byte)((offset >> (j * 8)) & 0xFF));
            }

            foreach (var item in items)
                buf.AddRange(item);
            return buf;
        }

        /// <summary>
        /// CFF Index를 buf에 기록합니다.
        /// </summary>
        private static void WriteIndex(List<byte> buf, string[] items) => buf.AddRange(BuildIndex(items));
    }
}
