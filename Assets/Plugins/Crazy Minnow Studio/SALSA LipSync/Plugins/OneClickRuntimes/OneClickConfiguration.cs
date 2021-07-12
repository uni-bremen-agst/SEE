using System.Collections.Generic;

namespace CrazyMinnow.SALSA.OneClicks
{
    public class OneClickConfiguration
    {
        public ConfigType type;
        public List<string> smrSearches = new List<string>();
        public List<OneClickExpression> oneClickExpressions = new List<OneClickExpression>();

        public enum ConfigType
        {
            Salsa,
            Emoter
        }

        public OneClickConfiguration(ConfigType type)
        {
            this.type = type;
            smrSearches.Clear();
            oneClickExpressions.Clear();
        }
    }
}