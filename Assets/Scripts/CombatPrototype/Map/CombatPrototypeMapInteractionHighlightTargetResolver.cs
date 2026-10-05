using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    internal sealed class CombatPrototypeMapInteractionHighlightTargetResolver
    {
        private readonly EntityQuery _gather;
        private readonly EntityQuery _trees;
        private readonly EntityQuery _mines;
        private readonly EntityQuery _drops;
        private Entity _fEntity;
        private Entity _gEntity;
        private byte _fKind;
        private int _fPlacement = -1;
        private int _gDropId;

        internal CombatPrototypeMapInteractionHighlightTargetResolver(EntityQuery gather, EntityQuery trees,
            EntityQuery mines, EntityQuery drops)
        {
            _gather = gather;
            _trees = trees;
            _mines = mines;
            _drops = drops;
        }

        internal void Resolve(EntityManager manager, string worldName, CombatPrototypeMapData map,
            CombatPrototypeMapInteractionHighlightSettings settings, int ownerNetworkId,
            CombatPrototypeMapInteractionHudState interaction, CombatPrototypeMapPickupHudState pickup,
            out CombatPrototypeMapInteractionHighlightFrame f, out CombatPrototypeMapInteractionHighlightFrame g)
        {
            f = g = default;
            if (settings.Enabled == 0) { Reset(); return; }
            if (settings.FTargetsEnabled != 0)
            {
                try { f = ResolveF(manager, map, settings, ownerNetworkId, interaction); }
                catch (Exception exception)
                {
                    ResetF();
                    Debug.LogError("[CombatPrototype.Map] Highlight target failed; stage=ResolveF, World=" + worldName +
                        ", map=" + map.MapDefinitionId + ", NetworkId=" + ownerNetworkId + ", kind=" + interaction.Kind +
                        ", placement=" + interaction.PlacementIndex + ". " + exception);
                }
            }
            else ResetF();
            if (settings.GTargetsEnabled != 0)
            {
                try { g = ResolveG(manager, map, settings, pickup); }
                catch (Exception exception)
                {
                    ResetG();
                    Debug.LogError("[CombatPrototype.Map] Highlight target failed; stage=ResolveG, World=" + worldName +
                        ", map=" + map.MapDefinitionId + ", NetworkId=" + ownerNetworkId + ", DropId=" + pickup.DropId +
                        ". " + exception);
                }
            }
            else ResetG();
        }

        private CombatPrototypeMapInteractionHighlightFrame ResolveF(EntityManager manager, CombatPrototypeMapData map,
            CombatPrototypeMapInteractionHighlightSettings settings, int owner, CombatPrototypeMapInteractionHudState state)
        {
            if (state.Mode == CombatPrototypeMapInteractionHudMode.Hidden) { ResetF(); return default; }
            if ((state.Mode != CombatPrototypeMapInteractionHudMode.Ready && state.Mode != CombatPrototypeMapInteractionHudMode.Working &&
                 state.Mode != CombatPrototypeMapInteractionHudMode.NoSpace) ||
                state.Kind < (byte)CombatPrototypeMapInteractionKind.Gather || state.Kind > (byte)CombatPrototypeMapInteractionKind.Mine ||
                (state.Mode == CombatPrototypeMapInteractionHudMode.NoSpace && (state.Kind != (byte)CombatPrototypeMapInteractionKind.Gather || state.ProgressPermille != 0)) ||
                state.PlacementIndex < 0)
                throw new InvalidOperationException("Invalid owner interaction target snapshot.");
            if (_fKind != state.Kind || _fPlacement != state.PlacementIndex)
            {
                ResetF();
                _fKind = state.Kind;
                _fPlacement = state.PlacementIndex;
            }
            if (_fEntity != Entity.Null && (!manager.Exists(_fEntity) || Placement(manager, _fEntity, _fKind) != _fPlacement))
                _fEntity = Entity.Null;
            if (_fEntity == Entity.Null)
            {
                var query = _fKind == (byte)CombatPrototypeMapInteractionKind.Gather ? _gather :
                    _fKind == (byte)CombatPrototypeMapInteractionKind.Tree ? _trees : _mines;
                using var entities = query.ToEntityArray(Allocator.Temp);
                foreach (var entity in entities)
                {
                    if (Placement(manager, entity, _fKind) != _fPlacement) continue;
                    if (_fEntity != Entity.Null) throw new InvalidOperationException("Duplicate resource placement identity.");
                    _fEntity = entity;
                }
            }
            // Owner and resource ghosts can arrive in different snapshots. Do not invent a position.
            if (_fEntity == Entity.Null) return default;
            int phase, collector;
            if (_fKind == (byte)CombatPrototypeMapInteractionKind.Gather)
            {
                var resource = manager.GetComponentData<CombatPrototypeMapGatherState>(_fEntity);
                phase = (int)resource.Phase; collector = resource.CollectorNetworkId;
            }
            else if (_fKind == (byte)CombatPrototypeMapInteractionKind.Tree)
            {
                var resource = manager.GetComponentData<CombatPrototypeMapTreeState>(_fEntity);
                phase = (int)resource.Phase; collector = resource.CollectorNetworkId;
            }
            else
            {
                var resource = manager.GetComponentData<CombatPrototypeMapMineState>(_fEntity);
                phase = (int)resource.Phase; collector = resource.CollectorNetworkId;
            }
            if (phase < 0 || phase > 2) throw new InvalidOperationException("Invalid resource phase snapshot.");
            var working = state.Mode == CombatPrototypeMapInteractionHudMode.Working;
            if (working ? phase != 1 || collector != owner : phase != 0) return default;
            var radius = _fKind == (byte)CombatPrototypeMapInteractionKind.Gather ? settings.GatherRadius :
                _fKind == (byte)CombatPrototypeMapInteractionKind.Tree ? settings.TreeRadius : settings.MineRadius;
            return Frame(manager, _fEntity, map, settings, _fKind, _fPlacement, radius,
                working ? settings.WorkingColor : settings.ReadyColor);
        }

        private CombatPrototypeMapInteractionHighlightFrame ResolveG(EntityManager manager, CombatPrototypeMapData map,
            CombatPrototypeMapInteractionHighlightSettings settings, CombatPrototypeMapPickupHudState state)
        {
            if (state.Mode == CombatPrototypeMapPickupHudMode.Hidden) { ResetG(); return default; }
            if ((state.Mode != CombatPrototypeMapPickupHudMode.Ready && state.Mode != CombatPrototypeMapPickupHudMode.NoSpace) || state.DropId <= 0)
                throw new InvalidOperationException("Invalid owner pickup target snapshot.");
            if (_gDropId != state.DropId) { ResetG(); _gDropId = state.DropId; }
            if (_gEntity != Entity.Null && (!manager.Exists(_gEntity) ||
                manager.GetComponentData<CombatPrototypeMapDropState>(_gEntity).DropId != _gDropId))
                _gEntity = Entity.Null;
            if (_gEntity == Entity.Null)
            {
                using var entities = _drops.ToEntityArray(Allocator.Temp);
                foreach (var entity in entities)
                {
                    if (manager.GetComponentData<CombatPrototypeMapDropState>(entity).DropId != _gDropId) continue;
                    if (_gEntity != Entity.Null) throw new InvalidOperationException("Duplicate drop identity.");
                    _gEntity = entity;
                }
            }
            if (_gEntity == Entity.Null) return default;
            var drop = manager.GetComponentData<CombatPrototypeMapDropState>(_gEntity);
            if (drop.Phase < CombatPrototypeMapDropPhase.Airborne || drop.Phase > CombatPrototypeMapDropPhase.Prepared)
                throw new InvalidOperationException("Invalid drop phase snapshot.");
            if (drop.Phase != CombatPrototypeMapDropPhase.Landed) return default;
            return Frame(manager, _gEntity, map, settings, 0, _gDropId, settings.DropRadius, settings.PickupColor);
        }

        private static int Placement(EntityManager manager, Entity entity, byte kind)
        {
            return kind == (byte)CombatPrototypeMapInteractionKind.Gather ?
                manager.GetComponentData<CombatPrototypeMapGatherState>(entity).PlacementIndex :
                kind == (byte)CombatPrototypeMapInteractionKind.Tree ?
                manager.GetComponentData<CombatPrototypeMapTreeState>(entity).PlacementIndex :
                manager.GetComponentData<CombatPrototypeMapMineState>(entity).PlacementIndex;
        }

        private static CombatPrototypeMapInteractionHighlightFrame Frame(EntityManager manager, Entity entity,
            CombatPrototypeMapData map, CombatPrototypeMapInteractionHighlightSettings settings,
            byte kind, int targetId, float radius, float3 color)
        {
            // Read the client's presented transform, not a PlayerView or a server work timer.
            var position = manager.GetComponentData<LocalToWorld>(entity).Position;
            var center = new float3(position.x, map.BaseHeight + settings.HeightOffset, position.z);
            if (!math.all(math.isfinite(position)) || !math.all(math.isfinite(center)))
                throw new InvalidOperationException("Highlight target position must be finite.");
            return new CombatPrototypeMapInteractionHighlightFrame
            {
                Visible = true, Kind = kind, TargetId = targetId, Center = center,
                Radius = radius, Color = new float4(color, settings.Opacity)
            };
        }

        internal void Reset() { ResetF(); ResetG(); }
        private void ResetF() { _fEntity = Entity.Null; _fKind = 0; _fPlacement = -1; }
        private void ResetG() { _gEntity = Entity.Null; _gDropId = 0; }
    }
}
