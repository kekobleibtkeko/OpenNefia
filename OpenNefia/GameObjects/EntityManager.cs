using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OpenNefia.Core.Maps;
using OpenNefia.Core.Maths;
using OpenNefia.Core.Prototypes;

namespace OpenNefia.Core.GameObjects
{
    public delegate void EntityQueryCallback(Entity entity);

    public delegate void EntityUidQueryCallback(EntityUid uid);

    /// <inheritdoc />
    public partial class EntityManager : IEntityManager
    {
        #region Dependencies

        [IoC.Dependency] private readonly IPrototypeManager PrototypeManager = default!;
        [IoC.Dependency] private readonly IEntitySystemManager EntitySystemManager = default!;
        [IoC.Dependency] private readonly IMapManager _mapManager = default!;

        #endregion Dependencies

        IComponentFactory IEntityManager.ComponentFactory => ComponentFactory;

        /// <inheritdoc />
        public IEntitySystemManager EntitySysManager => EntitySystemManager;

        private readonly Queue<EntityUid> QueuedDeletions = new();
        private readonly HashSet<EntityUid> QueuedDeletionsSet = new();

        /// <summary>
        ///     All entities currently stored in the manager.
        /// </summary>
        private readonly Dictionary<EntityUid, Entity> Entities = new();

        private EntityEventBus _eventBus = null!;

        private int NextEntityUid { get; set; } = (int)EntityUid.FirstUid;

        /// <inheritdoc />
        public IEventBus EventBus => _eventBus;

        public event EventHandler<EntityUid>? EntityAdded;
        public event EventHandler<EntityUid>? EntityInitialized;
        public event EventHandler<EntityUid>? EntityStarted;
        public event EventHandler<EntityUid>? EntityDeleted;

        public bool Started { get; private set; }
        public bool Initialized { get; private set; }

        /// <summary>
        /// Constructs a new instance of <see cref="EntityManager"/>.
        /// </summary>
        public EntityManager()
        {
        }

        public void Initialize()
        {
            if (Initialized)
                throw new InvalidOperationException("Initialize() called multiple times");

            _eventBus = new EntityEventBus(this);

            InitializeComponents();

            Initialized = true;
        }

        public void Startup()
        {
            if (Started)
                throw new InvalidOperationException("Startup() called multiple times");

            EntitySystemManager.Initialize();
            Started = true;
        }

        public void Shutdown()
        {
            FlushEntities();
            _eventBus.ClearEventTables();
            EntitySystemManager.Shutdown();
            ClearComponents();
            Initialized = false;
            Started = false;
        }

        public void Cleanup()
        {
            QueuedDeletions.Clear();
            QueuedDeletionsSet.Clear();
            Entities.Clear();
            _eventBus.Dispose();
            _eventBus = null!;
            ClearComponents();

            Initialized = false;
            Started = false;
        }

        #region Entity Management

        public Entity CreateEntityUninitialized(string? prototypeName, EntityUid? euid)
        {
            return CreateEntity(prototypeName, euid);
        }

        /// <inheritdoc />
        public Entity CreateEntityUninitialized(string? prototypeName)
        {
            return CreateEntity(prototypeName);
        }

        /// <inheritdoc />
        public Entity CreateEntityUninitialized(string? prototypeName, MapCoordinates coordinates)
        {
            var newEntity = CreateEntity(prototypeName);
            var map = coordinates.Map;
            if (map != null)
            {
                map.AddEntity(newEntity);
            }
            newEntity.Spatial.Pos = coordinates.Position;

            return newEntity;
        }

        /// <inheritdoc />
        public Entity SpawnEntity(string? protoName, MapCoordinates coordinates)
        {
            var entity = CreateEntityUninitialized(protoName, coordinates);
            InitializeAndStartEntity((Entity) entity, coordinates.Map?.Id ?? MapId.Nullspace);
            return entity;
        }

        /// <inheritdoc />
        public Entity SpawnEntity(string? protoName, IMap map, Vector2i position)
        {
            return SpawnEntity(protoName, new MapCoordinates(map, position));
        }

        /// <summary>
        /// Returns an entity by id
        /// </summary>
        /// <param name="uid"></param>
        /// <returns>Entity or throws if the entity doesn't exist</returns>
        public Entity GetEntity(EntityUid uid)
        {
            return Entities[uid];
        }

        /// <summary>
        /// Attempt to get an entity, returning whether or not an entity was gotten.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="entity">The requested entity or null if the entity couldn't be found.</param>
        /// <returns>True if a value was returned, false otherwise.</returns>
        public bool TryGetEntity(EntityUid uid, [NotNullWhen(true)] out Entity? entity)
        {
            if (Entities.TryGetValue(uid, out var cEntity) && !cEntity.Deleted)
            {
                entity = cEntity;
                return true;
            }

            // entity might get assigned if it's deleted but still found,
            // prevent somebody from being "smart".
            entity = null;
            return false;
        }

        /// <inheritdoc />
        public int EntityCount => Entities.Count;

        /// <inheritdoc />
        public IEnumerable<Entity> GetEntities() => Entities.Values;

        public IEnumerable<EntityUid> GetEntityUids() => Entities.Keys;

        /// <summary>
        /// Shuts-down and removes given Entity. This is also broadcast to all clients.
        /// </summary>
        /// <param name="e">Entity to remove</param>
        public void DeleteEntity(Entity e)
        {
            // Networking blindly spams entities at this function, they can already be
            // deleted from being a child of a previously deleted entity
            // TODO: Why does networking need to send deletes for child entities?
            if (e.Deleted)
                return;

            if (e.LifeStage >= EntityLifeStage.Terminating)
#if !EXCEPTION_TOLERANCE
                throw new InvalidOperationException("Called Delete on an entity already being deleted.");
#else
                return;
#endif

            RecursiveDeleteEntity(e);
        }

        private void RecursiveDeleteEntity(Entity entity)
        {
            if(entity.Deleted)
                return;

            var metadata = entity.MetaData;
            entity.LifeStage = EntityLifeStage.Terminating;

            EventBus.RaiseLocalEvent(entity.Uid, new EntityTerminatingEvent(), false);

            var map = entity.Spatial.Map;
            if (map != null)
            {
                map.RemoveEntity(entity);
            }

            // Dispose all my components, in a safe order so transform is available
            DisposeComponents(entity.Uid);

            metadata.EntityLifeStage = EntityLifeStage.Deleted;
            EntityDeleted?.Invoke(this, entity.Uid);
            EventBus.RaiseEvent(EventSource.Local, new EntityDeletedMessage(entity));
            Entities.Remove(entity.Uid);
        }

        public void QueueDeleteEntity(Entity entity)
        {
            QueueDeleteEntity(entity.Uid);
        }

        public void QueueDeleteEntity(EntityUid uid)
        {
            if(QueuedDeletionsSet.Add(uid))
                QueuedDeletions.Enqueue(uid);
        }

        public void DeleteEntity(EntityUid uid)
        {
            if (TryGetEntity(uid, out var entity))
            {
                DeleteEntity(entity);
            }
        }

        public bool EntityExists(EntityUid uid)
        {
            return TryGetEntity(uid, out _);
        }

        /// <summary>
        /// Disposes all entities and clears all lists.
        /// </summary>
        public void FlushEntities()
        {
            foreach (var e in GetEntities())
            {
                DeleteEntity(e);
            }
        }

        /// <summary>
        ///     Allocates an entity and stores it but does not load components or do initialization.
        /// </summary>
        private protected Entity AllocEntity(string? prototypeName, EntityUid? uid = null)
        {
            EntityPrototype? prototype = null;
            if (!string.IsNullOrWhiteSpace(prototypeName))
            {
                // If the prototype doesn't exist then we throw BEFORE we allocate the entity.
                prototype = PrototypeManager.Index<EntityPrototype>(new(prototypeName));
            }

            var entity = AllocEntity(uid);

            entity.Prototype = prototype;

            return entity;
        }

        /// <summary>
        ///     Allocates an entity and stores it but does not load components or do initialization.
        /// </summary>
        private protected Entity AllocEntity(EntityUid? uid = null)
        {
            if (uid == null)
            {
                uid = GenerateEntityUid();
            }

            if (EntityExists(uid.Value))
            {
                throw new InvalidOperationException($"UID already taken: {uid}");
            }

            var entity = new Entity(this, uid.Value);


            // we want this called before adding components
            EntityAdded?.Invoke(this, entity.Uid);

            // We do this after the event, so if the event throws we have not committed
            Entities[entity.Uid] = entity;

            // Create the MetaDataComponent and set it directly on the Entity to avoid a stack overflow in DEBUG.
            var metadata = new MetaDataComponent() { Owner = entity };
            entity.MetaData = metadata;

            // add the required MetaDataComponent directly.
            AddComponentInternal(uid.Value, metadata);

            // allocate the required SpatialComponent
            AddComponent<SpatialComponent>(entity);

            return entity;
        }

        /// <summary>
        ///     Allocates an entity and loads components but does not do initialization.
        /// </summary>
        private protected Entity CreateEntity(string? prototypeName, EntityUid? uid = null)
        {
            if (prototypeName == null)
                return AllocEntity(uid);

            var entity = AllocEntity(prototypeName, uid);
            try
            {
                EntityPrototype.LoadEntity(entity.Prototype, entity, ComponentFactory, null);
                return entity;
            }
            catch (Exception e)
            {
                // Exception during entity loading.
                // Need to delete the entity to avoid corrupt state causing crashes later.
                DeleteEntity(entity);
                throw new EntityCreationException($"Exception inside CreateEntity with prototype {prototypeName}", e);
            }
        }

        private protected void LoadEntity(Entity entity, IEntityLoadContext? context)
        {
            EntityPrototype.LoadEntity(entity.Prototype, entity, ComponentFactory, context);
        }

        private void InitializeAndStartEntity(Entity entity, MapId mapId)
        {
            try
            {
                InitializeEntity(entity);
                StartEntity(entity);

                // If the map we're initializing the entity on is initialized, run map init on it.
                if (_mapManager.IsMapInitialized(mapId))
                    entity.RunMapInit();
            }
            catch (Exception e)
            {
                DeleteEntity(entity);
                throw new EntityCreationException("Exception inside InitializeAndStartEntity", e);
            }
        }

        private protected void InitializeEntity(Entity entity)
        {
            InitializeComponents(entity.Uid);
            EntityInitialized?.Invoke(this, entity.Uid);
        }

        private protected void StartEntity(Entity entity)
        {
            StartComponents(entity.Uid);
            EntityStarted?.Invoke(this, entity.Uid);
        }

#endregion Entity Management

        /// <summary>
        ///     Factory for generating a new EntityUid for an entity currently being created.
        /// </summary>
        /// <inheritdoc />
        private EntityUid GenerateEntityUid()
        {
            return new(NextEntityUid++);
        }
    }

    public enum EntityMessageType : byte
    {
        Error = 0,
        ComponentMessage,
        SystemMessage
    }
}
