using Unity.Collections;
using Unity.Entities;

namespace Code_01.CombatPrototype.Map
{
    public struct CombatPrototypeMapResourcePersistenceSettings : IComponentData
    {
        public byte Enabled;
        public FixedString64Bytes SaveSlotId;
        public float SaveIntervalSeconds;
        public FixedString128Bytes LayoutSignature;
    }

    public enum CombatPrototypeMapResourceRestorePhase : byte { Pending, Ready, Failed }

    public struct CombatPrototypeMapResourceRestoreState : IComponentData
    {
        public CombatPrototypeMapResourceRestorePhase Phase;
    }

    internal enum CombatPrototypeMapResourceSaveKind : byte { Gather = 1, Tree = 2, Mine = 3 }

    internal struct CombatPrototypeMapResourceBinding
    {
        public Entity Entity;
        public CombatPrototypeMapResourceSaveKind Kind;
        public int PlacementIndex;
        public string ObjectId;
        public byte RegrowEnabled;
        public float RegrowSeconds;
        public int ObstacleIndex;

        public string SaveKind => Kind == CombatPrototypeMapResourceSaveKind.Gather ? "gather" :
            Kind == CombatPrototypeMapResourceSaveKind.Tree ? "tree" : "mine";
    }
}
