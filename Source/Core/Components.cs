using System;
using System.Runtime.CompilerServices;

#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace Nanory.Lex
{
    public interface IEcsPool
    {
        void Resize(int capacity);
        bool Has(int entity);
        void Del(int entity);
        void InitAutoReset(int entity);
        object GetRaw(int entity);
        int GetId();
        Type GetComponentType();
        void Destroy();
        void CpyToDstWorld(int src, int dst);
        void CpyToDstEntity(int src, int dst);
        void Activate(int entity);
    }

    public interface IEcsAutoReset<T> where T : struct
    {
        void AutoReset(ref T c);
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif
    public sealed class EcsPool<T> : IEcsPool where T : struct
    {
        readonly Type _type;
        readonly EcsWorld _world;
        readonly int _id;
        readonly AutoResetHandler _autoReset;
        public PoolItem[] Items;
        PoolItem[] _dstItems;
#if ENABLE_IL2CPP && !UNITY_EDITOR
        T _autoresetFakeInstance;
#endif

        internal EcsPool(EcsWorld world, int id, int capacity, PoolItem[] dstItems = null)
        {
            _type = typeof(T);
            _world = world;
            _id = id;
            Items = new PoolItem[capacity];
            _dstItems = dstItems;
            var isAutoReset = typeof(IEcsAutoReset<T>).IsAssignableFrom(_type);
#if DEBUG
            if (!isAutoReset && _type.GetInterface("IEcsAutoReset`1") != null)
            {
                throw new Exception($"IEcsAutoReset should have <{typeof(T).Name}> constraint for component \"{typeof(T).Name}\".");
            }
#endif
            if (isAutoReset)
            {
                var autoResetMethod = typeof(T).GetMethod(nameof(IEcsAutoReset<T>.AutoReset));
#if DEBUG
                if (autoResetMethod == null)
                {
                    throw new Exception(
                        $"IEcsAutoReset<{typeof(T).Name}> explicit implementation not supported, use implicit instead.");
                }
#endif
                _autoReset = (AutoResetHandler)Delegate.CreateDelegate(
                    typeof(AutoResetHandler),
#if ENABLE_IL2CPP && !UNITY_EDITOR
                    _autoresetFakeInstance,
#else
                    null,
#endif
                    autoResetMethod);
            }
        }

#if UNITY_2020_3_OR_NEWER
        [UnityEngine.Scripting.Preserve]
#endif
        void ReflectionSupportHack()
        {
            _world.GetPool<T>();
            _world.Filter<T>().Without<T>().End();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetId()
        {
            return _id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Type GetComponentType()
        {
            return _type;
        }

        internal PoolItem[] GetItems() => Items;

        void IEcsPool.Resize(int capacity)
        {
            Array.Resize(ref Items, capacity);
        }

        void IEcsPool.InitAutoReset(int entity)
        {
            _autoReset?.Invoke(ref Items[entity].Data);
        }

        object IEcsPool.GetRaw(int entity)
        {
            return Items[entity].Data;
        }

        void IEcsPool.Destroy()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Add(int entity)
        {
#if DEBUG
            if (!_world.IsEntityAliveInternal(entity)) { throw new Exception("Cant touch destroyed entity."); }
#endif
            ref var itemData = ref Items[entity];
#if DEBUG
            if (_world.GetEntityGen(entity) < 0) { throw new Exception("Cant add component to destroyed entity."); }
            if (itemData.Attached) { throw new Exception($"{typeof(T).Name} is Already attached to entity {entity}"); }
#endif
            itemData.Attached = true;
            _world.OnEntityChange(entity, _id, true);
            _world.Entities[entity].ComponentsCount++;
#if DEBUG || LEX_WORLD_EVENTS
            _world.RaiseEntityChangeEvent(entity);
#endif
            return ref itemData.Data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(int entity)
        {
#if DEBUG
            if (!_world.IsEntityAliveInternal(entity)) { throw new Exception("Cant touch destroyed entity."); }
#endif
#if DEBUG
            if (_world.GetEntityGen(entity) < 0) { throw new Exception("Cant get component from destroyed entity."); }
            if (!Items[entity].Attached) { throw new Exception($"Component {_type} is not attached to entity {entity}."); }
#endif
            return ref Items[entity].Data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entity)
        {
#if DEBUG
            if (!_world.IsEntityAliveInternal(entity))
            {
                throw new Exception("Cant touch destroyed entity.");
            }
#endif
            return Items[entity].Attached;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(int entity)
        {
#if DEBUG
            if (!_world.IsEntityAliveInternal(entity)) { throw new Exception("Cant touch destroyed entity."); }
#endif
            ref var itemData = ref Items[entity];
            if (itemData.Attached)
            {
                _world.OnEntityChange(entity, _id, false);
                itemData.Attached = false;
                if (_autoReset != null)
                {
                    _autoReset.Invoke(ref Items[entity].Data);
                }
                else
                {
                    itemData.Data = default;
                }
#if DEBUG || LEX_WORLD_EVENTS
                _world.RaiseEntityChangeEvent(entity);
#endif
                ref var entityData = ref _world.Entities[entity];
                entityData.ComponentsCount--;
                if (entityData.ComponentsCount == 0)
                {
                    _world.DelEntity(entity);
                }
            }
        }

        public void CpyToDstWorld(int src, int dst)
        {
            _dstItems[dst] = Items[src];
        }

        public void CpyToDstEntity(int src, int dst)
        {
            Activate(dst);
            Items[dst] = Items[src];
        }

        public void Activate(int entity)
        {
#if DEBUG
            if (!_world.IsEntityAliveInternal(entity)) { throw new Exception($"Unable to activate {typeof(T).Name} on {entity}."); }
#endif
            ref var itemData = ref Items[entity];
#if DEBUG
            if (_world.GetEntityGen(entity) < 0) { throw new Exception($"Cant add {typeof(T).Name} to destroyed entity {entity}."); }
            if (itemData.Attached) { throw new Exception($"{typeof(T).Name} is Already attached to entity {entity}"); }
#endif
            itemData.Attached = true;
            _world.OnEntityChange(entity, _id, true);
            _world.Entities[entity].ComponentsCount++;
#if DEBUG || LEX_WORLD_EVENTS
            _world.RaiseEntityChangeEvent(entity);
#endif
        }

        public struct PoolItem
        {
            public bool Attached;
            public T Data;
        }

        delegate void AutoResetHandler(ref T component);
    }
}