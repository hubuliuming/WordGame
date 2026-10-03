namespace Code_01.CombatPrototype.Map
{
    public interface ICombatMapConfigSource
    {
        CombatMapConfigSet LoadValidated(string mapDefinitionId);
    }
}