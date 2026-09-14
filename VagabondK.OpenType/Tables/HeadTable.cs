using System;
using VagabondK.OpenType;

namespace VagabondK.OpenType.Tables
{
    /// <summary>
    /// head 테이블(폰트 헤더)입니다.
    /// 폰트에 대한 전역 정보를 제공합니다.
    /// 바운딩 박스(xMin, yMin, xMax, yMax)는 contour가 있는 글리프만 사용하여 계산하며,
    /// contour가 없는 글리프는 무시합니다.
    /// </summary>
    public class HeadTable : OpenTypeTable
    {
        /// <summary>
        /// unitsPerEm입니다. 폰트 좌표 그리드의 해상도를 정의합니다.
        /// 16 이상 16384 이하의 값이 유효하며, TrueType 윤곽의 폰트는
        /// 일부 rasterizer의 성능 최적화를 위해 2의 거듭제곱을 권장합니다.
        /// 모든 글리프 좌표와 메트릭이 이 단위로 표현되며,
        /// 값이 클수록 좌표 정밀도가 높아집니다.
        /// </summary>
        public int UnitsPerEm
        {
            get => unitsPerEm;
            set
            {
                if (value < 16 || value > 16384)
                    throw new ArgumentOutOfRangeException(nameof(value), $"unitsPerEm must be between 16 and 16384 (got {value}).");
                unitsPerEm = value;
            }
        }
        private int unitsPerEm = 1000;

        /// <summary>
        /// fontRevision입니다 (16.16 고정 소수점). 폰트 제조사가 설정합니다.
        /// Windows는 이 값 대신 name 테이블의 version 문자열(nameID 5)을
        /// 폰트 버전 판정에 사용합니다.
        /// </summary>
        public double FontRevision { get; set; } = 1.0;

        /// <summary>
        /// lowestRecPPEM입니다. 픽셀 단위로 읽을 수 있는 최소 크기입니다.
        /// 이 크기 이하로 렌더링하면 가독성이 떨어질 수 있음을
        /// 애플리케이션에 알리는 데 사용됩니다.
        /// </summary>
        public int LowestRecPPEM { get; set; } = 8;

        /// <summary>
        /// indexToLocFormat입니다. loca 테이블의 오프셋 형식을 지정합니다.
        /// <c>false</c>(0)이면 short 형식(Offset16, 2바이트), <c>true</c>(1)이면 long 형식(Offset32, 4바이트)입니다.
        /// </summary>
        public bool UseLongLoca { get; set; }

        /// <summary>
        /// flags입니다. 폰트의 전역 동작을 제어하는 비트 플래그입니다.
        /// baseline 위치, left side bearing 기준점, ppem 정수 강제 등
        /// rasterizer의 동작에 영향을 줍니다.
        /// </summary>
        public HeadFlag Flags { get; set; } = HeadFlag.BaselineAtY0 | HeadFlag.LeftSidebearingAtX0 | HeadFlag.ForceIntegerPPem;

        /// <summary>
        /// macStyle입니다. 폰트의 스타일 플래그이며, OS/2 테이블의 fsSelection 비트와 일치해야 합니다.
        /// </summary>
        public MacStyleFlag MacStyle { get; set; }

        /// <summary>
        /// fontDirectionHint입니다. 더 이상 사용되지 않으며 2로 설정하는 것이 권장됩니다.
        /// </summary>
        public DirectionHint FontDirectionHint { get; set; } = DirectionHint.LeftToRightWithNeutrals;

        /// <summary>
        /// created입니다. 폰트가 처음 생성된 날짜와 시간입니다.
        /// 1904년 1월 1일 0시(UTC) 기준의 초(second) 수로 저장됩니다.
        /// </summary>
        public DateTime Created { get; set; } = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// modified입니다. 폰트가 마지막으로 수정된 날짜와 시간입니다.
        /// 1904년 1월 1일 0시(UTC) 기준의 초(second) 수로 저장됩니다.
        /// </summary>
        public DateTime Modified { get; set; } = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// checkSumAdjustment입니다. 0xB1B0AFBA - 전체 폰트 checksum으로 계산되며,
        /// 파일 조립 시 자동 설정됩니다.
        /// </summary>
        internal int CheckSumAdjustment { get; set; }

        /// <summary>
        /// xMin입니다. contour가 있는 모든 글리프의 바운딩 박스 중 최소 X 좌표입니다. 글리프에서 자동 계산됩니다.
        /// </summary>
        internal int XMin { get; set; }

        /// <summary>
        /// yMin입니다. contour가 있는 모든 글리프의 바운딩 박스 중 최소 Y 좌표입니다. 글리프에서 자동 계산됩니다.
        /// </summary>
        internal int YMin { get; set; }

        /// <summary>
        /// xMax입니다. contour가 있는 모든 글리프의 바운딩 박스 중 최대 X 좌표입니다. 글리프에서 자동 계산됩니다.
        /// </summary>
        internal int XMax { get; set; }

        /// <summary>
        /// yMax입니다. contour가 있는 모든 글리프의 바운딩 박스 중 최대 Y 좌표입니다. 글리프에서 자동 계산됩니다.
        /// </summary>
        internal int YMax { get; set; }

        /// <inheritdoc />
        internal override string Tag => "head";

        /// <inheritdoc />
        internal override byte[] ToBytes()
        {
            var writer = new OpenTypeBinaryWriter();
            writer.WriteUInt32(0x00010000); // version 1.0
            writer.WriteFixed(FontRevision); // fontRevision
            writer.WriteUInt32((uint)CheckSumAdjustment); // checkSumAdjustment
            writer.WriteUInt32(0x5F0F3CF5); // magicNumber
            writer.WriteUInt16((ushort)(int)Flags); // flags
            writer.WriteUInt16((ushort)UnitsPerEm); // unitsPerEm
            writer.WriteLongDateTime(Created); // created
            writer.WriteLongDateTime(Modified); // modified
            writer.WriteInt16((short)XMin);
            writer.WriteInt16((short)YMin);
            writer.WriteInt16((short)XMax);
            writer.WriteInt16((short)YMax);
            writer.WriteUInt16((ushort)(int)MacStyle); // macStyle
            writer.WriteUInt16((ushort)LowestRecPPEM); // lowestRecPPEM
            writer.WriteInt16((short)(int)FontDirectionHint); // fontDirectionHint
            writer.WriteUInt16(UseLongLoca ? (ushort)1 : (ushort)0); // indexToLocFormat
            writer.WriteUInt16(0); // glyphDataFormat
            return writer.ToArray();
        }

        /// <summary>
        /// head 테이블 바이트를 파싱하여 <see cref="HeadTable"/>을 생성합니다. <see cref="ToBytes"/>의 역입니다.
        /// </summary>
        /// <param name="reader">테이블 데이터를 읽는 리더입니다. (테이블 시작 위치)</param>
        internal static HeadTable FromBytes(OpenTypeBinaryReader reader)
        {
            var table = new HeadTable();
            reader.ReadUInt32(); // version
            table.FontRevision = reader.ReadFixed(); // fontRevision
            table.CheckSumAdjustment = (int)reader.ReadUInt32(); // checkSumAdjustment
            reader.ReadUInt32(); // magicNumber
            table.Flags = (HeadFlag)reader.ReadUInt16(); // flags
            table.UnitsPerEm = reader.ReadUInt16(); // unitsPerEm
            table.Created = reader.ReadDateTime(); // created
            table.Modified = reader.ReadDateTime(); // modified
            table.XMin = reader.ReadInt16();
            table.YMin = reader.ReadInt16();
            table.XMax = reader.ReadInt16();
            table.YMax = reader.ReadInt16();
            table.MacStyle = (MacStyleFlag)reader.ReadUInt16(); // macStyle
            table.LowestRecPPEM = reader.ReadUInt16(); // lowestRecPPEM
            table.FontDirectionHint = (DirectionHint)reader.ReadInt16(); // fontDirectionHint
            table.UseLongLoca = reader.ReadUInt16() == 1; // indexToLocFormat
            reader.ReadUInt16(); // glyphDataFormat
            return table;
        }
    }
}
