using System.Collections.Generic;

namespace CrazyMinnow.SALSA.OneClicks
{
    public class OneClickExpression
    {
        public string name;
        public List<OneClickComponent> components;

        public OneClickExpression(){}

        public OneClickExpression(string name, List<OneClickComponent> components)
        {
            this.name = name;
            this.components = components;
        }
    }

    public class OneClickEmoterExpression : OneClickExpression
    {
        public bool isRandom = false;
        public bool isEmphasis = false;
        public bool isAlwaysEmphasis = false;
        public bool isRepeater = false;
        public float expressionDynamics = 1.0f;

        public OneClickEmoterExpression(string name, List<OneClickComponent> components)
        {
            this.name = name;
            this.components = components;
        }

        public void SetEmoterBools(bool isRand, bool isEmph, bool isRep, float frac, bool isAlwaysEmph = false)
        {
            isRandom = isRand;
            isEmphasis = isEmph;
            isAlwaysEmphasis = isAlwaysEmph;
            isRepeater = isRep;
            expressionDynamics = frac;
        }
    }
}