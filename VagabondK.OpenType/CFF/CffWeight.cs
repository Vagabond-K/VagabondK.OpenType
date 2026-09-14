namespace VagabondK.OpenType.CFF
{
    /// <summary>
    /// CFF Top DICT의 Weight(op 4) 표준 이름입니다.
    /// CFF 스펙의 standard weight name 9개(Thin~Black)를 나타냅니다.
    /// enum 값은 OS/2 usWeightClass(<see cref="Weight"/>)와 일치시켜 <see cref="Weight"/>과의 매핑을 단순 캐스트로 만듭니다.
    /// 직렬화는 이름(ToString)으로 이루어지며, 숫자 값은 내부 전용입니다.
    /// </summary>
    public enum CffWeight
    {
        /// <summary>Thin(100).</summary>
        Thin = 100,

        /// <summary>ExtraLight(200).</summary>
        ExtraLight = 200,

        /// <summary>Light(300).</summary>
        Light = 300,

        /// <summary>Regular(400). OS/2의 Normal(400)에 대응합니다.</summary>
        Regular = 400,

        /// <summary>Medium(500).</summary>
        Medium = 500,

        /// <summary>SemiBold(600).</summary>
        SemiBold = 600,

        /// <summary>Bold(700).</summary>
        Bold = 700,

        /// <summary>ExtraBold(800).</summary>
        ExtraBold = 800,

        /// <summary>Black(900).</summary>
        Black = 900
    }
}
