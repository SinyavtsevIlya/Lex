#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace Nanory.Lex
{

    public class EcsBufferWorld : EcsWorld
    {
        private readonly EcsWorld _dstWorld; 

        public EcsBufferWorld(EcsWorld dstWorld, Config cfg) : base (cfg)
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
        private int _capacity;

        private readonly EcsFilter _ecbNewEntityFilter;
        private readonly EcsFilter _ecbSetFilter;
        private readonly EcsFilter _ecbAddFilter;
        private readonly EcsFilter _ecbAddOrSetFilter;
        private readonly EcsFilter _ecbDelFilter;
        private readonly EcsFilter _ecbDelEntityFilter;

        public readonly EcsPool<NewEntityCommand> PoolECBNewEntityCommand;
        public readonly EcsPool<SetCommand> PoolECBSetCommand;
        public readonly EcsPool<AddCommand> PoolECBAddCommand;
        public readonly EcsPool<AddOrSetCommand> PoolECBAddOrSetCommand;
        public readonly EcsPool<DelCommand> PoolECBDelCommand;
        public readonly EcsPool<DelEntityCommand> PoolECBDelEntityCommand;

        public EntityCommandBuffer(EcsWorld dstWorld, in EcsWorld.Config cfg = default)
        {
            BufferWorld = new EcsBufferWorld(dstWorld, cfg);
            DstWorld = dstWorld;

            _ecbNewEntityFilter = BufferWorld.Filter<NewEntityCommand>().End();
            _ecbSetFilter = BufferWorld.Filter<SetCommand>().End();
            _ecbAddFilter = BufferWorld.Filter<AddCommand>().End();
            _ecbAddOrSetFilter = BufferWorld.Filter<AddOrSetCommand>().End();
            _ecbDelFilter = BufferWorld.Filter<DelCommand>().End();
            _ecbDelEntityFilter = BufferWorld.Filter<DelEntityCommand>().End();

            PoolECBNewEntityCommand = BufferWorld.GetPool<NewEntityCommand>();
            PoolECBSetCommand = BufferWorld.GetPool<SetCommand>();
            PoolECBAddCommand = BufferWorld.GetPool<AddCommand>();
            PoolECBAddOrSetCommand = BufferWorld.GetPool<AddOrSetCommand>();
            PoolECBDelCommand = BufferWorld.GetPool<DelCommand>();
            PoolECBDelEntityCommand = BufferWorld.GetPool<DelEntityCommand>();
        }
        
        public void Playback()
        {
            foreach (var bufferEntity in _ecbNewEntityFilter)
            {
                DstWorld.NewEntity();
                BufferWorld.DelEntity(bufferEntity);
            }

            foreach (var bufferEntity in _ecbSetFilter)
            {
                ref var setCmd = ref PoolECBSetCommand.Get(bufferEntity);
                BufferWorld.PoolsSparse[setCmd.componentIndex].CpyToDstWorld(bufferEntity, setCmd.entity);
                BufferWorld.DelEntity(bufferEntity);
            }

            foreach (var bufferEntity in _ecbAddFilter)
            {
                ref var addCmd = ref PoolECBAddCommand.Get(bufferEntity);
                var pool = DstWorld.PoolsSparse[addCmd.componentIndex];
                pool.Activate(addCmd.entity);
                var bufferPool = BufferWorld.PoolsSparse[addCmd.componentIndex];
                bufferPool.CpyToDstWorld(bufferEntity, addCmd.entity);
                BufferWorld.DelEntity(bufferEntity);
            }

            foreach (var bufferEntity in _ecbAddOrSetFilter)
            {
                ref var addOrSetCmd = ref PoolECBAddOrSetCommand.Get(bufferEntity);
                var pool = DstWorld.PoolsSparse[addOrSetCmd.componentIndex];
                if (!pool.Has(addOrSetCmd.entity))
                {
                    pool.Activate(addOrSetCmd.entity);
                }
                var bufferPool = BufferWorld.PoolsSparse[addOrSetCmd.componentIndex];
                bufferPool.CpyToDstWorld(bufferEntity, addOrSetCmd.entity);
                BufferWorld.DelEntity(bufferEntity);
            }

            foreach (var bufferEntity in _ecbDelFilter)
            {
                ref var delCmd = ref PoolECBDelCommand.Get(bufferEntity);
                DstWorld.PoolsSparse[delCmd.componentIndex].Del(delCmd.entity);
                BufferWorld.DelEntity(bufferEntity);
            }

            foreach (var bufferEntity in _ecbDelEntityFilter)
            {
                ref var delEntityCmd = ref PoolECBDelEntityCommand.Get(bufferEntity);
                DstWorld.DelEntity(delEntityCmd.entity);
                BufferWorld.DelEntity(bufferEntity);
            }
        }

        public struct SetCommand
        {
            public int entity;
            public int componentIndex;
        }

        public struct AddCommand
        {
            public int entity;
            public int componentIndex;
        }

        public struct AddOrSetCommand
        {
            public int entity;
            public int componentIndex;
        }

        public struct DelCommand
        {
            public int entity;
            public int componentIndex;
        }

        public struct NewEntityCommand 
        {
            public int entity;
        }
        public struct DelEntityCommand 
        {
            public int entity;
        }
    }

    public static class EcsBufferWorldExtensions
    {
        public static ref TComponent Add<TComponent>(this EntityCommandBuffer entityCommandBuffer, int entity) where TComponent : struct
        {
            var bufferEntity = entityCommandBuffer.BufferWorld.NewEntity();
            entityCommandBuffer.PoolECBAddCommand.Add(bufferEntity) = new EntityCommandBuffer.AddCommand()
            {
                componentIndex = EcsComponent<TComponent>.TypeIndex,
                entity = entity
            };
            return ref entityCommandBuffer.BufferWorld.GetPool<TComponent>().Add(bufferEntity);
        }

        public static ref TComponent Set<TComponent>(this EntityCommandBuffer entityCommandBuffer, int entity) where TComponent : struct
        {
            var bufferEntity = entityCommandBuffer.BufferWorld.NewEntity();
            entityCommandBuffer.PoolECBSetCommand.Add(bufferEntity) = new EntityCommandBuffer.SetCommand()
            {
                componentIndex = EcsComponent<TComponent>.TypeIndex,
                entity = entity
            };
            var pool = entityCommandBuffer.BufferWorld.GetPool<TComponent>();

            if (!entityCommandBuffer.BufferWorld.Has<TComponent>(bufferEntity)) // ?
            {
                return ref pool.Add(bufferEntity);
            }
            else
            {
                return ref pool.Get(bufferEntity);
            }
        }

        public static ref TComponent AddOrSet<TComponent>(this EntityCommandBuffer entityCommandBuffer, int entity) where TComponent : struct
        {
            var bufferEntity = entityCommandBuffer.BufferWorld.NewEntity();
            entityCommandBuffer.PoolECBAddOrSetCommand.Add(bufferEntity) = new EntityCommandBuffer.AddOrSetCommand()
            {
                componentIndex = EcsComponent<TComponent>.TypeIndex,
                entity = entity
            };
            return ref entityCommandBuffer.BufferWorld.GetPool<TComponent>().Add(bufferEntity);
        }

        public static void Del<TComponent>(this EntityCommandBuffer entityCommandBuffer, int entity) where TComponent : struct
        {
            var bufferEntity = entityCommandBuffer.BufferWorld.NewEntity();
            entityCommandBuffer.PoolECBDelCommand.Add(bufferEntity) = new EntityCommandBuffer.DelCommand()
            {
                componentIndex = EcsComponent<TComponent>.TypeIndex,
                entity = entity
            };
        }

        public static void Del(this EntityCommandBuffer entityCommandBuffer, int entity, int componentIndex)
        {
            var bufferEntity = entityCommandBuffer.BufferWorld.NewEntity();
            entityCommandBuffer.PoolECBDelCommand.Add(bufferEntity) = new EntityCommandBuffer.DelCommand()
            {
                componentIndex = componentIndex,
                entity = entity
            };
        }

        public static void Add(this EntityCommandBuffer entityCommandBuffer, int entity, int componentIndex)
        {
            var bufferEntity = entityCommandBuffer.BufferWorld.NewEntity();
            entityCommandBuffer.PoolECBAddCommand.Add(bufferEntity) = new EntityCommandBuffer.AddCommand()
            {
                componentIndex = componentIndex,
                entity = entity
            };
            entityCommandBuffer.BufferWorld.PoolsSparse[componentIndex].Activate(bufferEntity);
        }

        public static void DelBuffer<TElement>(this EntityCommandBuffer entityCommandBuffer, int entity) where TElement : struct
        {
            var bufferEntity = entityCommandBuffer.BufferWorld.NewEntity();
            entityCommandBuffer.PoolECBDelCommand.Add(bufferEntity) = new EntityCommandBuffer.DelCommand()
            {
                componentIndex = EcsComponent<Buffer<TElement>>.TypeIndex,
                entity = entity
            };
        }

        public static void DelEntity(this EntityCommandBuffer entityCommandBuffer, int entity)
        {
            var bufferEntity = entityCommandBuffer.BufferWorld.NewEntity();
            entityCommandBuffer.PoolECBDelEntityCommand.Add(bufferEntity) = new EntityCommandBuffer.DelEntityCommand()
            {
                entity = entity
            };
        }
    }
}