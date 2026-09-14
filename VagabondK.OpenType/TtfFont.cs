using System;
using System.Collections.Generic;
using System.Linq;
using VagabondK.OpenType.Tables;

namespace VagabondK.OpenType
{
    /// <summary>
    /// TrueType(TTF) 폰트 파일을 생성하는 클래스입니다.
    /// 글리프와 메타데이터를 추가한 후 <see cref="FontBase.ToBytes"/>를 호출하여
    /// sfnt 형식의 TTF 파일 바이트 배열을 생성합니다.
    /// sfnt 파일은 테이블 디렉터리와 각 테이블 데이터로 구성되며,
    /// glyf/loca/hmtx/cmap/maxp 테이블은 글리프에서 자동 생성됩니다.
    /// </summary>
    public class TtfFont : FontBase
    {
        /// <summary>
        /// <see cref="TtfFont"/>의 새 인스턴스를 생성합니다.
        /// 글리프 0은 .notdef(테두리만 있는 사각형) 글리프로 자동 생성됩니다.
        /// </summary>
        public TtfFont()
        {
        }

        /// <inheritdoc />
        protected override uint SfntVersion => 0x00010000; // sfnt version 1.0 (TrueType)

        /// <inheritdoc />
        protected override List<OpenTypeTable> CreateOutlineTables(int xMin, int yMin, int xMax, int yMax)
        {
            var glyfTable = new GlyfTable(Glyphs.Select(g => g.Outline).ToList());
            glyfTable.ToBytes(); // glyf 테이블 내 각 글리프의 오프셋 계산 (loca 테이블에 사용)
            var locaTable = new LocaTable(glyfTable.Offsets, Head.UseLongLoca);
            return new List<OpenTypeTable> { glyfTable, locaTable };
        }

        /// <inheritdoc />
        internal override MaxpTable CreateMaxpTable(int numGlyphs, int maxPoints, int maxContours)
        {
            // TrueType 윤곽은 maxp version 1.0(maxPoints/maxContours 포함)을 사용합니다.
            // glyf 직렬화 시 3차 베지어가 2개의 2차 베지어로 변환되어 점 수가 늘어날 수 있으므로,
            // 변환 후 점 수 기준으로 maxPoints를 재계산합니다.
            int ttfMaxPoints = 0;
            foreach (var glyph in Glyphs)
                ttfMaxPoints = Math.Max(ttfMaxPoints, GlyfTable.CountTtfPoints(glyph.Outline));
            return new MaxpTable(numGlyphs, ttfMaxPoints, maxContours);
        }
    }
}
