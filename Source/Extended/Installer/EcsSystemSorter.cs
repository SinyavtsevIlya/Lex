using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nanory.Lex.Collections;
using Nanory.Lex.View;

namespace Nanory.Lex
{
    public class EcsSystemSorter : IDisposable
    {
        private World _world;
        
        private SystemsGroup _rootSystemGroup;

        public EcsSystemSorter(World world, Func<Type, ISystem> creator = null)
        {
            _world = world;
        }

        public SystemsGroup GetSortedSystems(IEnumerable<Type> systemTypes)
        {
            _rootSystemGroup = _world.CreateSystemsGroup();

            var reactions = new List<IReact>();

            foreach (var systemType in systemTypes)
            {
                var instance = Activator.CreateInstance(systemType);

                if (instance is ISystem system)
                {
                    _rootSystemGroup.AddSystem(system);
                }
                else if (instance is IInitializer initializer)
                {
                    _rootSystemGroup.AddInitializer(initializer);
                }

                if (instance is IReact reaction)
                {
                    reactions.Add(reaction);   
                }
            }
            
            SetupWorldReactions(reactions);
            
            SetupDisposableStashes();

            return _rootSystemGroup;
        }

        private void SetupWorldReactions(List<IReact> reactions)
        {
            var customSortings = new Dictionary<(Type systemType, Type emissionType), int>();

            foreach (var system in reactions)
            {
                var attr = system.GetType().GetCustomAttribute<ReactionOrderAttribute>();
                if (attr != null)
                    customSortings[(system.GetType(), attr.EmissionType)] = attr.Order;
            }

            var map = reactions
                .SelectMany(sys =>
                    sys.GetType()
                        .GetInterfaces()
                        .Where(i => i.IsGenericType &&
                                    i.GetGenericTypeDefinition() == typeof(IReact<>))
                        .Select(i => (Arg: i.GetGenericArguments()[0], Sys: sys)))
                .GroupBy(x => x.Arg)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Sys).Distinct().ToList()
                );

            _world.allReactions = new Dictionary<int, FastList<IReact>>();

            foreach (var reaction in map)
            {
                var emitIdType = typeof(EmitId<>).MakeGenericType(reaction.Key);
                var field = emitIdType.GetField("Id", BindingFlags.Public | BindingFlags.Static);
                var id = (int)field!.GetValue(null);

                var sortedReactions = reaction.Value
                    .OrderBy(r =>
                    {
                        var key = (r.GetType(), reaction.Key);
                        return customSortings.GetValueOrDefault(key, 0);
                    })
                    .ToList();

                var reactionsList = new FastList<IReact>();
                
                foreach (var react in sortedReactions) 
                    reactionsList.Add(react);

                _world.allReactions[id] = reactionsList;
            }
        }
        
        private void SetupDisposableStashes()
        {
            var worldType = typeof(WorldStashExtensions);
            var getStashMethod = worldType
                .GetMethods()
                .First(m => m.Name == "GetStash");

            var asDisposableMethod = typeof(StashExtensions)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .First(m => m.Name == "AsDisposable" && m.IsGenericMethod);

            var componentTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    t.IsValueType &&
                    !t.IsAbstract &&
                    !t.ContainsGenericParameters &&
                    typeof(IComponent).IsAssignableFrom(t) &&
                    typeof(IDisposable).IsAssignableFrom(t));


            foreach (var type in componentTypes)
            {
                var genericGetStash = getStashMethod.MakeGenericMethod(type);
                var stash = genericGetStash.Invoke(null, new object[]{ _world });

                var genericAsDisposable = asDisposableMethod.MakeGenericMethod(type);
                genericAsDisposable.Invoke(null, new[] { stash });
            }
        }

        public void Dispose()
        {
            _world = null;
            _rootSystemGroup = null;
        }
    }
}
