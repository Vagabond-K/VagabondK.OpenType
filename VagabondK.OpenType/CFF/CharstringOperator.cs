namespace VagabondK.OpenType.CFF
{
    /// <summary>
    /// Type 2 charstring operator입니다.
    /// 글리프 윤곽을 그리는 charstring 명령어입니다.
    /// DICT operator와 번호가 겹칠 수 있어 별도 enum으로 구분합니다.
    /// </summary>
    enum CharstringOperator
    {
        // 1바이트 operator (2, 9, 12, 13, 17, 28은 예약, 12는 2바이트 prefix)
        /// <summary>hstem입니다. y 좌표와 두께를 수평 스템 힌트에 등록합니다.</summary>
        HStem = 1,
        /// <summary>vstem입니다. x 좌표와 두께를 수직 스템 힌트에 등록합니다.</summary>
        VStem = 3,
        /// <summary>vmoveto입니다. 현재 y 기준 상대 수직 이동으로, x는 0으로 고정됩니다.</summary>
        VMoveTo = 4,
        /// <summary>rlineto입니다. (dx, dy) 쌍을 반복하여 여러 상대 직선을 그립니다.</summary>
        RLineTo = 5,
        /// <summary>hlineto입니다. dx만 기록하는 수평 상대 직선입니다.</summary>
        HLineTo = 6,
        /// <summary>vlineto입니다. dy만 기록하는 수직 상대 직선입니다.</summary>
        VLineTo = 7,
        /// <summary>rrcurveto입니다. (dx1, dy1, dx2, dy2, dx3, dy3) 6개 값이 누적 상대 오프셋으로
        /// 기록되는 상대 3차 베지어 곡선입니다 (c1−start, c2−c1, end−c2).</summary>
        RRCurveTo = 8,
        /// <summary>callsubr입니다. Private Subrs INDEX의 서브루틴을 호출합니다.
        /// 인덱스에 bias를 더한 값이 실제 서브루틴 인덱스입니다.</summary>
        CallSubr = 10,
        /// <summary>return입니다. 서브루틴 charstring은 반드시 이 operator로 끝나야 합니다.</summary>
        Return = 11,
        /// <summary>endchar입니다. charstring을 종료하며, 선택적으로 width 값을 포함할 수 있습니다.</summary>
        EndChar = 14,
        /// <summary>vsindex입니다. vertical stem index. vstemhm의 인덱스 지정에 사용됩니다.</summary>
        VsIndex = 15,
        /// <summary>blend입니다. CID 블렌딩.</summary>
        Blend = 16,
        /// <summary>hstemhm입니다. 수평 스템 힌트(mask 포함).</summary>
        HStemHm = 18,
        /// <summary>hintmask입니다.</summary>
        HintMask = 19,
        /// <summary>cntrmask입니다.</summary>
        CntrMask = 20,
        /// <summary>rmoveto입니다. (dx, dy)로 현재 포인트를 상대 이동합니다.</summary>
        RMoveTo = 21,
        /// <summary>hmoveto입니다. 현재 x 기준 상대 수평 이동으로, y는 0으로 고정됩니다.</summary>
        HMoveTo = 22,
        /// <summary>vstemhm입니다. 수직 스템 힌트(mask 포함).</summary>
        VStemHm = 23,
        /// <summary>rcrc입니다. 상대 곡선-직선.</summary>
        RCurveLine = 24,
        /// <summary>rrlc입니다. 상대 직선-곡선.</summary>
        RLineCurve = 25,
        /// <summary>vvcurveto입니다. 수직-수직 상대 곡선.</summary>
        VVCurveTo = 26,
        /// <summary>hhcurveto입니다. 수평-수평 상대 곡선.</summary>
        HHCurveTo = 27,
        /// <summary>callgsubr입니다. Global Subrs INDEX의 서브루틴을 호출합니다.</summary>
        CallGSubr = 29,
        /// <summary>vhcurveto입니다. 수직-수평 상대 곡선.</summary>
        VHCurveTo = 30,
        /// <summary>hvcurveto입니다. 수평-수직 상대 곡선.</summary>
        HVCurveTo = 31
    }
}
