# Lex 
Lex - is a high performance c# framework based on **[LeoEcsLite](https://github.com/Leopotam/ecslite)**.

The main goal is to provide a more convenient API while maintaining the performance. And also add a number of features that simplify the development process.

> NOTE: The documentation is in progress

> NOTE: This framework is **almost** engine agnostic. Some **Unity** dependencies will be wrapped in defines, soon.

# Features workflow
**Features workflow** helps control which system will be created in which world without manually adding those systems to the world. Instead, the user determines which "Feature" the system belongs to. 

Here is a quick example:

1. First, let's define a user class, inherited from FeatureBase.

```c#
namespace Client.Some
{
    public class Feature : FeatureBase { }
}
```

> Note: the namespace is an important metadata that serves as a constraint for grouping user defined ecs-types by features.

2. And then we can create as many systems as we need. Let's create one: 

```c#
namespace Client.Some // note: the namespace is the same
{
    public class SomeSystem : EcsSystemBase 
    {
        public override void OnUpdate() 
        {
           // some code
        }
    }
}
```

3. In the end, we need to create a world, and determine what features will be included in this world:

```c#
class CoreStartup // can be a monobehavior or whatever you need.
{
    private EcsWorld _world;
    private EcsSystems _systems;
    private EcsSystemSorter _sorter;
    
    private Type[] FeatureTypes => new Type[]
    {
        // add a newly created feature...
        typeof(Some.Feature), 
        // ...and all other desired features
        typeof(Another.Feature), 
        typeof(OneMore.Feature)
    };

    private void Start()
    {
        _world = new EcsWorldBase(default, "Core");
        _systems = new EcsSystems(_world);

        // create a scanner - a special class that finds all user-defined ecs-data types.
        var scanner = new EcsTypesScanner();
        // pass necessary features to the scanner
        var systemTypes = scanner.ScanSystemTypes(FeatureTypes);
        // create a sorter - a special class that sorts systems in a hierarchical manner based on their order and groups.
        _sorter = new EcsSystemSorter(_world);
        // get systems and their system groups
        var featuredSystems = _sorter.GetSortedSystems(systemTypes);
        // add them to a "_systems" instance.
        _systems.Add(featuredSystems);

        _systems.Init();
    }

    private void Update()
    {
        _systems?.Run();
    }

    private void OnDestroy()
    {
        _systems?.Destroy();
        _systems = null;
        _world?.Destroy();
        _world = null;
        _sorter.Dispose();
    }
}
```
# Prefab entities
Prefab-entitiy is an entity that has a `Prefab` component assigned to it.
`Prefab` Component contains all pool indecies of all added components to this entity.
`Prefab` Component must be added first.
Prefab-entities doesn't match any `Filter`. 
World.Instantiate 

# Buffer
Buffer - is a pool-able collection type. Wraps a `System.Collections.Generic.List<T>` inside it.
Implements an automated pooling mechanism, to prevent allocations.
Can be used:
* As a component field
```c#
public struct SomeComponent 
{
    public Buffer Buffer;
}
```
* As a component itself
```csharp
   Add<Buffer<SomeComponent>>(entity);
```
If the buffer is used as a component field, then this component must implement an `IEcsAutoReset<T>`, and call Buffer's `AutoReset` inside it."
> **NOTE:** All `Buffer.Values` will be overwritten when the component is added to the entity.
# Conversion
# EntityCommandBuffers
# Lifecycle
# MonoComponents
# ProjectStructure
# Stats
# UI
# Timer
# TypeScan
# OneFrame
# VisualDebug

# Extended Ecs Types
### EcsSystemSorter
### EcsTypesScanner
### EcsSystemGroup
### UpdateInGroup and UpdateBefore attrributes
### SystemTypesProviders
