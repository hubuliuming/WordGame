namespace Code_01.CombatPrototype.Map
{
    [global::System.Serializable]
    public sealed class CombatMapConfigSet
    {
        public MapDefinitionConfig map;
        public BiomeDefinitionConfig[] biomes;
        public GroundDefinitionConfig[] grounds;
        public MapObjectDefinitionConfig[] objects;
    }
}