﻿using OpenNefia.Content.DisplayName;
using OpenNefia.Content.GameObjects.Pickable;
using OpenNefia.Content.Logic;
using OpenNefia.Content.Maps;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Locale;
using OpenNefia.Core.Maps;
using OpenNefia.Core.Maths;
using System.Text;

namespace OpenNefia.Content.GameObjects
{
    public class PlayerMovementSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerQuery _playerQuery = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEntityLookup _lookup = default!;
        [Dependency] private readonly DisplayNameSystem _displayNames = default!;
        [Dependency] private readonly TargetTextSystem _targetText = default!;
        [Dependency] private readonly IMessagesManager _mes = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<MoveableComponent, BeforeMoveEventArgs>(HandleBeforeMove, nameof(HandleBeforeMove));
            SubscribeLocalEvent<PlayerComponent, BeforeMoveEventArgs>(HandleBeforeMovePlayer, nameof(HandleBeforeMovePlayer));
            SubscribeLocalEvent<PlayerComponent, AfterMoveEventArgs>(HandleAfterMovePlayer, nameof(HandleAfterMovePlayer));
        }

        private void HandleBeforeMovePlayer(EntityUid uid, PlayerComponent player, BeforeMoveEventArgs args)
        {
            CheckMovementOutOfMap(uid, args, player);
        }

        public void CheckMovementOutOfMap(EntityUid uid, BeforeMoveEventArgs args,
           PlayerComponent? player = null, 
           SpatialComponent? playerSpatial = null)
        {
            if (args.Handled || !Resolve(uid, ref player, ref playerSpatial))
                return;

            var (pos, mapId) = args.NewPosition;

            if (_mapManager.TryGetMap(mapId, out var map) && !map.IsInBounds(pos))
            {
                if (EntityManager.TryGetComponent<MapEdgesEntranceComponent>(map.MapEntityUid, out var mapEdgesEntrance))
                {
                    if (_playerQuery.YesOrNo(Loc.GetString("Elona.PlayerMovement.PromptLeaveMap", ("map", map.MapEntityUid))))
                    {
                        playerSpatial.Direction = playerSpatial.Direction.GetOpposite();
                        Raise(uid, new ExitMapEventArgs(map, mapEdgesEntrance.Entrance), args);
                    }
                }
            }
        }

        private void HandleBeforeMove(EntityUid uid, MoveableComponent moveable, BeforeMoveEventArgs args)
        {
            CollideWithEntities(uid, args, moveable);
        }

        public void CollideWithEntities(EntityUid source, BeforeMoveEventArgs args,
            MoveableComponent? moveable = null)
        {
            if (args.Handled || !Resolve(source, ref moveable))
                return;

            var entities = _lookup.GetLiveEntitiesAtCoords(args.NewPosition)
                .Where(spatial => spatial.IsSolid);

            foreach (var collidedSpatial in entities.ToList())
            {
                var ev = new CollideWithEventArgs(collidedSpatial.Owner);
                if (!Raise(source, ev, args))
                    return;

                var ev2 = new WasCollidedWithEventArgs(source);
                if (!Raise(collidedSpatial.Owner, ev2, args))
                    return;
            }
        }

        private void HandleAfterMovePlayer(EntityUid uid, PlayerComponent player, AfterMoveEventArgs args)
        {
            // TODO blindness
            var blind = false;

            if (!blind)
            {
                var text = _targetText.GetItemOnCellText(uid, args.NewPosition);
                if (text != null)
                {
                    _mes.Display(text);
                }
            }
            else
            {
                var items = _lookup.EntitiesUnderneath(uid)
                    .Where(spatial => EntityManager.HasComponent<PickableComponent>(spatial.Owner));

                if (items.Any())
                {
                    _mes.Display(Loc.GetString("Elona.PlayerMovement.SenseSomething"));
                }
            }
        }
    }

    public class ExitMapEventArgs : TurnResultEntityEventArgs
    {
        public readonly IMap Map;
        public readonly MapEntrance Entrance;

        public ExitMapEventArgs(IMap map, MapEntrance entrance)
        {
            Map = map;
            Entrance = entrance;
        }
    }

    public class CollideWithEventArgs : TurnResultEntityEventArgs
    {
        public readonly EntityUid Target;

        public CollideWithEventArgs(EntityUid target)
        {
            Target = target;
        }
    }

    public class WasCollidedWithEventArgs : TurnResultEntityEventArgs
    {
        public readonly EntityUid Source;

        public WasCollidedWithEventArgs(EntityUid source)
        {
            Source = source;
        }
    }
}
