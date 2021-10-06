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
        int _capacity;

        EcsFilter _ecbNewEntityFilter;
        EcsFilter _ecbSetFilter;
        EcsFilter _ecbAddFilter;
        EcsFilter _ecbDelFilter;
        EcsFilter _ecbDelEntityFilter;

        public readonly EcsPool<ECBNewEntityCommand> PoolECBNewEntityCommand;
        public readonly EcsPool<ECBSetCommand> PoolECBSetCommand;
        public readonly EcsPool<ECBAddCommand> PoolECBAddCommand;
        public readonly EcsPool<ECBDelCommand> PoolECBDelCommand;
        public readonly EcsPool<ECBDelEntityCommand> PoolECBDelEntityCommand;

        public EntityCommandBuffer(EcsWorld dstWorld, in EcsWorld.Config cfg = default)
        {
            BufferWorld = new EcsBufferWorld(dstWorld, cfg);
            DstWorld = dstWorld;
            CheckGrow();

            _ecbNewEntityFilter = BufferWorld.Filter<ECBNewEntityCommand>().End();
            _ecbSetFilter = BufferWorld.Filter<ECBSetCommand>().End();
            _ecbAddFilter = BufferWorld.Filter<ECBAddCommand>().End();
            _ecbDelFilter = BufferWorld.Filter<ECBDelCommand>().End();
            _ecbDelEntityFilter = BufferWorld.Filter<ECBDelEntityCommand>().End();

            PoolECBNewEntityCommand = BufferWorld.GetPool<ECBNewEntityCommand>();
            PoolECBSetCommand = BufferWorld.GetPool<ECBSetCommand>();
            PoolECBAddCommand = BufferWorld.GetPool<ECBAddCommand>();
            PoolECBDelCommand = BufferWorld.GetPool<ECBDelCommand>();
            PoolECBDelEntityCommand = BufferWorld.GetPool<ECBDelEntityCommand>();
        }

        public void CheckGrow()
        {
            if (_capacity < DstWorld.Entities.Length)
            {
                for (int idx = _capacity; idx < DstWorld.Entities.Length; idx++)
                {
                    BufferWorld.NewEntity();
                }

                _capacity = DstWorld.Entities.Length;
            }
        }

        public void Playback()
        {
            CheckGrow();

            foreach (var entity in _ecbNewEntityFilter)
            {
                DstWorld.NewEntity();
                PoolECBNewEntityCommand.Del(entity);
            }

            foreach (var entity in _ecbSetFilter)
            {
                ref var setCmd = ref PoolECBSetCommand.Get(entity);
                BufferWorld.PoolsSparse[setCmd.componentIndex].CpyToDstWorld(setCmd.entity);

                PoolECBSetCommand.Del(entity);
            }

            foreach (var entity in _ecbAddFilter)
            {
                ref var addCmd = ref PoolECBAddCommand.Get(entity);
                var pool = DstWorld.PoolsSparse[addCmd.componentIndex];
                pool.Activate(addCmd.entity);

                var bufferPool = BufferWorld.PoolsSparse[addCmd.componentIndex];
                bufferPool.CpyToDstWorld(addCmd.entity);
                bufferPool.Del(entity);

                PoolECBAddCommand.Del(entity);

            }
            
            foreach (var entity in _ecbDelFilter)
            {
                ref var delCmd = ref PoolECBDelCommand.Get(entity);
                DstWorld.PoolsSparse[delCmd.componentIndex].Del(delCmd.entity);
                PoolECBDelCommand.Del(entity);
            }

            foreach (var entity in _ecbDelEntityFilter)
            {
                ref var delEntityCmd = ref PoolECBDelCommand.Get(entity);
                DstWorld.DelEntity(delEntityCmd.entity);
                PoolECBDelEntityCommand.Del(entity);
            }
        }

        public struct ECBSetCommand
        {
            public int entity;
            public int componentIndex;
        }

        public struct ECBAddCommand
        {
            public int entity;
            public int componentIndex;
        }

        public struct ECBDelCommand
        {
            public int entity;
            public int componentIndex;
        }

        public struct ECBNewEntityCommand 
        {
            public int entity;
        }
        public struct ECBDelEntityCommand 
        {
            public int entity;
        }
    }

    public static class EcsBufferWorldExtensions
    {
        public static ref TComponent Add<TComponent>(this EntityCommandBuffer entityCommandBuffer, int entity) where TComponent : struct
        {
            entityCommandBuffer.PoolECBAddCommand.Add(entity) = new EntityCommandBuffer.ECBAddCommand()
            {
                componentIndex = EcsComponent<TComponent>.TypeIndex,
                entity = entity
            };
            return ref entityCommandBuffer.BufferWorld.GetPool<TComponent>().Add(entity);
        }

        public static ref TComponent Set<TComponent>(this EntityCommandBuffer entityCommandBuffer, int entity) where TComponent : struct
        {
            entityCommandBuffer.PoolECBSetCommand.Add(entity) = new EntityCommandBuffer.ECBSetCommand()
            {
                componentIndex = EcsComponent<TComponent>.TypeIndex,
                entity = entity
            };
            if (!entityCommandBuffer.BufferWorld.Has<TComponent>(entity))
            {
                return ref entityCommandBuffer.BufferWorld.GetPool<TComponent>().Add(entity);
            }
            else
            {
                return ref entityCommandBuffer.BufferWorld.GetPool<TComponent>().Get(entity);
            }
        }

        public static void Del<TComponent>(this EntityCommandBuffer entityCommandBuffer, int entity) where TComponent : struct
        {
            entityCommandBuffer.PoolECBDelCommand.Add(entity) = new EntityCommandBuffer.ECBDelCommand()
            {
                componentIndex = EcsComponent<TComponent>.TypeIndex,
                entity = entity
            };
        }

        public static void Del(this EntityCommandBuffer entityCommandBuffer, int entity, int componentIndex)
        {
            entityCommandBuffer.PoolECBDelCommand.Add(entity) = new EntityCommandBuffer.ECBDelCommand()
            {
                componentIndex = componentIndex,
                entity = entity
            };
        }

        public static void DelBuffer<TElement>(this EntityCommandBuffer entityCommandBuffer, int entity) where TElement : struct
        {
            entityCommandBuffer.PoolECBDelCommand.Add(entity) = new EntityCommandBuffer.ECBDelCommand()
            {
                componentIndex = EcsComponent<Buffer<TElement>>.TypeIndex,
                entity = entity
            };
        }

        public static void DelEntity<TComponent>(this EntityCommandBuffer entityCommandBuffer, int entity) where TComponent : struct
        {
            entityCommandBuffer.PoolECBDelEntityCommand.Add(entity) = new EntityCommandBuffer.ECBDelEntityCommand()
            {
                entity = entity
            };
        }

        public static int NewEntity(this EntityCommandBuffer buffer)
        {
            var entity = buffer.NewEntity();
            buffer.PoolECBNewEntityCommand.Add(entity);
            return entity;
        }
    }
}