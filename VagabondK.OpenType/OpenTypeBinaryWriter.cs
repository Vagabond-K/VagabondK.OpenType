using System;
using System.Collections.Generic;

namespace VagabondK.OpenType
{
    /// <summary>
    /// OpenType 바이너리 데이터를 빅 엔디안(네트워크 바이트 순서)으로 기록하는 클래스입니다.
    /// OpenType 스펙의 모든 다바이트 데이터 타입(uint16, uint32, Fixed, FWORD 등)은
    /// 빅 엔디안으로 저장되므로, 테이블 생성 시 이 클래스를 통해 값을 기록합니다.
    /// </summary>
    class OpenTypeBinaryWriter
    {
        private List<byte> buffer;

        /// <summary>
        /// 초기 용량을 지정하여 <see cref="OpenTypeBinaryWriter"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="capacity">예상되는 바이트 수입니다.</param>
        public OpenTypeBinaryWriter(int capacity = 256)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            buffer = new List<byte>(capacity);
        }

        /// <summary>
        /// 현재까지 기록된 바이트 수를 반환합니다.
        /// </summary>
        public int Length => buffer.Count;

        /// <summary>
        /// uint8(1바이트) 값을 기록합니다.
        /// </summary>
        /// <param name="value">기록할 값입니다.</param>
        public void WriteUInt8(byte value) => buffer.Add(value);

        /// <summary>
        /// uint16(2바이트) 값을 빅 엔디안으로 기록합니다.
        /// OpenType의 uint16, FWORD, UFWORD 등 2바이트 타입에 사용됩니다.
        /// </summary>
        /// <param name="value">기록할 값입니다.</param>
        public void WriteUInt16(ushort value)
        {
            buffer.Add((byte)(value >> 8));
            buffer.Add((byte)value);
        }

        /// <summary>
        /// uint24(3바이트) 값을 빅 엔디안으로 기록합니다.
        /// </summary>
        /// <param name="value">기록할 값입니다.</param>
        public void WriteUInt24(uint value)
        {
            buffer.Add((byte)(value >> 16));
            buffer.Add((byte)(value >> 8));
            buffer.Add((byte)value);
        }

        /// <summary>
        /// uint32(4바이트, long) 값을 빅 엔디안으로 기록합니다.
        /// OpenType의 uint32, LONGDATETIME(하위 32비트) 등에 사용됩니다.
        /// </summary>
        /// <param name="value">기록할 값입니다.</param>
        public void WriteUInt32(uint value)
        {
            buffer.Add((byte)(value >> 24));
            buffer.Add((byte)(value >> 16));
            buffer.Add((byte)(value >> 8));
            buffer.Add((byte)value);
        }

        /// <summary>
        /// int8(1바이트) 값을 기록합니다.
        /// </summary>
        /// <param name="value">기록할 값입니다.</param>
        public void WriteInt8(sbyte value) => buffer.Add((byte)value);

        /// <summary>
        /// int16(2바이트, FWORD) 값을 빅 엔디안으로 기록합니다.
        /// FWORD는 부호 있는 16비트 값으로, hhea의 ascender/descender,
        /// hmtx의 left side bearing, OS/2의 sTypoAscender 등에 사용됩니다.
        /// </summary>
        /// <param name="value">기록할 값입니다.</param>
        public void WriteInt16(short value) => WriteUInt16((ushort)value);

        /// <summary>
        /// int32(4바이트, LONG) 값을 빅 엔디안으로 기록합니다.
        /// </summary>
        /// <param name="value">기록할 값입니다.</param>
        public void WriteInt32(int value) => WriteUInt32((uint)value);

        /// <summary>
        /// long date time(8바이트, int64) 값을 빅 엔디안으로 기록합니다.
        /// 1904년 1월 1일 0시(UTC) 기준의 초(second) 수를 나타내며,
        /// head 테이블의 created, modified 필드에 사용됩니다.
        /// </summary>
        /// <param name="value">기록할 값입니다.</param>
        public void WriteLongDateTime(long value)
        {
            WriteUInt32((uint)(value >> 32));
            WriteUInt32((uint)value);
        }

        /// <summary>
        /// <see cref="DateTime"/> 값을 long date time(8바이트)으로 변환하여 빅 엔디안으로 기록합니다.
        /// 1904년 1월 1일 0시(UTC) 기준의 초(second) 수로 변환됩니다.
        /// </summary>
        /// <param name="value">기록할 날짜와 시간입니다.</param>
        public void WriteLongDateTime(DateTime value)
        {
            long epochTicks = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
            WriteLongDateTime((value.ToUniversalTime().Ticks - epochTicks) / TimeSpan.TicksPerSecond);
        }

        /// <summary>
        /// Fixed(16.16 고정 소수점, 4바이트) 값을 빅 엔디안으로 기록합니다.
        /// head의 fontRevision, post의 italicAngle 등에 사용됩니다.
        /// </summary>
        /// <param name="value">기록할 값입니다.</param>
        public void WriteFixed(double value) => WriteUInt32(unchecked((uint)(int)Math.Round(value * 65536.0)));

        /// <summary>
        /// F2Dot14(2.14 고정 소수점, 2바이트) 값을 빅 엔디안으로 기록합니다.
        /// glyf의 composite glyph scale 변환에 사용됩니다.
        /// </summary>
        /// <param name="value">기록할 값입니다.</param>
        public void WriteF2Dot14(double value) => WriteUInt16((ushort)Math.Round(value * 16384.0));

        /// <summary>
        /// Tag(4바이트 ASCII 문자열)을 기록합니다.
        /// </summary>
        /// <param name="tag">4자리의 태그 문자열입니다. (예: "head", "glyf")</param>
        public void WriteTag(string tag)
        {
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            if (tag.Length != 4)
                throw new ArgumentException("The tag must be exactly 4 characters.", nameof(tag));

            foreach (char c in tag)
                buffer.Add((byte)c);
        }

        /// <summary>
        /// 바이트 배열을 그대로 기록합니다.
        /// </summary>
        /// <param name="bytes">기록할 바이트 배열입니다.</param>
        public void WriteBytes(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            buffer.AddRange(bytes);
        }

        /// <summary>
        /// 문자열을 UTF-16 빅 엔디안 인코딩으로 기록합니다.
        /// name 테이블의 Windows 플랫폼(platformID 3, encodingID 1) 문자열 값에 사용됩니다.
        /// </summary>
        /// <param name="value">기록할 문자열입니다.</param>
        public void WriteUtf16BigEndian(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            foreach (char c in value)
                WriteUInt16(c);
        }

        /// <summary>
        /// 기록된 모든 바이트를 새 배열로 반환합니다.
        /// </summary>
        /// <returns>기록된 바이트 배열입니다.</returns>
        public byte[] ToArray() => buffer.ToArray();
    }
}
