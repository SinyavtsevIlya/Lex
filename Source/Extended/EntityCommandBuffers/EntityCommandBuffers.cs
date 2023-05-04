#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif
using System.Collections.Generic;

namespace Nanory.Lex
{
    public class EcsBufferWorld : EcsWorld
    {
        private readonly EcsWorld _dstWorld;

        public EcsBufferWorld(EcsWorld dstWorld, Config cfg) : base(cfg)
        {
            _dstWorld = dstWorld;
        }

        protected override EcsPool<TComponent> CreatePool<TComponent>()
        {
            var dstPool = _dstWorld.GetPool<TComponent>();
            var bufferPool = new EcsPool<TComponent>(this, _poolsCount, Entities.Length, dstPool);

            return bufferPool;
        }
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif

    public class EntityCommandBuffer
    {
        public EcsWorld BufferWorld;
        public EcsWorld DstWorld;

        private List<Op> _ops;

        public EntityCommandBuffer(EcsWorld dstWorld, in EcsWorld.Config cfg = default)
        {
            BufferWorld = new EcsBufferWorld(dstWorld, cfg);
            DstWorld = dstWorld;

            _ops = new List<Op>(128);
        }

        public void Schedule(Op op)
        {
            _ops.Add(op);
        }

        public void Playback()
        {
            foreach (var op in _ops)
            {
                var pool = DstWorld.PoolsSparse[op.ComponentIndex];
                var bufferPool = BufferWorld.PoolsSparse[op.ComponentIndex];

                if (op.Entity.Unpack(DstWorld, out var dstEntity))
                {
                    switch (op.OpType)
                    {
                        case OpType.DelEntity:
                            DstWorld.DelEntity(dstEntity);
                            break;
                        case OpType.Del:
                            DstWorld.PoolsSparse[op.ComponentIndex].Del(dstEntity);
                            break;
                        case OpType.AddOrSet:
                            if (!pool.Has(dstEntity))
                            {
                                pool.Activate(dstEntity);
                            }
                            bufferPool.CpyToDstWorld(op.BufferEntity, dstEntity);
                            break;
                        case OpType.Add:
                            if (!pool.Has(dstEntity))
                            {
                                pool.Activate(dstEntity);
                                bufferPool.CpyToDstWorld(op.BufferEntity, dstEntity);
                            }
                            break;
                        case OpType.Set:
#if DEBUG && ENABLE_ECB_STACKTRACE
                            if (!pool.Has(dstEntity))
                            {
                                throw new System.Exception(
                                    $"Entity Command Buffer: Unable to set the {pool.GetComponentType()} " +
                                    $"value to entity-{dstEntity} that doesn't have such component. " +
                                    $"Call location: {op.CallLocation}");
                            }
#endif
                            bufferPool.CpyToDstWorld(op.BufferEntity, dstEntity);
                            break;
                        case OpType.Activate:
                            pool.Activate(dstEntity);
                            break;
                        default:
                            break;
                    }

                    if (op.BufferEntity != -1)
                    {
                        BufferWorld.DelEntity(op.BufferEntity);
                    }
                }
            }
            _ops.Clear();
        }

        public bool IsEmpty() => _ops.Count == 0;

        public struct Op
        {
            public OpType OpType;
            public int ComponentIndex;
            public EcsPackedEntity Entity;
            public int BufferEntity;
#if DEBUG && ENABLE_ECB_STACKTRACE
            public string CallLocation;
#endif
        }

        public enum OpType
        {
            DelEntity,
            Del,
            AddOrSet,
            Add,
            Set,
            Activate
        }
    }

    public static class EcsBufferWorldExtensions
    {
        public static ref TComponent Add<TComponent>(this EntityCommandBuffer entityCommandBuffer, int entity) where TComponent : struct
        {
            var bufferEntity = entityCommandBuffer.BufferWorld.NewEntity();
            entityCommandBuffer.Schedule(new EntityCommandBuffer.Op()
            {
                OpType = EntityCommandBuffer.OpType.Add,
                ComponentIndex = EcsComponent<TComponent>.TypeIndex,
                Entity = entityCommandBuffer.DstWorld.PackEntity(entity),
                BufferEntity = bufferEntity,
#if DEBUG && ENABLE_ECB_STACKTRACE
                CallLocation = GetCallLocation()
#endif
            });
            return ref entityCommandBuffer.BufferWorld.GetPool<TComponent>().Add(bufferEntity);
        }

        public static void Add<TComponent1, TComponent2>(this EntityCommandBuffer entityCommandBuffer, int entity)
            where TComponent1 : struct
            where TComponent2 : struct
        {
            entityCommandBuffer.Add<TComponent1>(entity);
            entityCommandBuffer.Add<TComponent2>(entity);
        }
        
        public static void Del<TComponent1, TComponent2>(this EntityCommandBuffer entityCommandBuffer, int entity)
            where TComponent1 : struct
            where TComponent2 : struct
        {
            entityCommandBuffer.Del<TComponent1>(entity);
            entityCommandBuffer.Del<TComponent2>(entity);
        }

        public static ref TComponent Set<TComponent>(this EntityCommandBuffer entityCommandBuffer, int entity) where TComponent : struct
        {
            var bufferEntity = entityCommandBuffer.BufferWorld.NewEntity();
            entityCommandBuffer.Schedule(new EntityCommandBuffer.Op()
            {
                OpType = EntityCommandBuffer.OpType.Set,
                ComponentIndex = EcsComponent<TComponent>.TypeIndex,
                Entity = entityCommandBuffer.DstWorld.PackEntity(entity),
                BufferEntity = bufferEntity,
#if DEBUG && ENABLE_ECB_STACKTRACE
                CallLocation = GetCallLocation()
#endif
            });
            return ref entityCommandBuffer.BufferWorld.GetPool<TComponent>().Add(bufferEntity);
        }

        public static ref TComponent AddOrSet<TComponent>(this EntityCommandBuffer entityCommandBuffer, int entity) where TComponent : struct
        {
            var bufferEntity = entityCommandBuffer.BufferWorld.NewEntity();
            entityCommandBuffer.Schedule(new EntityCommandBuffer.Op()
            {
                OpType = EntityCommandBuffer.OpType.AddOrSet,
                ComponentIndex = EcsComponent<TComponent>.TypeIndex,
                Entity = entityCommandBuffer.DstWorld.PackEntity(entity),
                BufferEntity = bufferEntity,
#if DEBUG && ENABLE_ECB_STACKTRACE
                CallLocation = GetCallLocation()
#endif
            });
            return ref entityCommandBuffer.BufferWorld.GetPool<TComponent>().Add(bufferEntity);
        }

        public static void Del<TComponent>(this EntityCommandBuffer entityCommandBuffer, int entity) where TComponent : struct
        {
            entityCommandBuffer.Schedule(new EntityCommandBuffer.Op()
            {
                OpType = EntityCommandBuffer.OpType.Del,
                ComponentIndex = EcsComponent<TComponent>.TypeIndex,
                Entity = entityCommandBuffer.DstWorld.PackEntity(entity),
                BufferEntity = -1,
#if DEBUG && ENABLE_ECB_STACKTRACE
                CallLocation = GetCallLocation()
#endif
            });
        }

        public static void Del(this EntityCommandBuffer entityCommandBuffer, int entity, int componentIndex)
        {
            entityCommandBuffer.Schedule(new EntityCommandBuffer.Op()
            {
                OpType = EntityCommandBuffer.OpType.Del,
                ComponentIndex = componentIndex,
                Entity = entityCommandBuffer.DstWorld.PackEntity(entity),
                BufferEntity = -1,
#if DEBUG && ENABLE_ECB_STACKTRACE
                CallLocation = GetCallLocation()
#endif
            });
        }

        /// <summary>
        /// <remarks>Component pool of passed <see cref="componentIndex"/> should be previously initialized using
        /// <see cref="EcsWorld.GetPool{TComponent}"/> otherwise an <see cref="System.Exception"/> will be thrown on Playback</remarks>
        /// </summary>
        public static void AddOrSet(this EntityCommandBuffer entityCommandBuffer, int entity, int componentIndex)
        {
            var bufferEntity = entityCommandBuffer.BufferWorld.NewEntity();
            entityCommandBuffer.Schedule(new EntityCommandBuffer.Op()
            {
                OpType = EntityCommandBuffer.OpType.AddOrSet,
                ComponentIndex = componentIndex,
                Entity = entityCommandBuffer.DstWorld.PackEntity(entity),
                BufferEntity = bufferEntity,
#if DEBUG && ENABLE_ECB_STACKTRACE
                CallLocation = GetCallLocation()
#endif
            });
            entityCommandBuffer.BufferWorld.PoolsSparse[componentIndex].Activate(bufferEntity);
        }
        
        public static void Activate(this EntityCommandBuffer entityCommandBuffer, int entity, int componentIndex)
        {
            var bufferEntity = entityCommandBuffer.BufferWorld.NewEntity();
            entityCommandBuffer.Schedule(new EntityCommandBuffer.Op()
            {
                OpType = EntityCommandBuffer.OpType.Activate,
                ComponentIndex = componentIndex,
                Entity = entityCommandBuffer.DstWorld.PackEntity(entity),
                BufferEntity = bufferEntity,
#if DEBUG && ENABLE_ECB_STACKTRACE
                CallLocation = GetCallLocation()
#endif
            });
        }

        public static void DelBuffer<TElement>(this EntityCommandBuffer entityCommandBuffer, int entity) where TElement : struct
        {
            entityCommandBuffer.Schedule(new EntityCommandBuffer.Op()
            {
                OpType = EntityCommandBuffer.OpType.Del,
                ComponentIndex = EcsComponent<Buffer<TElement>>.TypeIndex,
                Entity = entityCommandBuffer.DstWorld.PackEntity(entity),
                BufferEntity = -1,
#if DEBUG && ENABLE_ECB_STACKTRACE
                CallLocation = GetCallLocation()
#endif
            });
        }

        public static void DelEntity(this EntityCommandBuffer entityCommandBuffer, int entity)
        {
            var bufferEntity = entityCommandBuffer.BufferWorld.NewEntity();
            entityCommandBuffer.Schedule(new EntityCommandBuffer.Op()
            {
                OpType = EntityCommandBuffer.OpType.DelEntity,
                Entity = entityCommandBuffer.DstWorld.PackEntity(entity),
                BufferEntity = -1,
#if DEBUG && ENABLE_ECB_STACKTRACE
                CallLocation = GetCallLocation()
#endif
            });
        }
#if DEBUG && ENABLE_ECB_STACKTRACE
        private static string GetCallLocation() => System.Environment.StackTrace.Split(new string[] { System.Environment.NewLine }, System.StringSplitOptions.None)[3];
#endif
    }
}