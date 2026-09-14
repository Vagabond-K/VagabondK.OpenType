namespace VagabondK.OpenType.CFF
{
    /// <summary>
    /// CFF Private DICT 옵션입니다.
    /// Private DICT는 charstring의 width 계산과 스템 힌팅에 사용되는
    /// 폰트(또는 CID 폰트의 개별 FD) 단위 딕셔너리입니다.
    /// null인 항목은 Private DICT에 포함하지 않습니다.
    /// </summary>
    public class PrivateDict
    {
        /// <summary>
        /// StdHW(Standard Horizontal Width)입니다. (op 10) 기본값 70
        /// charstring에 width가 명시되지 않을 때 사용하는 기본 수평 advance width입니다. DefaultWidthX가 있으면 이 값이 우선합니다.
        /// </summary>
        public int? StdHW { get; set; } = 70;

        /// <summary>
        /// StdVW(Standard Vertical Width)입니다. (op 11) 기본값 90
        /// charstring에 width가 명시되지 않을 때 사용하는 기본 수직 advance width입니다.
        /// </summary>
        public int? StdVW { get; set; } = 90;

        /// <summary>
        /// BlueScale입니다. (op 12 9) 스템 힌팅 시 blue zone(수평 정렬 영역)으로 스냅하는 강도입니다.
        /// 0이면 스냅하지 않으며, 1에 가까울수록 강하게 스냅합니다.
        /// </summary>
        public double? BlueScale { get; set; }

        /// <summary>
        /// BlueShift입니다. (op 12 10) blue zone의 높이(두께)로, BlueValues의 각 값 주위에
        /// 이 값만큼의 허용 범위가 만들어집니다.
        /// </summary>
        public int? BlueShift { get; set; }

        /// <summary>
        /// BlueFuzz입니다. (op 12 11) 스템이 blue zone에 스냅될 수 있는 최대 거리로,
        /// 이 범위를 벗어나면 스냅하지 않습니다.
        /// </summary>
        public int? BlueFuzz { get; set; }

        /// <summary>
        /// ForceBold입니다. (op 12 14) 0이 아니면 스템 힌팅 시 모든 스템을 최소 1픽셀 두께로 강제합니다.
        /// 얇은 글리프가 작은 크기에서 사라지는 것을 방지합니다.
        /// </summary>
        public int? ForceBold { get; set; }

        /// <summary>
        /// LanguageGroup입니다. (op 12 17) 스템 힌팅에 사용할 언어 그룹을 지정합니다.
        /// 0=Latin, 1=JIS Level 1, 2=JIS Level 2, 3=GB Level 1, 4=GB Level 2, 5=Korean Level 1, 6=Korean Level 2
        /// </summary>
        public int? LanguageGroup { get; set; }

        /// <summary>
        /// ExpansionFactor입니다. (op 12 18) 스템 힌팅 시 스템을 확장할 수 있는 최대 비율입니다.
        /// </summary>
        public double? ExpansionFactor { get; set; }

        /// <summary>
        /// InitialRandomSeed입니다. (op 12 19) 스템 힌팅의 결정적 난수 시드입니다.
        /// </summary>
        public int? InitialRandomSeed { get; set; }

        /// <summary>
        /// DefaultWidthX입니다. (op 20) charstring에 width가 명시되지 않을 때 사용하는 기본 수평 advance width입니다.
        /// 이 값이 있으면 StdHW를 우선합니다.
        /// </summary>
        public int? DefaultWidthX { get; set; }

        /// <summary>
        /// NominalWidthX입니다. (op 21) charstring의 width가 이 값과 같으면 width를 기록하지 않고
        /// DefaultWidthX를 사용하도록 허용하는 기준 width로, charstring 크기를 줄이는 데 사용됩니다.
        /// </summary>
        public int? NominalWidthX { get; set; }

        /// <summary>
        /// VsIndex입니다. (op 22) 수직 스템 힌팅에 사용할 vsindex입니다.
        /// </summary>
        public int? VsIndex { get; set; }

        /// <summary>
        /// BlueValues입니다. (op 6) 절대값 배열입니다. (delta로 기록)
        /// 소문자 x-height, 대문자 cap-height 등 수평 정렬에 사용되는 y 좌표로,
        /// 오름차순으로 정렬되어야 하며 스템 힌팅 시 이 값으로 스냅합니다.
        /// </summary>
        public int[] BlueValues { get; set; }

        /// <summary>
        /// OtherBlues입니다. (op 7) 절대값 배열입니다. (delta로 기록)
        /// descender, underscore 등 BlueValues 외의 수평 정렬 y 좌표로, 오름차순으로 정렬되어야 합니다.
        /// </summary>
        public int[] OtherBlues { get; set; }

        /// <summary>
        /// FamilyBlues입니다. (op 8) 절대값 배열입니다. (delta로 기록)
        /// 같은 family의 모든 스타일에서 공유하는 BlueValues로, family 간 일관된 정렬을 보장합니다.
        /// </summary>
        public int[] FamilyBlues { get; set; }

        /// <summary>
        /// FamilyOtherBlues입니다. (op 9) 절대값 배열입니다. (delta로 기록)
        /// 같은 family의 모든 스타일에서 공유하는 OtherBlues입니다.
        /// </summary>
        public int[] FamilyOtherBlues { get; set; }

        /// <summary>
        /// StemSnapH입니다. (op 12 12) 절대값 배열입니다. (delta로 기록)
        /// 수평 스템(가로 선)의 허용 두께 목록으로, 스템 힌팅 시 이 값 중 가장 가까운 값으로 스냅합니다.
        /// 오름차순으로 정렬되어야 합니다.
        /// </summary>
        public int[] StemSnapH { get; set; }

        /// <summary>
        /// StemSnapV입니다. (op 12 13) 절대값 배열입니다. (delta로 기록)
        /// 수직 스템(세로 선)의 허용 두께 목록으로, 스템 힌팅 시 이 값 중 가장 가까운 값으로 스냅합니다.
        /// 오름차순으로 정렬되어야 합니다.
        /// </summary>
        public int[] StemSnapV { get; set; }
    }
}
