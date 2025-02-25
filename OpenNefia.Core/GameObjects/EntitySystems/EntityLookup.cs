﻿using OpenNefia.Core.IoC;
using OpenNefia.Core.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Dependency = OpenNefia.Core.IoC.DependencyAttribute;

namespace OpenNefia.Core.GameObjects
{
    /// <summary>
    /// Defines an entity system for querying a list of entities in a map.
    /// </summary>
    public interface IEntityLookup : IEntitySystem
    {
        /// <summary>
        /// Gets all entities in this map, including children nested in containers.
        /// </summary>
        /// <param name="includeMapEntity">If true, include the map entity as the last in the enumerable.</param>
        IEnumerable<SpatialComponent> GetAllEntitiesIn(MapId mapId, bool includeMapEntity = false);

        /// <summary>
        /// Returns all entities that are direct children of the map,
        /// excluding entities in containers.
        /// </summary>
        /// <param name="mapId">The map to query in.</param>
        /// <param name="includeMapEntity">If true, include the map entity as the last in the enumerable.</param>
        IEnumerable<SpatialComponent> GetEntitiesDirectlyIn(MapId mapId, bool includeMapEntity = false);

        /// <summary>
        /// Returns all entities that are direct children of the entity.
        /// </summary>
        /// <param name="entity">Entity to query in.</param>
        /// <param name="includeParent">If true, include the passed entity as the last in the enumerable.</param>
        IEnumerable<SpatialComponent> GetEntitiesDirectlyIn(EntityUid entity, bool includeParent = false);

        /// <summary>
        /// Gets the live entities at the position in the map, excluding
        /// entities in containers.
        /// </summary>
        /// <param name="coords">The coordinates to query at.</param>
        /// <param name="includeMapEntity">If true, include the map entity as the last in the enumerable.</param>
        IEnumerable<SpatialComponent> GetLiveEntitiesAtCoords(MapCoordinates coords, bool includeMapEntity = false);

        /// <summary>
        /// Gets the live entities at the position. This is used in case
        /// the entity is in a container or similar.
        /// </summary>
        /// <param name="coords">The coordinates to query at.</param>
        /// <param name="includeParent">If true, include the parent entity as the last in the enumerable.</param>
        IEnumerable<SpatialComponent> GetLiveEntitiesAtCoords(EntityCoordinates coords, bool includeParent = false);

        /// <summary>
        /// Gets the primary character on this tile.
        /// 
        /// In Elona, traditionally only one character is allowed on each tile. However, extra features
        /// such as the Riding mechanic or the Tag Teams mechanic added in Elona+ allow multiple characters to
        /// occupy the same tile.
        /// 
        /// This function retrieves the "primary" character used for things like
        /// damage calculation, spell effects, and so on, which should exclude the riding mount, tag team
        /// partner, etc.
        /// 
        /// It's necessary to keep track of the non-primary characters on the same tile because they are 
        /// still affected by things like area of effect magic.
        /// </summary>
        SpatialComponent? GetBlockingEntity(MapCoordinates coords);

        IEnumerable<SpatialComponent> EntitiesUnderneath(EntityUid player, bool includeMapEntity = false, SpatialComponent? spatial = null);

        /// <summary>
        ///     Returns ALL component instances of a specified type in the given map.
        /// </summary>
        /// <typeparam name="TComp">A trait or type of a component to retrieve.</typeparam>
        /// <param name="mapId">Map to query in.</param>
        /// <param name="includeChildren">If true, also query the children of entities in the map.</param>
        /// <returns>All components in the map that have the specified type.</returns>
        IEnumerable<TComp> EntityQueryInMap<TComp>(MapId mapId, bool includeChildren = false)
            where TComp : IComponent;

        /// <summary>
        /// Returns the relevant components from all entities in the map that contain the two required components.
        /// </summary>
        /// <typeparam name="TComp1">First required component.</typeparam>
        /// <typeparam name="TComp2">Second required component.</typeparam>
        /// <param name="mapId">Map to query in.</param>
        /// <param name="includeChildren">If true, also query the children of entities in the map.</param>
        /// <returns>The pairs of components from each entity directly in the map that has the two required components.</returns>
        IEnumerable<(TComp1, TComp2)> EntityQueryInMap<TComp1, TComp2>(MapId mapId, bool includeChildren = false)
            where TComp1 : IComponent
            where TComp2 : IComponent;

        /// <summary>
        /// Returns the relevant components from all entities in the map that contain the three required components.
        /// </summary>
        /// <typeparam name="TComp1">First required component.</typeparam>
        /// <typeparam name="TComp2">Second required component.</typeparam>
        /// <typeparam name="TComp3">Third required component.</typeparam>
        /// <param name="mapId">Map to query in.</param>
        /// <param name="includeChildren">If true, also query the children of entities in the map.</param>
        /// <returns>The pairs of components from each entity in the map that has the three required components.</returns>
        IEnumerable<(TComp1, TComp2, TComp3)> EntityQueryInMap<TComp1, TComp2, TComp3>(MapId mapId, bool includeChildren = false)
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent;

        /// <summary>
        /// Returns the relevant components from all entities in the map that contain the four required components.
        /// </summary>
        /// <typeparam name="TComp1">First required component.</typeparam>
        /// <typeparam name="TComp2">Second required component.</typeparam>
        /// <typeparam name="TComp3">Third required component.</typeparam>
        /// <typeparam name="TComp4">Fourth required component.</typeparam>
        /// <param name="mapId">Map to query in.</param>
        /// <param name="includeChildren">If true, also query the children of entities in the map.</param>
        /// <returns>The pairs of components from each entity in the map that has the four required components.</returns>
        IEnumerable<(TComp1, TComp2, TComp3, TComp4)> EntityQueryInMap<TComp1, TComp2, TComp3, TComp4>(MapId mapId, bool includeChildren = false)
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
            where TComp4 : IComponent;
    }

    public class EntityLookup : EntitySystem, IEntityLookup
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<MapComponent, MapCreatedEvent>(HandleMapCreated, nameof(HandleMapCreated));
        }

        private void HandleMapCreated(EntityUid uid, MapComponent component, MapCreatedEvent args)
        {
            var lookup = EntityManager.EnsureComponent<MapEntityLookupComponent>(uid);
            var map = _mapManager.GetMap(component.MapId);
            lookup.InitializeFromMap(map);
        }

        /// <inheritdoc />
        public IEnumerable<SpatialComponent> GetAllEntitiesIn(MapId mapId, bool includeMapEntity = false)
        {
            if (!_mapManager.TryGetMap(mapId, out var map))
                yield break;
            if (!EntityManager.TryGetComponent(map.MapEntityUid, out SpatialComponent mapEntitySpatial))
                yield break;
            
            foreach (var spatial in EntityManager.GetAllComponents<SpatialComponent>())
            {
                if (spatial.MapID == mapId && spatial.Owner != mapEntitySpatial.Owner)
                    yield return spatial;
            }

            if (includeMapEntity)
            {
                yield return mapEntitySpatial;
            }
        }

        /// <inheritdoc />
        public IEnumerable<SpatialComponent> GetEntitiesDirectlyIn(EntityUid entity, bool includeParent = false)
        {
            if (!EntityManager.TryGetComponent(entity, out SpatialComponent spatial))
                return Enumerable.Empty<SpatialComponent>();

            var spatials = spatial.Children;

            if (includeParent)
            {
                spatials = spatials.Append(spatial);
            }

            return spatials;
        }

        /// <inheritdoc />
        public IEnumerable<SpatialComponent> GetEntitiesDirectlyIn(MapId mapId, bool includeParent = false)
        {
            if (!_mapManager.TryGetMap(mapId, out var map))
                return Enumerable.Empty<SpatialComponent>();

            return GetEntitiesDirectlyIn(map.MapEntityUid, includeParent);
        }

        /// <inheritdoc />
        public IEnumerable<SpatialComponent> GetLiveEntitiesAtCoords(MapCoordinates coords, bool includeMapEntity = false)
        {
            if (!_mapManager.TryGetMap(coords.MapId, out var map))
                return Enumerable.Empty<SpatialComponent>();

            if (!map.IsInBounds(coords))
                return Enumerable.Empty<SpatialComponent>();

            var mapLookupComp = EntityManager.GetComponent<MapEntityLookupComponent>(map.MapEntityUid);

            var ents = mapLookupComp.EntitySpatial[coords.X, coords.Y]
                .Where(uid => EntityManager.IsAlive(uid));
        
            if (includeMapEntity)
            {
                ents = ents.Append(map.MapEntityUid);
            }

            return ents.Select(uid => EntityManager.GetComponent<SpatialComponent>(uid));
        }

        /// <inheritdoc />
        public IEnumerable<SpatialComponent> GetLiveEntitiesAtCoords(EntityCoordinates coords, bool includeParent = false)
        {
            return GetLiveEntitiesAtCoords(coords.ToMap(EntityManager), includeParent);
        }

        /// <inheritdoc />
        public SpatialComponent? GetBlockingEntity(MapCoordinates coords)
        {
            foreach (var spatial in GetLiveEntitiesAtCoords(coords))
            {
                if (spatial.IsSolid)
                    return spatial;
            }

            return null;
        }
        
        /// <inheritdoc/>
        public IEnumerable<SpatialComponent> EntitiesUnderneath(EntityUid player, bool includeMapEntity = false, SpatialComponent? spatial = null)
        {
            if (!Resolve(player, ref spatial))
                return Enumerable.Empty<SpatialComponent>();

            return GetLiveEntitiesAtCoords(spatial.MapPosition, includeMapEntity: includeMapEntity)
                .Where(s => s.Owner != player);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool EntityIsInMap(MapId mapId, SpatialComponent spatial, bool includeChildren)
        {
            return spatial.MapID == mapId
                && spatial.Parent != null // is this a non-map entity?
                && (includeChildren || spatial.Parent?.Parent == null);
        }

        /// <inheritdoc />
        public IEnumerable<TComp> EntityQueryInMap<TComp>(MapId mapId, bool includeChildren = false)
            where TComp : IComponent
        {
            foreach (var ent in EntityManager.EntityQuery<TComp>())
            {
                if (EntityManager.TryGetComponent(ent.Owner, out SpatialComponent? spatial) 
                    && EntityIsInMap(mapId, spatial, includeChildren))
                {
                    yield return ent;
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<(TComp1, TComp2)> EntityQueryInMap<TComp1, TComp2>(MapId mapId, bool includeChildren = false)
            where TComp1 : IComponent
            where TComp2 : IComponent
        {
            foreach (var ent in EntityManager.EntityQuery<TComp1, TComp2>())
            {
                if (EntityManager.TryGetComponent(ent.Item1.Owner, out SpatialComponent? spatial)
                    && EntityIsInMap(mapId, spatial, includeChildren))
                {
                    yield return ent;
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<(TComp1, TComp2, TComp3)> EntityQueryInMap<TComp1, TComp2, TComp3>(MapId mapId, bool includeChildren = false)
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
        {
            foreach (var ent in EntityManager.EntityQuery<TComp1, TComp2, TComp3>())
            {
                if (EntityManager.TryGetComponent(ent.Item1.Owner, out SpatialComponent? spatial)
                    && EntityIsInMap(mapId, spatial, includeChildren))
                {
                    yield return ent;
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<(TComp1, TComp2, TComp3, TComp4)> EntityQueryInMap<TComp1, TComp2, TComp3, TComp4>(MapId mapId, bool includeChildren = false)
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
            where TComp4 : IComponent
        {
            foreach (var ent in EntityManager.EntityQuery<TComp1, TComp2, TComp3, TComp4>())
            {
                if (EntityManager.TryGetComponent(ent.Item1.Owner, out SpatialComponent? spatial)
                    && EntityIsInMap(mapId, spatial, includeChildren))
                {
                    yield return ent;
                }
            }
        }
    }
}
