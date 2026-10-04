using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class BiomeDefinitionConfig
    {
        public string biomeId;
        public string groundId;
        public string decorationObjectId;
        public string rockObjectId;
        public string treeObjectId;
        public string gatherObjectId;
        public string mineObjectId;
        public float decorationDensityPer100m2;
        public float treeDensityPer100m2;
        public float gatherableDensityPer100m2;
        public float rockDensityPer100m2;
        public float mineDensityPer100m2;
    }
}