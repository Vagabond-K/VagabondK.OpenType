namespace VagabondK.OpenType
{
    /// <summary>
    /// panose입니다. 특정 서체의 시각적 특성을 설명하는 10바이트 배열입니다.
    /// 이름이 다르지만 외관이 비슷한 폰트를 연관시키는 데 사용됩니다.
    /// 첫 번째 바이트(bFamilyType)에 따라 나머지 바이트의 의미가 달라집니다.
    /// </summary>
    public class PanoseProperties
    {
        /// <summary>bFamilyType입니다. 2=Latin Text.
        /// 이 값에 따라 나머지 바이트의 의미가 달라집니다.</summary>
        public int FamilyType { get; set; }

        /// <summary>bSerifStyle입니다. 0=No Serif, 1=Modern, 2=Old Style, 3=Slab Serif, 4=Freeform Serif.</summary>
        public int SerifStyle { get; set; }

        /// <summary>bWeight입니다. 1=Thin ~ 9=Black. 5=Medium.</summary>
        public int Weight { get; set; }

        /// <summary>bProportion입니다. 0=No Proportion, 1=Medium, 2=Condensed, 3=Expanded.</summary>
        public int Proportion { get; set; }

        /// <summary>bContrast입니다. 1=No Contrast ~ 9=High Contrast.</summary>
        public int Contrast { get; set; }

        /// <summary>bStrokeVariation입니다. 1=No Variation ~ 9=High Variation.</summary>
        public int StrokeVariation { get; set; }

        /// <summary>bArmStyle입니다. 0=Straight, 1=Unbracketed, 2=Bracketed, 3=Unbracketed Angled, 4=Bracketed Angled, 5=Unbracketed Slanted, 6=Bracketed Slanted, 7=Unbracketed Rounded, 8=Bracketed Rounded.</summary>
        public int ArmStyle { get; set; }

        /// <summary>bLetterform입니다. 0=Normal, 1=Lowercase, 2=Small Caps, 3=All Small Caps, 4=Display, 5=Flare, 6=Script.</summary>
        public int Letterform { get; set; }

        /// <summary>bMidline입니다. 0=Alphabetic, 1=Midpoint, 2=Low, 3=Hungry, 4=Generous, 5=Tight.</summary>
        public int Midline { get; set; }

        /// <summary>bXHeight입니다. 1=Extra-small Cap ~ 9=Extra-large Cap.</summary>
        public int XHeight { get; set; }

        /// <summary>
        /// <see cref="PanoseProperties"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        public PanoseProperties(int familyType, int serifStyle, int weight, int proportion, int contrast, int strokeVariation, int armStyle, int letterform, int midline, int xHeight)
        {
            FamilyType = familyType;
            SerifStyle = serifStyle;
            Weight = weight;
            Proportion = proportion;
            Contrast = contrast;
            StrokeVariation = strokeVariation;
            ArmStyle = armStyle;
            Letterform = letterform;
            Midline = midline;
            XHeight = xHeight;
        }
    }
}
