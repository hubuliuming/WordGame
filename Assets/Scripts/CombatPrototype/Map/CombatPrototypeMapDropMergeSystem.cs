using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypeMapDropMotionSystem))]
    [UpdateBefore(typeof(CombatPrototypeMapDropPickupSystem))]
    public partial class CombatPrototypeMapDropMergeSystem : SystemBase
    {
        private EntityQuery _maps;
        private Entity _source;
        private double _nextScanAt;
        private readonly List<CombatPrototypeMapDropMergeUtility.Candidate> _candidates = new List<CombatPrototypeMapDropMergeUtility.Candidate>();
        private readonly HashSet<int> _candidateIds = new HashSet<int>();

        protected override void OnCreate()
        {
            _maps = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapData>());
            RequireForUpdate(_maps);
        }

        protected override void OnUpdate()
        {
            var source = Entity.Null;
            FixedString64Bytes mapId = default;
            var stage = "ReadMap";
            var time = SystemAPI.Time.ElapsedTime;
            CombatPrototypeMapDropMergeSettings settings;
            IReadOnlyList<Entity> owned;
            int lastDropId;
            try
            {
                source = _maps.GetSingletonEntity();
                mapId = EntityManager.GetComponentData<CombatPrototypeMapData>(source).MapDefinitionId;
                if (_source != source) { Reset(); _source = source; }
                stage = "ReadSettings";
                settings = EntityManager.GetComponentData<CombatPrototypeMapDropMergeSettings>(source);
                var restore = EntityManager.GetComponentData<CombatPrototypeMapResourceRestoreState>(source);
                if (settings.Enabled == 0 || restore.Phase != CombatPrototypeMapResourceRestorePhase.Ready || time < _nextScanAt) return;
                _nextScanAt = time + settings.ScanInterval;
                stage = "ReadOwner";
                Dependency.Complete();
                var owner = World.GetExistingSystemManaged<CombatPrototypeMapDropSpawnSystem>();
                owned = owner.GetOwnedDrops(source);
                lastDropId = owner.GetLastAllocatedDropId(source);
            }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] Drop merge batch failed; stage=" + stage + ", map=" + mapId +
                    ", source=" + source + ". " + exception);
                return;
            }

            ReadCandidates(owned, lastDropId, mapId, time);
            _candidates.Sort(CombatPrototypeMapDropMergeUtility.ByDropId);
            var states = GetComponentLookup<CombatPrototypeMapDropState>(false);
            var progresses = GetComponentLookup<CombatPrototypeMapDropProgress>(false);
            for (var index = 0; index < _candidates.Count; index++)
            {
                var candidate = _candidates[index];
                var targetId = 0;
                stage = "SelectPair";
                try
                {
                    var targetIndex = CombatPrototypeMapDropMergeUtility.SelectTarget(_candidates, index, settings.MergeDistance, settings.MaxStackQuantity);
                    if (targetIndex < 0) continue;
                    var target = _candidates[targetIndex];
                    targetId = target.DropId;
                    stage = "CommitPair";
                    CombatPrototypeMapDropMergeUtility.Merge(candidate, target, settings.MaxStackQuantity, time,
                        states, progresses, out var quantity, out var expiresAt);
                    var oldQuantity = target.Quantity;
                    target.Quantity = quantity;
                    target.ExpiresAt = expiresAt;
                    candidate.Absorbed = true;
                    _candidates[targetIndex] = target;
                    _candidates[index] = candidate;
                    stage = "LogMerge";
                    Debug.Log("[CombatPrototype.Map] Drops merged; map=" + mapId + ", sourceDropId=" + candidate.DropId +
                        ", targetDropId=" + targetId + ", itemId=" + candidate.ItemId + ", sourceQuantity=" + candidate.Quantity +
                        ", targetQuantityBefore=" + oldQuantity + ", quantity=" + quantity + ", expiresAt=" + expiresAt + ".");
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.Map] Drop merge failed; stage=" + stage + ", map=" + mapId +
                        ", sourceDropId=" + candidate.DropId + ", targetDropId=" + targetId + ", itemId=" + candidate.ItemId +
                        ", quantity=" + candidate.Quantity + ", entity=" + candidate.Entity + ". " + exception);
                }
            }
        }

        private void ReadCandidates(IReadOnlyList<Entity> owned, int lastDropId, FixedString64Bytes mapId, double time)
        {
            _candidates.Clear();
            _candidateIds.Clear();
            for (var index = 0; index < owned.Count; index++)
            {
                var entity = owned[index];
                var dropId = 0;
                FixedString64Bytes itemId = default;
                try
                {
                    // The owner retains references to drops already removed by normal EndSimulation ECB playback.
                    if (!EntityManager.Exists(entity)) continue;
                    var drop = EntityManager.GetComponentData<CombatPrototypeMapDropState>(entity);
                    dropId = drop.DropId;
                    itemId = drop.ItemId;
                    var progress = EntityManager.GetComponentData<CombatPrototypeMapDropProgress>(entity);
                    if (!CombatPrototypeMapDropMergeUtility.IsEligible(drop, progress, time)) continue;
                    var position = EntityManager.GetComponentData<LocalTransform>(entity).Position;
                    CombatPrototypeMapDropMergeUtility.ValidateCandidate(drop, progress, position, lastDropId);
                    if (!_candidateIds.Add(dropId)) throw new InvalidOperationException("Duplicate owned drop ID.");
                    _candidates.Add(new CombatPrototypeMapDropMergeUtility.Candidate
                    {
                        Entity = entity, DropId = dropId, ItemId = itemId, Quantity = drop.Quantity,
                        Position = position, ExpiresAt = progress.ExpiresAt
                    });
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.Map] Drop merge candidate failed; stage=ReadCandidate, map=" + mapId +
                        ", DropId=" + dropId + ", itemId=" + itemId + ", ownerIndex=" + index +
                        ", entity=" + entity + ". " + exception);
                }
            }
        }

        private void Reset()
        {
            _source = Entity.Null;
            _nextScanAt = 0d;
            _candidates.Clear();
            _candidateIds.Clear();
        }

        protected override void OnStopRunning() { Reset(); }
        protected override void OnDestroy() { Reset(); }
    }
}
