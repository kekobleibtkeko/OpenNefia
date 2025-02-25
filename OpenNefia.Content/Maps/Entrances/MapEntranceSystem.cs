using OpenNefia.Core.Areas;
using OpenNefia.Core.Game;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Log;
using OpenNefia.Core.Maps;
using OpenNefia.Core.Prototypes;
using OpenNefia.Core.SaveGames;
using OpenNefia.Core.Utility;
using System.Diagnostics.CodeAnalysis;

namespace OpenNefia.Content.Maps
{
    public sealed class MapEntranceSystem : EntitySystem
    {
        [Dependency] private readonly IMapLoader _mapLoader = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IAreaManager _areaManager = default!;
        [Dependency] private readonly ISaveGameManager _saveGameManager = default!;

        public bool TryGetAreaOfEntrance(MapEntrance entrance, [NotNullWhen(true)] out IArea? area)
        {
            var entranceAreaId = entrance.MapIdSpecifier.GetAreaId();
            if (entranceAreaId == null)
            {
                area = null;
                return false;
            }

            return _areaManager.TryGetArea(entranceAreaId.Value, out area);
        }

        public bool UseMapEntrance(EntityUid user, MapEntrance entrance,
            SpatialComponent? spatial = null)
            => UseMapEntrance(user, entrance, out _, spatial);

        public bool UseMapEntrance(EntityUid user, MapEntrance entrance, 
            [NotNullWhen(true)] out MapId? mapId,
            SpatialComponent? spatial = null)
        {
            mapId = null;

            if (!Resolve(user, ref spatial))
                return false;

            mapId = entrance.MapIdSpecifier.GetMapId();
            if (mapId == null)
            {
                Logger.WarningS("map.entrance", $"Failed to get map ID for entrance {entrance}!");
                return false;
            }

            if (!TryMapLoad(mapId.Value, out var map))
                return false;

            var newPos = entrance.StartLocation.GetStartPosition(user, map)
                .BoundWithin(map.Bounds);

            spatial.Coordinates = new EntityCoordinates(map.MapEntityUid, newPos);

            return true;
        }

        /// <summary>
        /// Loads the map from memory or disk. This will ensure that there is a map entity for the
        /// travelling entity to be parented to.
        /// </summary>
        private bool TryMapLoad(MapId mapToLoad, [NotNullWhen(true)] out IMap? map)
        {
            // See if this map is still in memory and hasn't been flushed yet.
            if (_mapManager.TryGetMap(mapToLoad, out map))
            {
                Logger.WarningS("map.entrance", $"Travelling to cached map {map.Id}");
                return true;
            }

            // Let's try to load the map from disk, using the current save.
            var save = _saveGameManager.CurrentSave;
            if (save == null)
            {
                Logger.ErrorS("map.entrance", "No active save game!");
                return false;
            }

            if (!_mapLoader.TryLoadMap(mapToLoad, save, out map))
            {
                Logger.ErrorS("map.entrance", $"Failed to load map {mapToLoad} from disk!");
                return false;
            }

            Logger.InfoS("map.entrance", $"Loaded map {mapToLoad} from disk.");
            return true;
        }

        /// <summary>
        /// Sets the map to travel to when exiting the destination map via the edges.
        /// </summary>
        /// <param name="mapTravellingTo">Map that is being travelled to.</param>
        /// <param name="prevCoords">Location that exiting the given map from the edges should lead to.</param>
        public void SetPreviousMap(MapId mapTravellingTo, MapCoordinates prevCoords)
        {
            var mapEntityUid = _mapManager.GetMap(mapTravellingTo).MapEntityUid;
            var mapMapEntrance = EntityManager.EnsureComponent<MapEdgesEntranceComponent>(mapEntityUid);
            mapMapEntrance.Entrance.MapIdSpecifier = new BasicMapIdSpecifier(prevCoords.MapId);
            mapMapEntrance.Entrance.StartLocation = new SpecificMapLocation(prevCoords.Position);
        }
    }
}