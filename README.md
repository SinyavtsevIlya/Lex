# Lex 
Lex - is a high performance c# framework based on **[LeoEcsLite](https://github.com/Leopotam/ecslite)**.

The main goal is to provide a more convenient API while maintaining the performance. And also add a number of features that simplify the development process.

> NOTE: The documentation is in progress

> NOTE: This framework is `almost` engine agnostic. Some `Unity` dependencies will be wrapped in defines, soon.

# Extended Ecs Types

## EcsWorldBase
In `Lex` the world contains a collection of systems. 
> NOTE: Everything that works for leo-ecs lite is working for `Lex`. If you want some system to have several worlds passed via ctor you surely can.

## EcsSystemBase
Systems can refer to the world they belong to.
```csharp
public class SomeSystem : EcsSystemBase 
{
    public override void OnCreate() 
    {
        World.NewEntity();
    }
}
```
> NOTE: World doesn't control the system's initialization and update order.

# Features pipeline
Features pipeline - helps you to keep control over systems per world creation, and don't care about their instantiation at the same time.

## Startup
Startup - is an entry point for the world.
Thats how your custom `startup` may look:
```c#
namespace Client
{
    class CoreStartup : MonoBehaviour
    {
        private EcsWorld _world;
        private EcsSystems _systems;
        private EcsSystemSorter _sorter;

        protected virtual Type[] FeatureTypes => new Type[]
        {
            typeof(Nanory.Lex.Lifecycle.Feature),
            typeof(Core.Feature),
            typeof(Combat.Feature),
            typeof(Movement.Feature),
            typeof(Inventory.Feature)
        };

        private void Start()
        {
            _world = new EcsWorldBase(default, "Core");
            _systems = new EcsSystems(_world);

            var scanner = new EcsTypesScanner();
            var systemTypes = scanner.ScanSystemTypes(FeatureTypes);
            _sorter = new EcsSystemSorter(_world);
            
            var featuredSystems = _sorter.GetSortedSystems(systemTypes);
            _systems.Add(featuredSystems);

            _systems.Init();
        }

        private void Update()
        {
            _systems?.Run();
        }

        private void OnDestroy()
        {
            if (_systems != null)
            {
                _systems.Destroy();
                _systems = null;
            }
            if (_world != null)
            {
                _world.Destroy();
                _world = null;
            }
            _sorter.Dispose();
        }
    }
}
```
## FeatureBase
## EcsSystemSorter
## EcsTypesScanner
## EcsSystemGroup
## UpdateInGroup and UpdateBefore
## SystemTypesProviders

# Prefabs
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
