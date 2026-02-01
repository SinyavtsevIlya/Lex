using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace Nanory.Lex
{
    public static class EcsWorldExtensions
    {
        public static ref TComponent Add<TComponent>(this World world, Entity entity) where TComponent : struct, IComponent
        {
            return ref world.GetStash<TComponent>().Add(entity);
        }

        public static ref TComponent Get<TComponent>(this World world, Entity entity) where TComponent : struct, IComponent
        {
            return ref world.GetStash<TComponent>().Get(entity);
        }

        public static ref TComponent GetOrAdd<TComponent>(this World world, Entity entity) where TComponent : struct, IComponent
        {
            var stash = world.GetStash<TComponent>();
            
            if (stash.Has(entity))
            {
                return ref stash.Get(entity);
            }
            
            return ref stash.Add(entity);
        }

        public static bool Has<TComponent>(this World world, Entity entity) where TComponent : struct, IComponent
        {
            return world.GetStash<TComponent>().Has(entity);
        }

        public static bool TryGet<TComponent>(this World world, Entity entity, out TComponent component) where TComponent : struct, IComponent
        {
            if (world.GetStash<TComponent>().Has(entity))
            {
                component = world.GetStash<TComponent>().Get(entity);
                return true;
            }
            component = default;
            return false;
        }

        public static void Del<TComponent>(this World world, Entity entity) where TComponent : struct, IComponent
        {
            world.GetStash<TComponent>().Remove(entity);
        }
        
        public static void Emit<TEmission>(this World world, Entity entity, in TEmission emission = default) where TEmission : struct, IEmit
        {
        }

        public static void SetReactiveSystems(this World world, Dictionary<Type, List<IReact>> reactions)
        {
            
        }

        public static bool Unpack(this Entity inEntity, World world, out Entity entity)
        {
            entity = default;
            if (world.IsDisposed(inEntity))
                return false;
            
            entity = inEntity;
            return true;
        }
        
        public static Entity PackEntity(this World world, Entity inEntity)
        {
            return inEntity;
        }

        public static TSystem GetSystem<TSystem>(this World world) where TSystem : IInitializer
        {
            if (!world.systemsMap.TryGetValue(typeof(TSystem), out var system))
            {
                throw new Exception($"No system found of type {typeof(TSystem)}");
            }
            return (TSystem) system;
        }
    }
}