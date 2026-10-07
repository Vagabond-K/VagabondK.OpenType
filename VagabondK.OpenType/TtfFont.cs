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

            // loca short 형식은 오프셋을 2로 나눈 값을 uint16으로 저장하므로
            // glyf 오프셋이 131070(=65535*2)을 넘으면 long 형식을 써야 한다.
            // 사용자가 short를 지정했어도 한계를 넘으면 자동으로 승격한다 (그렇지 않으면 파일이 손상된다).
            bool needsLong = Head.UseLongLoca;
            if (!needsLong)
                foreach (int off in glyfTable.Offsets)
                    if (off > LocaTable.MaxShortOffset)
                    {
                        needsLong = true;
                        break;
                    }
            Head.UseLongLoca = needsLong;

            var locaTable = new LocaTable(glyfTable.Offsets, needsLong);
            return new List<OpenTypeTable> { glyfTable, locaTable };
        }

        /// <inheritdoc />
        internal override MaxpTable CreateMaxpTable(int numGlyphs, int maxPoints, int maxContours)
        {
            // TrueType 윤곽은 maxp version 1.0(maxPoints/maxContours 포함)을 사용합니다.
            // maxPoints는 호출자(FontBase.ToBytes)가 glyf 직렬화 기준(3차 베지어는 2차로 확장된 후의 점 수)으로
            // 계산해 전달하므로 그대로 사용합니다.
            return new MaxpTable(numGlyphs, maxPoints, maxContours);
        }
    }
}
