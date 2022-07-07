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

        // TODO: Since every GetPool() in BufferWorld implicitly results in GetPool() from DstWorld, 
        // even internal ECB command pools are created inside DstWorld. 
        // There are only 4 of them and this is not critical, but it is better to fix it.

        protected override EcsPool<TComponent> CreatePool<TComponent>()
        {
            return new EcsPool<TComponent>(this, _poolsCount, Entities.Length, _dstWorld.GetPool<TComponent>().GetItems());
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

        public List<Op> _ops;

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

                switch (op.OpType)
                {
                    case OpType.NewEntity:
                        DstWorld.NewEntity();
                        break;
                    case OpType.DelEntity:
                        DstWorld.DelEntity(op.Entity);
                        break;
                    case OpType.Del:
                        DstWorld.PoolsSparse[op.ComponentIndex].Del(op.Entity);
                        break;
                    case OpType.AddOrSet:
                        if (!pool.Has(op.Entity))
                        {
                            pool.Activate(op.Entity);
                        }
                        bufferPool.CpyToDstWorld(op.BufferEntity, op.Entity);
                        break;
                    case OpType.Add:
                        if (!pool.Has(op.Entity))
                        {
                            pool.Activate(op.Entity);
                            bufferPool.CpyToDstWorld(op.BufferEntity, op.Entity);
                        }
                        break;
                    case OpType.Set:
#if DEBUG
                        if (!pool.Has(op.Entity))
                        {
                            throw new System.Exception(
                                $"Entity Command Buffer: Unable to set the {pool.GetComponentType()} " +
                                $"value to entity-{op.Entity} that doesn't have such component. " +
                                $"Call location: {op.CallLocation}");
                        }
#endif
                        bufferPool.CpyToDstWorld(op.BufferEntity, op.Entity);
                        break;
                    default:
                        break;
                }

                if (op.BufferEntity != -1)
                {
                    BufferWorld.DelEntity(op.BufferEntity);
                }
            }
            _ops.Clear();
        }

        public struct Op
        {
            public OpType OpType;
            public int ComponentIndex;
            public int Entity;
            public int BufferEntity;
#if DEBUG
            public string CallLocation;
#endif
        }

        public enum OpType
        {
            NewEntity,
            DelEntity,
            Del,
            AddOrSet,
            Add,
            Set
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
                Entity = entity,
                BufferEntity = bufferEntity,
#if DEBUG
                CallLocation = GetCallLocation()
#endif
            });
            return ref entityCommandBuffer.BufferWorld.GetPool<TComponent>().Add(bufferEntity);
        }

        public static ref TComponent Set<TComponent>(this EntityCommandBuffer entityCommandBuffer, int entity) where TComponent : struct
        {
            var bufferEntity = entityCommandBuffer.BufferWorld.NewEntity();
            entityCommandBuffer.Schedule(new EntityCommandBuffer.Op()
            {
                OpType = EntityCommandBuffer.OpType.Set,
                ComponentIndex = EcsComponent<TComponent>.TypeIndex,
                Entity = entity,
                BufferEntity = bufferEntity,
#if DEBUG
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
                Entity = entity,
                BufferEntity = bufferEntity,
#if DEBUG
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
                Entity = entity,
                BufferEntity = -1,
#if DEBUG
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
                Entity = entity,
                BufferEntity = -1,
#if DEBUG
                CallLocation = GetCallLocation()
#endif
            });
        }

        public static void AddOrSet(this EntityCommandBuffer entityCommandBuffer, int entity, int componentIndex)
        {
            var bufferEntity = entityCommandBuffer.BufferWorld.NewEntity();
            entityCommandBuffer.Schedule(new EntityCommandBuffer.Op()
            {
                OpType = EntityCommandBuffer.OpType.AddOrSet,
                ComponentIndex = componentIndex,
                Entity = entity,
                BufferEntity = bufferEntity,
#if DEBUG
                CallLocation = GetCallLocation()
#endif
            });
            entityCommandBuffer.BufferWorld.PoolsSparse[componentIndex].Activate(bufferEntity);
        }

        public static void DelBuffer<TElement>(this EntityCommandBuffer entityCommandBuffer, int entity) where TElement : struct
        {
            entityCommandBuffer.Schedule(new EntityCommandBuffer.Op()
            {
                OpType = EntityCommandBuffer.OpType.Del,
                ComponentIndex = EcsComponent<Buffer<TElement>>.TypeIndex,
                Entity = entity,
                BufferEntity = -1,
#if DEBUG
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
                Entity = entity,
                BufferEntity = -1,
#if DEBUG
                CallLocation = GetCallLocation()
#endif
            });
        }

        private static string GetCallLocation() => System.Environment.StackTrace.Split(new string[] { System.Environment.NewLine }, System.StringSplitOptions.None)[3];
    }
}