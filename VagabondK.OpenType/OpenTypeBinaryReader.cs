using System;
using System.IO;
using System.Text;

namespace VagabondK.OpenType
{
    /// <summary>
    /// OpenType 바이너리 데이터를 빅 엔디안(네트워크 바이트 순서)으로 읽는 클래스입니다.
    /// <see cref="OpenTypeBinaryWriter"/>의 역으로, 스트림에서 다바이트 데이터 타입
    /// (uint16, uint32, Fixed, FWORD 등)을 빅 엔디안으로 읽습니다.
    /// 스트림의 <see cref="Stream.Position"/>을 직접 조작해 순차 읽기와 임의 접근(seek)을 모두 지원합니다.
    /// </summary>
    class OpenTypeBinaryReader
    {
        private readonly Stream stream;

        /// <summary>
        /// <see cref="OpenTypeBinaryReader"/>의 새 인스턴스를 생성합니다.
        /// 스트림의 소유권은 호출자에게 있으며, 이 클래스는 스트림을 닫지 않습니다.
        /// </summary>
        /// <param name="stream">읽을 스트림입니다. 읽기(read)와 위치 지정(seek)이 가능해야 합니다.</param>
        public OpenTypeBinaryReader(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException("The stream must be readable.", nameof(stream));
            if (!stream.CanSeek)
                throw new ArgumentException("The stream must be seekable.", nameof(stream));
            this.stream = stream;
        }

        /// <summary>
        /// 현재 스트림 위치(바이트)를 반환/설정합니다.
        /// 테이블 임의 접근(오프셋으로 이동)에 사용됩니다.
        /// </summary>
        public long Position
        {
            get => stream.Position;
            set => stream.Position = value;
        }

        /// <summary>
        /// uint8(1바이트) 값을 읽습니다.
        /// </summary>
        public byte ReadUInt8()
        {
            int b = stream.ReadByte();
            if (b < 0)
                throw new EndOfStreamException("Unexpected end of stream.");
            return (byte)b;
        }

        /// <summary>
        /// uint16(2바이트) 값을 빅 엔디안으로 읽습니다.
        /// </summary>
        public ushort ReadUInt16()
        {
            int hi = stream.ReadByte();
            int lo = stream.ReadByte();
            if (hi < 0 || lo < 0)
                throw new EndOfStreamException("Unexpected end of stream.");
            return (ushort)((hi << 8) | lo);
        }

        /// <summary>
        /// uint24(3바이트) 값을 빅 엔디안으로 읽습니다.
        /// </summary>
        public uint ReadUInt24()
        {
            int b0 = stream.ReadByte();
            int b1 = stream.ReadByte();
            int b2 = stream.ReadByte();
            if (b0 < 0 || b1 < 0 || b2 < 0)
                throw new EndOfStreamException("Unexpected end of stream.");
            return (uint)((b0 << 16) | (b1 << 8) | b2);
        }

        /// <summary>
        /// uint32(4바이트) 값을 빅 엔디안으로 읽습니다.
        /// </summary>
        public uint ReadUInt32()
        {
            int b0 = stream.ReadByte();
            int b1 = stream.ReadByte();
            int b2 = stream.ReadByte();
            int b3 = stream.ReadByte();
            if (b0 < 0 || b1 < 0 || b2 < 0 || b3 < 0)
                throw new EndOfStreamException("Unexpected end of stream.");
            return (uint)((b0 << 24) | (b1 << 16) | (b2 << 8) | b3);
        }

        /// <summary>
        /// int8(1바이트) 값을 읽습니다.
        /// </summary>
        public sbyte ReadInt8() => (sbyte)ReadUInt8();

        /// <summary>
        /// int16(2바이트, FWORD) 값을 빅 엔디안으로 읽습니다.
        /// </summary>
        public short ReadInt16() => (short)ReadUInt16();

        /// <summary>
        /// int32(4바이트, LONG) 값을 빅 엔디안으로 읽습니다.
        /// </summary>
        public int ReadInt32() => (int)ReadUInt32();

        /// <summary>
        /// long date time(8바이트, int64) 값을 빅 엔디안으로 읽습니다.
        /// 1904년 1월 1일 0시(UTC) 기준의 초(second) 수를 반환합니다.
        /// </summary>
        public long ReadLongDateTime()
        {
            long hi = (long)ReadUInt32();
            long lo = ReadUInt32();
            return (hi << 32) | lo;
        }

        /// <summary>
        /// long date time(8바이트)을 <see cref="DateTime"/>로 변환하여 읽습니다.
        /// 1904년 1월 1일 0시(UTC) 기준의 초(second) 수를 <see cref="DateTime"/>로 변환합니다.
        /// </summary>
        public DateTime ReadDateTime()
        {
            long seconds = ReadLongDateTime();
            long epochTicks = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
            return new DateTime(epochTicks + seconds * TimeSpan.TicksPerSecond, DateTimeKind.Utc);
        }

        /// <summary>
        /// Fixed(16.16 고정 소수점, 4바이트) 값을 빅 엔디안으로 읽습니다.
        /// </summary>
        public double ReadFixed()
        {
            uint raw = ReadUInt32();
            return (int)raw / 65536.0;
        }

        /// <summary>
        /// F2Dot14(2.14 고정 소수점, 2바이트) 값을 빅 엔디안으로 읽습니다.
        /// </summary>
        public double ReadF2Dot14()
        {
            ushort raw = ReadUInt16();
            return raw / 16384.0;
        }

        /// <summary>
        /// Tag(4바이트 ASCII 문자열)을 읽습니다.
        /// </summary>
        public string ReadTag()
        {
            byte[] bytes = ReadBytes(4);
            return Encoding.ASCII.GetString(bytes);
        }

        /// <summary>
        /// 지정된 길이의 바이트 배열을 읽습니다.
        /// </summary>
        /// <param name="count">읽을 바이트 수입니다.</param>
        public byte[] ReadBytes(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read <= 0)
                    throw new EndOfStreamException("Unexpected end of stream.");
                offset += read;
            }
            return buffer;
        }

        /// <summary>
        /// 문자열을 UTF-16 빅 엔디안 인코딩으로 읽습니다.
        /// name 테이블의 Windows 플랫폼(platformID 3, encodingID 1) 문자열 값에 사용됩니다.
        /// </summary>
        /// <param name="charCount">읽을 문자 개수입니다.</param>
        public string ReadUtf16BigEndian(int charCount)
        {
            byte[] bytes = ReadBytes(charCount * 2);
            char[] chars = new char[charCount];
            for (int i = 0; i < charCount; i++)
                chars[i] = (char)((bytes[i * 2] << 8) | bytes[i * 2 + 1]);
            return new string(chars);
        }
    }
}
