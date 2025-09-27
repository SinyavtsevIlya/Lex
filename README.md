# Lex - The Entity Component System C# Framework
This framework based on **[LeoEcsLite](https://github.com/Leopotam/ecslite)**.

The main goal is to provide a more convenient API while maintaining the performance. And also add a number of features that simplify the development process.

## Requirements

`Min. Requirements:` Unity 2021.3.3f1

## Installation

You can install the repository using `UPM`:
Just add this line in Packages/manifest.json:

```
"com.nanory.lex": "https://github.com/SinyavtsevIlya/Lex.git",
```


# Quick overview

```c#
namespace Client.Battle
{
    public sealed class AttackSystem : EcsSystemBase
    {
        protected override void OnUpdate()
        {
            // filters can be builded in every OnUpdate call
            // (using caching local filters per system)
            foreach (var attackerEntity in Filter()
            .With<MelleeAttackRequest>()
            .With<AttackableLink>()
            .With<Attack>()
            .End())
            {
                // EcsSystemBase shortcut methods (Get, Add, TryGet, Has etc.) 
                ref var attackRequest = ref Get<MelleeAttackRequest>(attackerEntity);
                ref var attackableEntity = ref Get<AttackableLink>(attackerEntity).Value;

                if (attackableEntity.Unpack(World, out var targetEntity))
                {
                    var isBlocked = TryGet<Blocks>(targetEntity, out var blocks);

                    if (isBlocked)
                    {
                        blocks.Count--;
                        // Later - is a shortcut property for the "default" EntityCommandBuffer.
                        // Any other user-defined EntityCommandBuffer may be resolved using
                        // GetCommandBufferFrom<TSystem> method.
                        Later.Set<Blocks>(targetEntity) = blocks;

                        if (blocks.Count == 0)
                        {
                            Later.Del<Blocks>(targetEntity);
                        }
                    }
                    else
                    {
                        Later.AddOrSet<DamageEvent>(targetEntity) = new DamageEvent()
                        {
                            Source = World.PackEntity(attackerEntity),
                            Value = Get<Attack>(attackerEntity).Value
                        };
                    }
                }
            }            
        }
    }
}
```

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
class CoreStartup // can be a monobehavior or whatever you like.
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
This component must be added first.
Prefab-entities doesn't match any `Filter`. 
Any prefab-entity could be instantiated:
```c#
// create the exact copy of the prefabEntity
var instanceEntity = World.Instantiate(int prefabEntity);
```

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
   var buffer = Add<Buffer<SomeComponent>>(entity);
```
If the buffer is used as a component field, then this component must implement an `IEcsAutoReset<T>`, and call Buffer's `AutoReset` inside it."
> **NOTE:** All `Buffer.Values` will be overwritten when the component is added to the entity.
# Conversion
1. **Conversion** is a process of transformation runtime objects into the ECS world.
These objects can be anything (Poco objects, Scriptable objects, Gameobjects/monobehaviors etc.) that implements `IConvertToEntity`.


```c#
    public class AttackConversion : MonoBehaviour, IConvertToEntity
    {
        [SerializeField] int _value;

        public void Convert(int entity, ConvertToEntitySystem converstionSystem)
        {
            converstionSystem.World.Add<Attack>(entity).Value = _value;
        }
    }
```

> Note: note that the purpose of `IConvertToEntity` is to modify the passed entity, not to create a new one.

2. `ConvertToEntitySystem` has an API that instantly returns the converted entity:

* `ConvertAsInstansedEntity(IConvertToEntity convertToEntity)`
    1) creates a new entity 
    2) passes this entity to IConvertToEntity and returns it back.
* `ConvertOrGetAsUniqueEntity(IConvertToEntity convertToEntity)`
    1) gets the **primary** entity mapped to the convertToEntity-instance. 
    2) passes this entity to IConvertToEntity (n case of newly created entity) and returns it back.
* `ConvertOrGetAsPrefabEntity(IConvertToEntity convertToEntity)` 
    1) gets the **primary** entity mapped to the convertToEntity-instance 
    2) sets this entity as a prefab-entity 
    3) passes this entity to IConvertToEntity (n case of newly created entity) and returns it back.

There is also a scheduled version:

```c#
// ConversionMode: Instanced, Unique, Prefab
// creates a conversion request to be processed in the ConvertToEntitySystem
World.Convert(convertToEntity, conversionMode);
```

## Authoring-Entity and Authoring-Component (unity-only)

There is a buitin implementations of `IConvertToEntity` called **Authoring-Entity**.
It is a class derived from [ScriptableObject](https://docs.unity3d.com/Manual/class-ScriptableObject.html) that contains a list of **Authoring-Compoments**.

`AuthoringCompoment` is an abstract base class aslo implementing IConvertToEntity.
User can define as many Authoring Components as needed and then add them in the editor:

![image](https://user-images.githubusercontent.com/43283381/195467331-98f37999-569e-46e4-8085-9115cd694728.png)

## Base Authoring entities

Assigning a **base** athoring entity means that all components from this base entity will be also included in the conversion process.
These workflow reduces the memory consuption when working with a big dataset. And also make it easier for the designer to configure new entities. 

# EntityCommandBuffers
EntityCommandBuffer is a helper class for scheduling entity operations to be executed later (Playback).
You can create, record and playback it manually, but `Lex` by default has a predefined `EntityCommandBufferSystem`s working like syncpoints.

```c#
// resolve EntityCommandBuffer from user-defined system
var ecb = GetCommandBufferFrom<SomeEntityCommandBufferSystem>();
// record some ops
ecb.Add<SomeComponent>(entity).Value = 15;
ecb.Del<AnotherComponent>(entity);

// this is a shortcut proprty, returning a EntityCommandBuffer from the default predefined system.
Later.Add<SomeComponent>(entity).Value = 15;
```
# Lifecycle
# MonoComponents
# Stats
# UI
# Timer
# TypeScan
# OneFrame
# VisualDebug
# IL2CPP workaround
In case of `MakeGenericType` doesn't fully work with **IL2CPP** there is a `EcsTypesGeneratorBuildProcessor` that creates a `.cs` with a list of final ecs types `OnPreprocessBuild` phase. And deletes this file `OnPostprocessBuild` phase.
Thus, no action is required from the user.

# Extended Ecs Types
### EcsSystemSorter
### EcsTypesScanner
### EcsSystemGroup
### UpdateInGroup and UpdateBefore attrributes
### SystemTypesProviders
It would be not so fun to manually define all generic systems combinations to be created along with certain feature.
```c#
// for example:
var concreteTypes = new Type[] { typeof(MyGenericSystem<ComponentOne>, typeof(MyGenericSystem<ComponentTwo>, etc... )}
```
The good news, that there is a `SystemTypesProviderBase` class to inherit from for describing the resulting types:

```c#
public class StatsSystemTypesProvider : SystemTypesProviderBase
{
    public override IEnumerable<Type> GetSystemTypes(EcsTypesScanner scanner)
    {
       return scanner.
                GetComponentTypes().Where(t => typeof(IStat).IsAssignableFrom(t))
                .Select(statType => typeof(CalculateStatSystem<>).MakeGenericType(statType)).ToList();
    }
}
```

# Best practices

### Features separation (dll)
### Application layers (Simulation < Presentaion > View)
### Events and requests separation
### Order independent workflow
### Tech stack purification
### Context by default
### Naming guidline
### Abandonment of dependencies
### Project structuring

# Demo projects

### [Gemscapes - match3 rpg](https://github.com/SinyavtsevIlya/Gemscapes)
