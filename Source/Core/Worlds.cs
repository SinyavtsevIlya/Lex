using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace Nanory.Lex
{
    public class EcsWorld
    {
        internal EntityData[] Entities;
        int _entitiesCount;
        int[] _recycledEntities;
        int _recycledEntitiesCount;
        public IEcsPool[] Pools;
        public IEcsPool[] PoolsSparse;
        protected int _poolsCount;
        readonly Dictionary<int, EcsFilter> _filters;
        List<EcsFilter>[] _filtersByIncludedComponents;
        List<EcsFilter>[] _filtersByExcludedComponents;
        bool _destroyed;
#if DEBUG || LEX_WORLD_EVENTS
        List<IEcsWorldEventListener> _eventListeners;

        public void AddEventListener(IEcsWorldEventListener listener)
        {
#if DEBUG
            if (listener == null) { throw new Exception("Listener is null."); }
#endif
            _eventListeners.Add(listener);
        }

        public void RemoveEventListener(IEcsWorldEventListener listener)
        {
#if DEBUG
            if (listener == null) { throw new Exception("Listener is null."); }
#endif
            _eventListeners.Remove(listener);
        }

        public void RaiseEntityChangeEvent(int entity)
        {
            for (int ii = 0, iMax = _eventListeners.Count; ii < iMax; ii++)
            {
                _eventListeners[ii].OnEntityChanged(entity);
            }
        }
#endif
#if DEBUG
        readonly List<int> _leakedEntities = new List<int>(512);

        internal bool CheckForLeakedEntities()
        {
            if (_leakedEntities.Count > 0)
            {
                for (int i = 0, iMax = _leakedEntities.Count; i < iMax; i++)
                {
                    ref var entityData = ref Entities[_leakedEntities[i]];
                    if (entityData.Gen > 0 && entityData.ComponentsCount == 0)
                    {
                        return true;
                    }
                }
                _leakedEntities.Clear();
            }
            return false;
        }
#endif

        public EcsWorld(in Config cfg = default)
        {
            // entities.
            var capacity = cfg.Entities > 0 ? cfg.Entities : Config.EntitiesDefault;
            Entities = new EntityData[capacity];
            capacity = cfg.RecycledEntities > 0 ? cfg.RecycledEntities : Config.RecycledEntitiesDefault;
            _recycledEntities = new int[capacity];
            _entitiesCount = 0;
            _recycledEntitiesCount = 0;
            // pools.
            capacity = cfg.Pools > 0 ? cfg.Pools : Config.PoolsDefault;
            Pools = new IEcsPool[capacity];
            PoolsSparse = new IEcsPool[capacity];
            _filtersByIncludedComponents = new List<EcsFilter>[capacity];
            _filtersByExcludedComponents = new List<EcsFilter>[capacity];
            _poolsCount = 0;
            // filters.
            capacity = cfg.Filters > 0 ? cfg.Filters : Config.FiltersDefault;
            _filters = new Dictionary<int, EcsFilter>(capacity);
#if DEBUG || LEX_WORLD_EVENTS
            _eventListeners = new List<IEcsWorldEventListener>(4);
#endif
            _destroyed = false;
        }

        public void Destroy()
        {
#if DEBUG
            if (CheckForLeakedEntities()) { throw new Exception($"Empty entity detected before EcsWorld.Destroy()."); }
#endif
            _destroyed = true;
            for (var i = _entitiesCount - 1; i >= 0; i--)
            {
                ref var entityData = ref Entities[i];
                if (entityData.ComponentsCount > 0)
                {
                    DelEntity(i);
                }
            }
            for (var i = _poolsCount - 1; i >= 0; i--)
            {
                Pools[i].Destroy();
            }
            Pools = Array.Empty<IEcsPool>();
            PoolsSparse = Array.Empty<IEcsPool>();
            _filters.Clear();
            _filtersByIncludedComponents = Array.Empty<List<EcsFilter>>();
            _filtersByExcludedComponents = Array.Empty<List<EcsFilter>>();
#if DEBUG || LEX_WORLD_EVENTS
            for (var ii = _eventListeners.Count - 1; ii >= 0; ii--)
            {
                _eventListeners[ii].OnWorldDestroyed(this);
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive()
        {
            return !_destroyed;
        }

        public int NewEntity()
        {
            int entity;
            if (_recycledEntitiesCount > 0)
            {
                entity = _recycledEntities[--_recycledEntitiesCount];
                ref var entityData = ref Entities[entity];
                entityData.Gen = (short)-entityData.Gen;
            }
            else
            {
                // new entity.
                if (_entitiesCount == Entities.Length)
                {
                    // resize entities and component pools.
                    var newSize = _entitiesCount << 1;
                    Array.Resize(ref Entities, newSize);
                    for (int i = 0, iMax = _poolsCount; i < iMax; i++)
                    {
                        Pools[i].Resize(newSize);
                    }
#if DEBUG || LEX_WORLD_EVENTS
                    for (int ii = 0, iMax = _eventListeners.Count; ii < iMax; ii++)
                    {
                        _eventListeners[ii].OnWorldResized(newSize);
                    }
#endif
                }
                entity = _entitiesCount++;
                Entities[entity].Gen = 1;
                for (int i = 0, iMax = _poolsCount; i < iMax; i++)
                {
                    Pools[i].InitAutoReset(entity);
                }
            }
#if DEBUG
            _leakedEntities.Add(entity);
#endif
#if DEBUG || LEX_WORLD_EVENTS
            for (int ii = 0, iMax = _eventListeners.Count; ii < iMax; ii++)
            {
                _eventListeners[ii].OnEntityCreated(entity);
            }
#endif
            return entity;
        }

        public void DelEntity(int entity)
        {
#if DEBUG
            if (entity < 0 || entity >= _entitiesCount)
            {
                throw new Exception("Cant touch destroyed entity.");
            }
#endif
            ref var entityData = ref Entities[entity];
            if (entityData.Gen < 0)
            {
                return;
            }
            // kill components.
            if (entityData.ComponentsCount > 0)
            {
                var idx = 0;
                while (entityData.ComponentsCount > 0 && idx < _poolsCount)
                {
                    for (; idx < _poolsCount; idx++)
                    {
                        if (Pools[idx].Has(entity))
                        {
                            Pools[idx++].Del(entity);
                            break;
                        }
                    }
                }
#if DEBUG
                if (entityData.ComponentsCount != 0) { throw new Exception($"Invalid components count on entity {entity} => {entityData.ComponentsCount}."); }
#endif
                return;
            }
            entityData.Gen = (short)(entityData.Gen == short.MaxValue ? -1 : -(entityData.Gen + 1));
            if (_recycledEntitiesCount == _recycledEntities.Length)
            {
                Array.Resize(ref _recycledEntities, _recycledEntitiesCount << 1);
            }
            _recycledEntities[_recycledEntitiesCount++] = entity;
#if DEBUG || LEX_WORLD_EVENTS
            for (int ii = 0, iMax = _eventListeners.Count; ii < iMax; ii++)
            {
                _eventListeners[ii].OnEntityDestroyed(entity);
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetComponentsCount(int entity)
        {
            return Entities[entity].ComponentsCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short GetEntityGen(int entity)
        {
            return Entities[entity].Gen;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetAllocatedEntitiesCount()
        {
            return _entitiesCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetWorldSize()
        {
            return Entities.Length;
        }

        public EcsPool<T> GetPool<T>() where T : struct
        {
            var rawPool = PoolsSparse[EcsComponent<T>.TypeIndex];
            if (rawPool != null)
            {
                return (EcsPool<T>)rawPool;
            }
            var pool = CreatePool<T>();

            if (_poolsCount == Pools.Length)
            {
                var newSize = _poolsCount << 1;
                Array.Resize(ref Pools, newSize);
                Array.Resize(ref _filtersByIncludedComponents, newSize);
                Array.Resize(ref _filtersByExcludedComponents, newSize);
            }
            Pools[_poolsCount++] = pool;

            if (EcsComponent<T>.TypeIndex >= PoolsSparse.Length)
            {
                var newSize = EcsComponent<T>.TypeIndex << 1;
                Array.Resize(ref PoolsSparse, newSize);
            }

            PoolsSparse[EcsComponent<T>.TypeIndex] = pool;
            return pool;
        }

        public int GetAllEntities(ref int[] entities)
        {
            var count = _entitiesCount - _recycledEntitiesCount;
            if (entities == null || entities.Length < count)
            {
                entities = new int[count];
            }
            var id = 0;
            for (int i = 0, iMax = _entitiesCount; i < iMax; i++)
            {
                ref var entityData = ref Entities[i];
                // should we skip empty entities here?
                if (entityData.ComponentsCount >= 0)
                {
                    entities[id++] = i;
                }
            }
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter.Mask Filter<T>() where T : struct
        {
            return EcsFilter.Mask.New(this).With<T>();
        }

        public int GetComponents(int entity, ref object[] list)
        {
            var itemsCount = Entities[entity].ComponentsCount;
            if (list == null || list.Length < itemsCount)
            {
                list = new object[itemsCount];
            }
            for (int i = 0, j = 0, iMax = _poolsCount; i < iMax; i++)
            {
                if (Pools[i].Has(entity))
                {
                    list[j++] = Pools[i].GetRaw(entity);
                }
            }
            return itemsCount;
        }

        public int GetComponentTypes(int entity, ref Type[] list)
        {
            var itemsCount = Entities[entity].ComponentsCount;
            if (itemsCount == 0) { return 0; }
            if (list == null || list.Length < itemsCount)
            {
                list = new Type[Pools.Length];
            }
            for (int i = 0, j = 0, iMax = _poolsCount; i < iMax; i++)
            {
                if (Pools[i].Has(entity))
                {
                    list[j++] = Pools[i].GetComponentType();
                }
            }
            return itemsCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsEntityAliveInternal(int entity)
        {
            return entity >= 0 && entity < _entitiesCount && Entities[entity].Gen > 0;
        }

        internal (EcsFilter, bool) GetFilterInternal(EcsFilter.Mask mask, int capacity = 512)
        {
            var hash = mask.Hash;
            var exists = _filters.TryGetValue(hash, out var filter);
            if (exists) { return (filter, false); }
            filter = new EcsFilter(this, mask, capacity);
            _filters[hash] = filter;
            // add to component dictionaries for fast compatibility scan.
            for (int i = 0, iMax = mask.IncludeCount; i < iMax; i++)
            {
                var list = _filtersByIncludedComponents[mask.Include[i]];
                if (list == null)
                {
                    list = new List<EcsFilter>(8);
                    _filtersByIncludedComponents[mask.Include[i]] = list;
                }
                list.Add(filter);
            }
            for (int i = 0, iMax = mask.ExcludeCount; i < iMax; i++)
            {
                var list = _filtersByExcludedComponents[mask.Exclude[i]];
                if (list == null)
                {
                    list = new List<EcsFilter>(8);
                    _filtersByExcludedComponents[mask.Exclude[i]] = list;
                }
                list.Add(filter);
            }
            // scan exist entities for compatibility with new filter.
            for (int i = 0, iMax = _entitiesCount; i < iMax; i++)
            {
                ref var entityData = ref Entities[i];
                if (entityData.ComponentsCount > 0 && IsMaskCompatible(mask, i))
                {
                    filter.AddEntity(i);
                }
            }
#if DEBUG || LEX_WORLD_EVENTS
            for (int ii = 0, iMax = _eventListeners.Count; ii < iMax; ii++)
            {
                _eventListeners[ii].OnFilterCreated(filter);
            }
#endif
            return (filter, true);
        }

        internal void OnEntityChange(int entity, int componentType, bool added)
        {
            var includeList = _filtersByIncludedComponents[componentType];
            var excludeList = _filtersByExcludedComponents[componentType];
            if (added)
            {
                // add component.
                if (includeList != null)
                {
                    foreach (var filter in includeList)
                    {
                        if (IsMaskCompatible(filter.GetMask(), entity))
                        {
#if DEBUG
                            if (filter.EntitiesMap.ContainsKey(entity)) { throw new Exception("Entity already in filter."); }
#endif
                            filter.AddEntity(entity);
                        }
                    }
                }
                if (excludeList != null)
                {
                    foreach (var filter in excludeList)
                    {
                        if (IsMaskCompatibleWithout(filter.GetMask(), entity, componentType))
                        {
#if DEBUG
                            if (!filter.EntitiesMap.ContainsKey(entity)) { throw new Exception("Entity not in filter."); }
#endif
                            filter.RemoveEntity(entity);
                        }
                    }
                }
            }
            else
            {
                // remove component.
                if (includeList != null)
                {
                    foreach (var filter in includeList)
                    {
                        if (IsMaskCompatible(filter.GetMask(), entity))
                        {
#if DEBUG
                            if (!filter.EntitiesMap.ContainsKey(entity))
                            {
                                throw new Exception($"Entity not in filter: {entity}");
                            }
#endif
                            filter.RemoveEntity(entity);
                        }
                    }
                }
                if (excludeList != null)
                {
                    foreach (var filter in excludeList)
                    {
                        if (IsMaskCompatibleWithout(filter.GetMask(), entity, componentType))
                        {
#if DEBUG
                            if (filter.EntitiesMap.ContainsKey(entity)) { throw new Exception("Entity already in filter."); }
#endif
                            filter.AddEntity(entity);
                        }
                    }
                }
            }
        }

        protected virtual EcsPool<T> CreatePool<T>() where T : struct
        {
            return new EcsPool<T>(this, _poolsCount, Entities.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsMaskCompatible(EcsFilter.Mask filterMask, int entity)
        {
            for (int i = 0, iMax = filterMask.IncludeCount; i < iMax; i++)
            {
                if (!Pools[filterMask.Include[i]].Has(entity))
                {
                    return false;
                }
            }
            for (int i = 0, iMax = filterMask.ExcludeCount; i < iMax; i++)
            {
                if (Pools[filterMask.Exclude[i]].Has(entity))
                {
                    return false;
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsMaskCompatibleWithout(EcsFilter.Mask filterMask, int entity, int componentId)
        {
            for (int i = 0, iMax = filterMask.IncludeCount; i < iMax; i++)
            {
                var typeId = filterMask.Include[i];
                if (typeId == componentId || !Pools[typeId].Has(entity))
                {
                    return false;
                }
            }
            for (int i = 0, iMax = filterMask.ExcludeCount; i < iMax; i++)
            {
                var typeId = filterMask.Exclude[i];
                if (typeId != componentId && Pools[typeId].Has(entity))
                {
                    return false;
                }
            }
            return true;
        }

        public struct Config
        {
            public int Entities;
            public int RecycledEntities;
            public int Pools;
            public int Filters;

            internal const int EntitiesDefault = 512;
            internal const int RecycledEntitiesDefault = 512;
            internal const int PoolsDefault = 512;
            internal const int FiltersDefault = 512;
        }

        internal struct EntityData
        {
            public short Gen;
            public short ComponentsCount;
        }
    }

#if DEBUG || LEX_WORLD_EVENTS
    public interface IEcsWorldEventListener
    {
        void OnEntityCreated(int entity);
        void OnEntityChanged(int entity);
        void OnEntityDestroyed(int entity);
        void OnFilterCreated(EcsFilter filter);
        void OnWorldResized(int newSize);
        void OnWorldDestroyed(EcsWorld world);
    }
#endif
}

#if ENABLE_IL2CPP
// Unity IL2CPP performance optimization attribute.
namespace Unity.IL2CPP.CompilerServices {
    enum Option {
        NullChecks = 1,
        ArrayBoundsChecks = 2
    }

    [AttributeUsage (AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    class Il2CppSetOptionAttribute : Attribute {
        public Option Option { get; private set; }
        public object Value { get; private set; }

        public Il2CppSetOptionAttribute (Option option, object value) { Option = option; Value = value; }
    }
}
#endif