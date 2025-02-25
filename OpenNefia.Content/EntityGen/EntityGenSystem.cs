using OpenNefia.Content.Charas;
using OpenNefia.Content.DisplayName;
using OpenNefia.Content.Maps;
using OpenNefia.Core.Containers;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Log;
using OpenNefia.Core.Maps;
using OpenNefia.Core.Maths;
using OpenNefia.Core.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNefia.Content.EntityGen
{
    /// <summary>
    /// Wraps <see cref="IEntityManager"/>'s spawning functionality with additional event
    /// hooks for initializing entities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is necessary since <see cref="IEntityManager.SpawnEntity"/> is general-purpose
    /// and gets used for instantiating entities loaded from a save game. On the contrary,
    /// some events should only be run the very first time the entity is spawned, and not when
    /// they're loaded from a save.
    /// </para>
    /// </remarks>
    public interface IEntityGen : IEntitySystem
    {
        void FireGeneratedEvent(EntityUid entity);
        EntityUid? SpawnEntity(PrototypeId<EntityPrototype>? protoId, EntityCoordinates coordinates);
        EntityUid? SpawnEntity(PrototypeId<EntityPrototype>? protoId, MapCoordinates coordinates);
        EntityUid? SpawnEntity(PrototypeId<EntityPrototype>? protoId, IContainer container, int count = 1);
        EntityUid? SpawnEntity(PrototypeId<EntityPrototype>? protoId, IMap map);
    }

    public class EntityGenSystem : EntitySystem, IEntityGen
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IMapLoader _mapLoader = default!;
        [Dependency] private readonly IPrototypeManager _protos = default!;
        [Dependency] private readonly IStackSystem _stacks = default!;
        [Dependency] private readonly IMapPlacement _placement = default!;

        public override void Initialize()
        {
            _mapLoader.OnBlueprintEntityStartup += HandleBlueprintEntityStartup;

            SubscribeLocalEvent<SpatialComponent, EntityCloneFinishedEventArgs>(HandleClone, nameof(HandleClone));
        }

        public void FireGeneratedEvent(EntityUid entity)
        {
            // TODO: Check if generated has already been fired for this entity.
            var ev = new EntityGeneratedEvent();
            RaiseLocalEvent(entity, ref ev);
        }

        /// <summary>
        /// Runs entity generation events for entities loaded from blueprints.
        /// </summary>
        private void HandleBlueprintEntityStartup(EntityUid entity)
        {
            FireGeneratedEvent(entity);
        }

        /// <summary>
        /// Runs entity generation events for all cloned entities.
        /// </summary>
        private void HandleClone(EntityUid entity, SpatialComponent component, EntityCloneFinishedEventArgs args)
        {
            FireGeneratedEvent(entity);
        }

        public EntityUid? SpawnEntity(PrototypeId<EntityPrototype>? protoId, EntityCoordinates coordinates)
        {
            if (!coordinates.IsValid(EntityManager))
                return null;

            return SpawnEntity(protoId, coordinates.ToMap(EntityManager));
        }

        private enum PositionSearchType
        {
            General,
            Chara
        }

        private PositionSearchType GetSearchType(PrototypeId<EntityPrototype>? protoId)
        {
            if (protoId == null)
                return PositionSearchType.General;

            var proto = _protos.Index(protoId.Value);
            if (proto.Components.HasComponent<CharaComponent>())
                return PositionSearchType.Chara;

            return PositionSearchType.General;
        }

        public EntityUid? SpawnEntity(PrototypeId<EntityPrototype>? protoId, MapCoordinates coordinates)
        {
            var ent = EntityManager.SpawnEntity(protoId, new MapCoordinates(MapId.Global, Vector2i.Zero));

            var searchType = GetSearchType(protoId);
            var spatial = EntityManager.GetComponent<SpatialComponent>(ent);

            switch (searchType)
            {
                case PositionSearchType.Chara:
                    _placement.TryPlaceChara(ent, coordinates);
                    break;
                case PositionSearchType.General:
                default:
                    var map = _mapManager.GetMap(coordinates.MapId);
                    spatial.Coordinates = map.AtPosEntity(coordinates.Position);
                    break;
            }

            if (spatial.MapID == MapId.Global)
            {
                EntityManager.DeleteEntity(ent);

                Logger.ErrorS("entity.gen", $"Entity {ent} was not moved from global map to real position.");
                return null;
            }

            FireGeneratedEvent(ent);

            if (!EntityManager.IsAlive(ent))
            {
                EntityManager.DeleteEntity(ent);

                Logger.WarningS("entity.gen", $"Entity {ent} became invalid after {nameof(EntityGeneratedEvent)} was fired.");
                return null;
            }

            return ent;
        }

        public EntityUid? SpawnEntity(PrototypeId<EntityPrototype>? protoId, IMap map)
        {
            var pos = _placement.FindFreePosition(map);
            if (pos == null)
                return null;

            return SpawnEntity(protoId, pos.Value);
        }

        public EntityUid? SpawnEntity(PrototypeId<EntityPrototype>? protoId, IContainer container, int count = 1)
        {
            var coords = new EntityCoordinates(container.Owner, Vector2i.Zero);
            var ent = SpawnEntity(protoId, coords);

            if (!EntityManager.IsAlive(ent))
                return null;
        
            if (!container.Insert(ent.Value))
            {
                Logger.WarningS("entity.gen", $"Could not fit entity '{ent}' into container of entity '{container.Owner}'.");
                
                EntityManager.DeleteEntity(ent.Value);
                return null;
            }

            _stacks.SetCount(ent.Value, count);

            return ent.Value;
        }
    }

    public struct EntityGeneratedEvent
    {
    }
}
