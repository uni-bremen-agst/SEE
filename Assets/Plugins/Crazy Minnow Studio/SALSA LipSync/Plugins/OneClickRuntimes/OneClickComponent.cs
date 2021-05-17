namespace CrazyMinnow.SALSA.OneClicks
{
    public class OneClickComponent
    {
        public ComponentType type;
        public string componentName;
        public float durOn;
        public float durHold;
        public float durOff;

        public enum ComponentType
        {
            Shape,
            UMA,
            Bone,
            Animator
        }
    }

    public class OneClickShapeComponent : OneClickComponent
    {
        public string[] blendshapeNames;
        public float maxAmount;
        public bool useRegex = false;

        public OneClickShapeComponent(string componentName,
            string[] blendshapeNames,
            float maxAmount,
            float durOn,
            float durHold,
            float durOff,
            ComponentType type,
            bool useRegex)
        {
            this.componentName = componentName;
            this.blendshapeNames = blendshapeNames;
            this.maxAmount = maxAmount;
            this.durOn = durOn;
            this.durHold = durHold;
            this.durOff = durOff;
            this.type = type;
            this.useRegex = useRegex;
        }
    }

    public class OneClickBoneComponent : OneClickComponent
    {
        public string componentSearchName;
        public TformBase max;
        public bool usePos;
        public bool useRot;
        public bool useScl;

        public OneClickBoneComponent(string componentName,
            string boneSearchName,
            TformBase max,
            bool usePos,
            bool useRot,
            bool useScl,
            float durOn,
            float durHold,
            float durOff,
            ComponentType type)
        {
            this.componentSearchName = boneSearchName;
            this.componentName = componentName;
            this.max = max;
            this.usePos = usePos;
            this.useRot = useRot;
            this.useScl = useScl;
            this.durOn = durOn;
            this.durHold = durHold;
            this.durOff = durOff;
            this.type = type;
        }
    }

    public class OneClickUepComponent : OneClickComponent
    {
        public string poseName;
        public float maxAmount;

        public OneClickUepComponent(string componentName,
            string poseName,
            float maxAmount,
            float durOn,
            float durHold,
            float durOff,
            ComponentType type)
        {
            this.componentName = componentName;
            this.poseName = poseName;
            this.maxAmount = maxAmount;
            this.durOn = durOn;
            this.durHold = durHold;
            this.durOff = durOff;
            this.type = type;
        }
    }

    public class OneClickAnimatorComponent : OneClickComponent
    {
        public string componentSearchName;
        public int animationParmIndex;
        public bool isTriggerParmBiDirectional;

        public OneClickAnimatorComponent(string componentName,
            string animatorSearchName,
            int animationParmIndex,
            bool isTriggerParmBiDirectional,
            float durOn,
            float durHold,
            float durOff,
            ComponentType type)
        {
            this.componentName = componentName;
            this.componentSearchName = animatorSearchName;
            this.animationParmIndex = animationParmIndex;
            this.isTriggerParmBiDirectional = isTriggerParmBiDirectional;
            this.durOn = durOn;
            this.durHold = durHold;
            this.durOff = durOff;
            this.type = type;
        }
    }
}