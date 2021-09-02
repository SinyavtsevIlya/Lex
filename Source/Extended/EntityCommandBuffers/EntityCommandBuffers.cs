#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace Nanory.Lex
{
#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif

    public class EcsBufferWorld : EcsWorld
    {
        public EcsWorld DstWorld;
        int _capacity;

        EcsFilter _ecbNewEntityFilter;
        EcsFilter _ecbSetFilter;
        EcsFilter _ecbDelFilter;
        EcsFilter _ecbDelEntityFilter;

        public readonly EcsPool<ECBNewEntityCommand> PoolECBNewEntityCommand;
        public readonly EcsPool<ECBSetCommand> PoolECBSetCommand;
        public readonly EcsPool<ECBDelCommand> PoolECBDelCommand;
        public readonly EcsPool<ECBDelEntityCommand> PoolECBDelEntityCommand;

        public EcsBufferWorld(EcsWorld dstWorld, in Config cfg = default) : base(cfg)
        {
            DstWorld = dstWorld;
            CheckGrow();

            _ecbNewEntityFilter = Filter<ECBNewEntityCommand>().End();
            _ecbSetFilter = Filter<ECBSetCommand>().End();
            _ecbDelFilter = Filter<ECBDelCommand>().End();
            _ecbDelEntityFilter = Filter<ECBDelEntityCommand>().End();

            PoolECBNewEntityCommand = GetPool<ECBNewEntityCommand>();
            PoolECBSetCommand = GetPool<ECBSetCommand>();
            PoolECBDelCommand = GetPool<ECBDelCommand>();
            PoolECBDelEntityCommand = GetPool<ECBDelEntityCommand>();
        }

        public void CheckGrow()
        {
            if (_capacity < DstWorld.Entities.Length)
            {
                for (int idx = _capacity; idx < DstWorld.Entities.Length; idx++)
                {
                    NewEntity();
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
                PoolsSparse[setCmd.componentIndex].CpyToDst(setCmd.entity);

                PoolECBSetCommand.Del(entity);
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

        // TODO: Since every GetPool() in BufferWorld implicitly results in GetPool() from DstWorld, 
        // even internal ECB command pools are created inside DstWorld. 
        // There are only 4 of them and this is not critical, but it is better to fix it.

        protected override EcsPool<TComponent> CreatePool<TComponent>()
        {
            return new EcsPool<TComponent>(this, _poolsCount, Entities.Length, DstWorld.GetPool<TComponent>().GetItems());
        }

        public struct ECBSetCommand
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
        public static ref TComponent Set<TComponent>(this EcsBufferWorld buffer, int entity) where TComponent : struct
        {
            if (!buffer.Has<TComponent>(entity))
            {
                buffer.Add<TComponent>(entity);
            }
            buffer.PoolECBSetCommand.Add(entity) = new EcsBufferWorld.ECBSetCommand()
            {
                componentIndex = EcsComponent<TComponent>.TypeIndex,
                entity = entity
            };
            return ref buffer.GetPool<TComponent>().Add(entity);
        }

        public static void Del<TComponent>(this EcsBufferWorld buffer, int entity) where TComponent : struct
        {
            buffer.PoolECBDelCommand.Add(entity) = new EcsBufferWorld.ECBDelCommand()
            {
                componentIndex = EcsComponent<TComponent>.TypeIndex,
                entity = entity
            };
        }

        public static void DelBuffer<TElement>(this EcsBufferWorld buffer, int entity) where TElement : struct
        {
            buffer.PoolECBDelCommand.Add(entity) = new EcsBufferWorld.ECBDelCommand()
            {
                componentIndex = EcsComponent<Buffer<TElement>>.TypeIndex,
                entity = entity
            };
        }

        public static void DelEntity<TComponent>(this EcsBufferWorld buffer, int entity) where TComponent : struct
        {
            buffer.PoolECBDelEntityCommand.Add(entity) = new EcsBufferWorld.ECBDelEntityCommand()
            {
                entity = entity
            };
        }

        public static int NewEntity(this EcsBufferWorld buffer)
        {
            var entity = buffer.NewEntity();
            buffer.PoolECBNewEntityCommand.Add(entity);
            return entity;
        }
    }
}