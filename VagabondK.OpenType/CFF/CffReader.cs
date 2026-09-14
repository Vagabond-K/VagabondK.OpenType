using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using VagabondK.OpenType.Geometry;

namespace VagabondK.OpenType.CFF
{
    /// <summary>
    /// CFF 테이블을 역파싱하는 클래스입니다. <see cref="CffBuilder.Build"/>의 역으로,
    /// Header/Name INDEX/Top DICT/String INDEX/CharStrings/Private DICT/Subrs를 읽고,
    /// 각 글리프의 charstring(서브루틴 포함)을 <see cref="GlyphOutline"/>과 advance width로 복원합니다.
    /// 이 구현은 이 라이브러리가 생성한 CFF(셀프 리더)만 지원합니다.
    /// </summary>
    static class CffReader
    {
        /// <summary>
        /// CFF 역파싱 결과입니다.
        /// </summary>
        internal class CffData
        {
            /// <summary>Name INDEX의 PostScript 이름입니다.</summary>
            public string PsName;

            /// <summary>Top DICT 옵션입니다.</summary>
            public TopDict TopDict;

            /// <summary>Private DICT 옵션입니다.</summary>
            public PrivateDict Private;

            /// <summary>glyph ID 순서의 윤곽 목록입니다. (0=.notdef)</summary>
            public List<GlyphOutline> Outlines;

            /// <summary>glyph ID 순서의 advance width 목록입니다.</summary>
            public List<int> AdvanceWidths;
        }

        /// <summary>
        /// CFF 테이블을 역파싱합니다.
        /// </summary>
        /// <param name="reader">CFF 테이블 데이터를 읽는 리더입니다.</param>
        /// <param name="cffOffset">CFF 테이블의 sfnt 내 시작 오프셋입니다.</param>
        /// <returns>역파싱 결과입니다.</returns>
        internal static CffData Read(OpenTypeBinaryReader reader, int cffOffset)
        {
            reader.Position = cffOffset;

            // Header: major, minor, hdrSize, offSize
            reader.ReadUInt8();
            reader.ReadUInt8();
            reader.ReadUInt8();
            reader.ReadUInt8();

            // Name INDEX (단일 항목: PostScript 이름)
            string psName = ReadSingleString(reader);

            // Top DICT INDEX (단일 항목)
            var topDictIndex = ReadByteIndex(reader);
            byte[] topDictBytes = topDictIndex[0];

            // String INDEX (custom 문자열, SID 391+)
            var customStrings = ReadStringIndex(reader);

            // Global Subr INDEX (count=0, 스킵)
            ReadByteIndex(reader);

            // Top DICT 파싱 (오프셋 + 속성)
            var topDict = new TopDict();
            int charsetOffset = 0, privateSize = 0, privateOffset = 0, charstringsOffset = 0;
            ParseTopDict(topDictBytes, customStrings, topDict, ref charsetOffset, ref privateSize, ref privateOffset, ref charstringsOffset);

            // CharStrings INDEX
            reader.Position = cffOffset + charstringsOffset;
            var charstrings = ReadByteIndex(reader);

            // Private DICT
            reader.Position = cffOffset + privateOffset;
            byte[] privateBytes = reader.ReadBytes(privateSize);
            var privateDict = new PrivateDict();
            int subrsOffset = 0;
            ParsePrivateDict(privateBytes, privateDict, ref subrsOffset);

            // Subrs INDEX (Private DICT 시작 기준 상대 오프셋)
            List<byte[]> subrs = null;
            if (subrsOffset > 0)
            {
                reader.Position = cffOffset + privateOffset + subrsOffset;
                subrs = ReadByteIndex(reader);
            }

            // charstring 역해석 (공유 contour 인스턴스로 서브루틴 재추출 가능하게)
            var outlines = new List<GlyphOutline>(charstrings.Count);
            var advanceWidths = new List<int>(charstrings.Count);
            var sharedContours = new Dictionary<int, GlyphContour>();
            for (int i = 0; i < charstrings.Count; i++)
            {
                var result = DecodeGlyphCharstring(charstrings[i], subrs, sharedContours);
                outlines.Add(result.Outline);
                advanceWidths.Add(result.AdvanceWidth);
            }

            return new CffData
            {
                PsName = psName,
                TopDict = topDict,
                Private = privateDict,
                Outlines = outlines,
                AdvanceWidths = advanceWidths
            };
        }

        // ─────────────────────────────────────────────
        // INDEX
        // ─────────────────────────────────────────────

        /// <summary>
        /// CFF INDEX를 읽습니다. 형식: count(Card16) + [offsetSize(Card8) + offsets(count+1, 1-based) + data].
        /// count=0이면 빈 목록을 반환합니다. 읽기 후 스트림 위치는 INDEX 끝으로 이동합니다.
        /// </summary>
        private static List<byte[]> ReadByteIndex(OpenTypeBinaryReader reader)
        {
            int count = reader.ReadUInt16();
            if (count == 0)
                return new List<byte[]>();

            int offsetSize = reader.ReadUInt8();
            int[] offsets = new int[count + 1];
            for (int i = 0; i <= count; i++)
            {
                int v = 0;
                for (int j = 0; j < offsetSize; j++)
                    v = (v << 8) | reader.ReadUInt8();
                offsets[i] = v;
            }

            long dataStart = reader.Position;
            var items = new List<byte[]>(count);
            for (int i = 0; i < count; i++)
            {
                int start = offsets[i] - 1;
                int length = offsets[i + 1] - offsets[i];
                reader.Position = dataStart + start;
                items.Add(reader.ReadBytes(length));
            }
            reader.Position = dataStart + offsets[count] - 1;
            return items;
        }

        /// <summary>
        /// 단일 문자열을 가진 INDEX(Name INDEX)를 읽고 그 문자열을 반환합니다.
        /// </summary>
        private static string ReadSingleString(OpenTypeBinaryReader reader)
        {
            var items = ReadByteIndex(reader);
            if (items.Count == 0)
                return null;
            return Encoding.ASCII.GetString(items[0]);
        }

        /// <summary>
        /// String INDEX를 읽고 custom 문자열 목록을 반환합니다. (SID 391+)
        /// </summary>
        private static List<string> ReadStringIndex(OpenTypeBinaryReader reader)
        {
            var items = ReadByteIndex(reader);
            var strings = new List<string>(items.Count);
            foreach (var item in items)
                strings.Add(Encoding.ASCII.GetString(item));
            return strings;
        }

        // ─────────────────────────────────────────────
        // DICT
        // ─────────────────────────────────────────────

        /// <summary>
        /// Top DICT 바이트를 파싱합니다. 오프셋(charset/private/charstrings)과 속성을 추출합니다.
        /// </summary>
        private static void ParseTopDict(byte[] data, List<string> customStrings, TopDict topDict,
            ref int charsetOffset, ref int privateSize, ref int privateOffset, ref int charstringsOffset)
        {
            var stack = new List<double>();
            int pos = 0;
            while (pos < data.Length)
            {
                int b = data[pos++];
                if (IsDictNumber(b))
                {
                    stack.Add(ReadDictNumber(data, ref pos, b));
                }
                else
                {
                    int op = (b == 12) ? ((12 << 8) | data[pos++]) : b;
                    ApplyTopDictEntry(op, stack, customStrings, topDict,
                        ref charsetOffset, ref privateSize, ref privateOffset, ref charstringsOffset);
                    stack.Clear();
                }
            }
        }

        /// <summary>
        /// Private DICT 바이트를 파싱합니다. 속성과 Subrs 오프셋을 추출합니다.
        /// </summary>
        private static void ParsePrivateDict(byte[] data, PrivateDict privateDict, ref int subrsOffset)
        {
            var stack = new List<double>();
            int pos = 0;
            while (pos < data.Length)
            {
                int b = data[pos++];
                if (IsDictNumber(b))
                {
                    stack.Add(ReadDictNumber(data, ref pos, b));
                }
                else
                {
                    int op = (b == 12) ? ((12 << 8) | data[pos++]) : b;
                    ApplyPrivateDictEntry(op, stack, privateDict, ref subrsOffset);
                    stack.Clear();
                }
            }
        }

        private static void ApplyTopDictEntry(int op, List<double> values, List<string> customStrings, TopDict topDict,
            ref int charsetOffset, ref int privateSize, ref int privateOffset, ref int charstringsOffset)
        {
            switch (op)
            {
                case (int)DictOperator.Version: topDict.Version = ResolveSid((int)values[0], customStrings); break;
                case (int)DictOperator.Notice: topDict.Notice = ResolveSid((int)values[0], customStrings); break;
                case (int)DictOperator.FullName: topDict.FullName = ResolveSid((int)values[0], customStrings); break;
                case (int)DictOperator.FamilyName: topDict.FamilyName = ResolveSid((int)values[0], customStrings); break;
                case (int)DictOperator.Weight:
                    {
                        string name = ResolveSid((int)values[0], customStrings);
                        if (name != null && Enum.TryParse<CffWeight>(name, out CffWeight w))
                            topDict.Weight = w;
                    }
                    break;
                case (int)DictOperator.FontBBox:
                    topDict.FontBBox = new double[] { values[0], values[1], values[2], values[3] };
                    break;
                case (int)DictOperator.UniqueID: topDict.UniqueID = (int)values[0]; break;
                case (int)DictOperator.XUID:
                    {
                        var xuid = new int[values.Count];
                        for (int i = 0; i < values.Count; i++) xuid[i] = (int)values[i];
                        topDict.XUID = xuid;
                    }
                    break;
                case (int)DictOperator.Charset: charsetOffset = (int)values[0]; break;
                case (int)DictOperator.Private: privateSize = (int)values[0]; privateOffset = (int)values[1]; break;
                case (int)DictOperator.CharStrings: charstringsOffset = (int)values[0]; break;
                case (int)DictOperator.MaxStack: topDict.MaxStack = (int)values[0]; break;
                case (int)DictOperator.Copyright: topDict.Copyright = ResolveSid((int)values[0], customStrings); break;
                case (int)DictOperator.IsFixedPitch: topDict.IsFixedPitch = (int)values[0]; break;
                case (int)DictOperator.ItalicAngle: topDict.ItalicAngle = values[0]; break;
                case (int)DictOperator.UnderlinePosition: topDict.UnderlinePosition = (int)values[0]; break;
                case (int)DictOperator.UnderlineThickness: topDict.UnderlineThickness = (int)values[0]; break;
                case (int)DictOperator.PaintType: topDict.PaintType = (int)values[0]; break;
                case (int)DictOperator.CharstringType: topDict.CharstringType = (int)values[0]; break;
                case (int)DictOperator.FontMatrix:
                    topDict.FontMatrix = new FontMatrix(values[0], values[1], values[2], values[3], values[4], values[5]);
                    break;
                case (int)DictOperator.StrokeWidth: topDict.StrokeWidth = values[0]; break;
                case (int)DictOperator.SyntheticBase: topDict.SyntheticBase = (int)values[0]; break;
                case (int)DictOperator.PostScript: topDict.PostScript = ResolveSid((int)values[0], customStrings); break;
                case (int)DictOperator.BaseFontName: topDict.BaseFontName = ResolveSid((int)values[0], customStrings); break;
                case (int)DictOperator.FontName: topDict.FontName = ResolveSid((int)values[0], customStrings); break;
            }
        }

        private static void ApplyPrivateDictEntry(int op, List<double> values, PrivateDict privateDict, ref int subrsOffset)
        {
            switch (op)
            {
                case (int)DictOperator.StdHW: privateDict.StdHW = (int)values[0]; break;
                case (int)DictOperator.StdVW: privateDict.StdVW = (int)values[0]; break;
                case (int)DictOperator.BlueScale: privateDict.BlueScale = values[0]; break;
                case (int)DictOperator.BlueShift: privateDict.BlueShift = (int)values[0]; break;
                case (int)DictOperator.BlueFuzz: privateDict.BlueFuzz = (int)values[0]; break;
                case (int)DictOperator.ForceBold: privateDict.ForceBold = (int)values[0]; break;
                case (int)DictOperator.LanguageGroup: privateDict.LanguageGroup = (int)values[0]; break;
                case (int)DictOperator.ExpansionFactor: privateDict.ExpansionFactor = values[0]; break;
                case (int)DictOperator.InitialRandomSeed: privateDict.InitialRandomSeed = (int)values[0]; break;
                case (int)DictOperator.DefaultWidthX: privateDict.DefaultWidthX = (int)values[0]; break;
                case (int)DictOperator.NominalWidthX: privateDict.NominalWidthX = (int)values[0]; break;
                case (int)DictOperator.VsIndex: privateDict.VsIndex = (int)values[0]; break;
                case (int)DictOperator.Subrs: subrsOffset = (int)values[0]; break;
                case (int)DictOperator.BlueValues: privateDict.BlueValues = DecodeDeltaArray(values); break;
                case (int)DictOperator.OtherBlues: privateDict.OtherBlues = DecodeDeltaArray(values); break;
                case (int)DictOperator.FamilyBlues: privateDict.FamilyBlues = DecodeDeltaArray(values); break;
                case (int)DictOperator.FamilyOtherBlues: privateDict.FamilyOtherBlues = DecodeDeltaArray(values); break;
                case (int)DictOperator.StemSnapH: privateDict.StemSnapH = DecodeDeltaArray(values); break;
                case (int)DictOperator.StemSnapV: privateDict.StemSnapV = DecodeDeltaArray(values); break;
            }
        }

        /// <summary>
        /// delta 배열을 절대값 배열로 복원합니다. (첫 값은 절대값, 이후는 이전 값과의 차이)
        /// </summary>
        private static int[] DecodeDeltaArray(List<double> values)
        {
            var result = new int[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                int v = (int)values[i];
                result[i] = (i == 0) ? v : result[i - 1] + v;
            }
            return result;
        }

        /// <summary>
        /// SID를 문자열로 해석합니다. (standard SID 0-390 또는 custom SID 391+)
        /// </summary>
        private static string ResolveSid(int sid, List<string> customStrings)
        {
            if (sid >= 0 && sid < CffBuilder.StandardStrings.Length)
                return CffBuilder.StandardStrings[sid];
            int customIndex = sid - 391;
            if (customIndex >= 0 && customIndex < customStrings.Count)
                return customStrings[customIndex];
            return null;
        }

        // ─────────────────────────────────────────────
        // Number
        // ─────────────────────────────────────────────

        /// <summary>charstring number인지 판별합니다. (28, 29, 255, 32-254)</summary>
        private static bool IsCharstringNumber(int b)
        {
            return b == 28 || b == 29 || b == 255 || (b >= 32 && b <= 254);
        }

        /// <summary>DICT number인지 판별합니다. (28, 29, 30, 32-254. 255는 reserved)</summary>
        private static bool IsDictNumber(int b)
        {
            return b == 28 || b == 29 || b == 30 || (b >= 32 && b <= 254);
        }

        /// <summary>
        /// charstring number를 해독합니다. <see cref="CffBuilder"/>의 WriteCffNumber의 역입니다.
        /// </summary>
        private static double ReadCharstringNumber(byte[] data, ref int pos, int b)
        {
            if (b >= 32 && b <= 246)
                return b - 139;
            if (b >= 247 && b <= 250)
            {
                int b2 = data[pos++];
                return (b - 247) * 256 + b2 + 108;
            }
            if (b >= 251 && b <= 254)
            {
                int b2 = data[pos++];
                return -((b - 251) * 256 + b2 + 108);
            }
            if (b == 28)
            {
                int hi = data[pos++];
                int lo = data[pos++];
                return (short)((hi << 8) | lo);
            }
            if (b == 29)
            {
                int b0 = data[pos++];
                int b1 = data[pos++];
                int b2 = data[pos++];
                int b3 = data[pos++];
                return (int)((b0 << 24) | (b1 << 16) | (b2 << 8) | b3);
            }
            if (b == 255)
            {
                int b0 = data[pos++];
                int b1 = data[pos++];
                int b2 = data[pos++];
                int b3 = data[pos++];
                int fixedVal = (int)((b0 << 24) | (b1 << 16) | (b2 << 8) | b3);
                return fixedVal / 65536.0;
            }
            throw new InvalidOperationException("Invalid charstring number.");
        }

        /// <summary>
        /// DICT number를 해독합니다. 정수는 charstring과 동일, 실수는 op 30 + nibble 인코딩입니다.
        /// </summary>
        private static double ReadDictNumber(byte[] data, ref int pos, int b)
        {
            if (b >= 32 && b <= 246)
                return b - 139;
            if (b >= 247 && b <= 250)
            {
                int b2 = data[pos++];
                return (b - 247) * 256 + b2 + 108;
            }
            if (b >= 251 && b <= 254)
            {
                int b2 = data[pos++];
                return -((b - 251) * 256 + b2 + 108);
            }
            if (b == 28)
            {
                int hi = data[pos++];
                int lo = data[pos++];
                return (short)((hi << 8) | lo);
            }
            if (b == 29)
            {
                int b0 = data[pos++];
                int b1 = data[pos++];
                int b2 = data[pos++];
                int b3 = data[pos++];
                return (int)((b0 << 24) | (b1 << 16) | (b2 << 8) | b3);
            }
            if (b == 30)
                return ReadNibbleReal(data, ref pos);
            throw new InvalidOperationException("Invalid DICT number.");
        }

        /// <summary>
        /// nibble 인코딩의 CFF real number를 해독합니다. (op 30 이후)
        /// nibble: 0-9, '.'(10), 'E'(11), 'E-'(12), '-'(14), terminator(15)
        /// </summary>
        private static double ReadNibbleReal(byte[] data, ref int pos)
        {
            var sb = new StringBuilder();
            while (true)
            {
                int b = data[pos++];
                int hi = b >> 4;
                int lo = b & 0xF;
                if (hi == 15)
                    break;
                AppendNibble(sb, hi);
                if (lo == 15)
                    break;
                AppendNibble(sb, lo);
            }
            return double.Parse(sb.ToString(), CultureInfo.InvariantCulture);
        }

        private static void AppendNibble(StringBuilder sb, int n)
        {
            if (n >= 0 && n <= 9)
                sb.Append((char)('0' + n));
            else if (n == 10)
                sb.Append('.');
            else if (n == 11)
                sb.Append('E');
            else if (n == 12)
                sb.Append("E-");
            else if (n == 14)
                sb.Append('-');
        }

        // ─────────────────────────────────────────────
        // Charstring
        // ─────────────────────────────────────────────

        /// <summary>charstring 해독 상태입니다. (서브루틴 재귀 호출 시 공유)</summary>
        private class CharstringState
        {
            public double CurX, CurY;
            public GlyphContour CurrentContour;
            public GlyphOutline Outline;
            public List<byte[]> Subrs;
            public int SubrBias;
            public bool PendingMoveTo;
            public Dictionary<int, GlyphContour> SharedContours;
        }

        /// <summary>
        /// 글리프 charstring을 역해석하여 윤곽과 advance width를 반환합니다.
        /// 첫 number는 advance width입니다. (이 라이브러리의 charstring은 항상 width를 기록)
        /// </summary>
        private static GlyphResult DecodeGlyphCharstring(byte[] data, List<byte[]> subrs, Dictionary<int, GlyphContour> sharedContours)
        {
            var outline = new GlyphOutline();
            int advanceWidth = 0;
            int pos = 0;

            // width (첫 number, moveto 이전)
            if (pos < data.Length && IsCharstringNumber(data[pos]))
            {
                int b = data[pos++];
                advanceWidth = (int)ReadCharstringNumber(data, ref pos, b);
            }

            var state = new CharstringState
            {
                Outline = outline,
                Subrs = subrs,
                SubrBias = GetSubrBias(subrs != null ? subrs.Count : 0),
                SharedContours = sharedContours
            };
            DecodeCharstringBody(data, ref pos, state);

            return new GlyphResult { Outline = outline, AdvanceWidth = advanceWidth };
        }

        /// <summary>글리프 charstring 해독 결과입니다.</summary>
        private struct GlyphResult
        {
            public GlyphOutline Outline;
            public int AdvanceWidth;
        }

        /// <summary>
        /// charstring 본문을 역해석합니다. (width 제외)
        /// moveto/rlineto/rrcurveto/callsubr/return/endchar를 처리하며,
        /// 서브루틴(callsubr)은 재귀적으로 실행합니다.
        /// </summary>
        private static void DecodeCharstringBody(byte[] data, ref int pos, CharstringState state)
        {
            var stack = new List<double>();
            while (pos < data.Length)
            {
                int b = data[pos++];
                if (IsCharstringNumber(b))
                {
                    stack.Add(ReadCharstringNumber(data, ref pos, b));
                    continue;
                }

                int op = b;
                if (op == 12)
                {
                    int b2 = data[pos++];
                    op = (12 << 8) | b2;
                }

                switch (op)
                {
                    case (int)CharstringOperator.HMoveTo:
                        {
                            double dx = Pop(stack);
                            state.CurX += dx;
                            state.PendingMoveTo = true;
                        }
                        break;
                    case (int)CharstringOperator.VMoveTo:
                        {
                            double dy = Pop(stack);
                            state.CurY += dy;
                            state.PendingMoveTo = true;
                        }
                        break;
                    case (int)CharstringOperator.RMoveTo:
                        {
                            double dy = Pop(stack);
                            double dx = Pop(stack);
                            state.CurX += dx;
                            state.CurY += dy;
                            state.PendingMoveTo = true;
                        }
                        break;
                    case (int)CharstringOperator.RLineTo:
                        while (stack.Count >= 2)
                        {
                            double dy = Pop(stack);
                            double dx = Pop(stack);
                            EnsureContour(state);
                            state.CurX += dx;
                            state.CurY += dy;
                            state.CurrentContour.LineTo(state.CurX, state.CurY);
                        }
                        break;
                    case (int)CharstringOperator.RRCurveTo:
                        while (stack.Count >= 6)
                        {
                            double dy3 = Pop(stack);
                            double dx3 = Pop(stack);
                            double dy2 = Pop(stack);
                            double dx2 = Pop(stack);
                            double dy1 = Pop(stack);
                            double dx1 = Pop(stack);
                            EnsureContour(state);
                            double c1x = state.CurX + dx1, c1y = state.CurY + dy1;
                            double c2x = c1x + dx2, c2y = c1y + dy2;
                            double ex = c2x + dx3, ey = c2y + dy3;
                            state.CurrentContour.CurveTo(c1x, c1y, c2x, c2y, ex, ey);
                            state.CurX = ex;
                            state.CurY = ey;
                        }
                        break;
                    case (int)CharstringOperator.CallSubr:
                        {
                            int subrIndex = (int)Pop(stack) + state.SubrBias;
                            GlyphContour shared;
                            if (!state.SharedContours.TryGetValue(subrIndex, out shared))
                            {
                                shared = DecodeSubrAsContour(state.Subrs[subrIndex], state.CurX, state.CurY);
                                state.SharedContours[subrIndex] = shared;
                            }
                            // moveto는 서브루틴 위치 지정용이므로 contour를 만들지 않음
                            state.PendingMoveTo = false;
                            state.Outline.AddContour(shared);
                            var lastCmd = shared.Commands[shared.Commands.Count - 1];
                            state.CurX = lastCmd.EndX;
                            state.CurY = lastCmd.EndY;
                            state.CurrentContour = shared;
                        }
                        break;
                    case (int)CharstringOperator.Return:
                        return;
                    case (int)CharstringOperator.EndChar:
                        return;
                    default:
                        throw new NotSupportedException($"Unsupported charstring operator: {op}");
                }
            }
        }

        private static double Pop(List<double> stack)
        {
            double v = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            return v;
        }

        /// <summary>
        /// moveto 이후 첫 그리기 명령에서 contour를 생성합니다. (lazy contour 생성)
        /// moveto+callsubr 패턴에서는 contour를 만들지 않아 서브루틴 contour가 중복되지 않습니다.
        /// </summary>
        private static void EnsureContour(CharstringState state)
        {
            if (state.PendingMoveTo)
            {
                state.CurrentContour = state.Outline.BeginContour();
                state.CurrentContour.MoveTo(state.CurX, state.CurY);
                state.PendingMoveTo = false;
            }
        }

        /// <summary>
        /// 서브루틴 charstring을 contour로 해독합니다. (moveto 없이 시작점 기준 상대 모양)
        /// 호출 측의 moveto 위치(originX, originY)를 contour 시작점으로 사용해 절대 좌표 contour를 만듭니다.
        /// 같은 서브루틴 인덱스는 캐시되어 여러 글리프가 같은 contour 인스턴스를 공유합니다.
        /// </summary>
        private static GlyphContour DecodeSubrAsContour(byte[] subrData, double originX, double originY)
        {
            var contour = new GlyphContour();
            contour.MoveTo(originX, originY);
            var state = new CharstringState
            {
                Outline = new GlyphOutline(),
                CurX = originX,
                CurY = originY,
                CurrentContour = contour,
                PendingMoveTo = false
            };
            int pos = 0;
            DecodeCharstringBody(subrData, ref pos, state);
            return contour;
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
    }
}
