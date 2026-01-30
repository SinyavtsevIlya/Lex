#if UNITY_EDITOR
#define MORPEH_DEBUG
#endif
#if !MORPEH_DEBUG
#define MORPEH_DEBUG_DISABLED
#endif

namespace Scellecs.Morpeh {
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Collections;
    using JetBrains.Annotations;
    using Unity.IL2CPP.CompilerServices;

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public sealed partial class World : IDisposable {
        [CanBeNull, PublicAPI]
        public static World Default {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => defaultWorld;
        }

        [CanBeNull]
        internal static World defaultWorld;

        [NotNull, PublicAPI]
        internal static World[] worlds = new World[WorldConstants.MAX_WORLDS_COUNT];

        [NotNull, PublicAPI]
        internal static int[] worldsIndices = new int[WorldConstants.MAX_WORLDS_COUNT];

        [NotNull, PublicAPI]
        internal static byte[] worldsGens = new byte[WorldConstants.MAX_WORLDS_COUNT];

        internal static int worldsCount = 0;
        
        [CanBeNull]
        internal static FastList<IWorldPlugin> plugins;

        internal int filterCount;

        [NotNull, PublicAPI]
        public FilterBuilder Filter => FilterBuilder.Create(this);
        
        [PublicAPI]
        public bool UpdateByUnity;
        
        [PublicAPI]
        public bool DoNotDisableSystemOnException;
        
        [PublicAPI]
        public bool IsDisposed;
#if MORPEH_BURST
        [PublicAPI]
        public Unity.Jobs.JobHandle JobHandle;
#endif
        internal LongHashMap<LongHashMap<Filter>> filtersLookup;

        internal IntStack freeFilterIDs;

        //todo custom collection
        internal SortedList<int, SystemsGroup> systemsGroups;
        
        //todo custom collection
        internal FastList<SystemsGroup> pluginSystemsGroups;

        //todo custom collection
        internal SortedList<int, SystemsGroup> newSystemsGroups;
        
        //todo custom collection
        internal FastList<SystemsGroup> newPluginSystemsGroups;

        internal EntityData[] entities;
        
        internal ushort[] entitiesGens;
        
        //real entities count
        internal int entitiesCount;
        //count + unused slots
        internal int entitiesLength;
        //all possible slots
        internal int entitiesCapacity;

        internal IntSparseSet dirtyEntities;
        internal IntSparseSet disposedEntities;

        internal IntStack freeEntityIDs;

        internal IStash[] stashes;

        internal ArchetypeStore archetypes;
        internal int archetypesCount;
        
        internal ArchetypePool archetypePool;
        
        internal Archetype[] emptyArchetypes;
        
        internal int emptyArchetypesCount;
        
        internal int identifier;

        internal int generation;
        
        internal int threadIdLock;
        
#if MORPEH_DEBUG
        internal int iteratorLevel;
#endif
        public Metrics metrics;
        internal Metrics newMetrics;
        
        internal ComponentsToFiltersRelation componentsFiltersWith;
        internal ComponentsToFiltersRelation componentsFiltersWithout;

        [PublicAPI]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static World Create() {
            if (worldsCount == WorldConstants.MAX_WORLDS_COUNT) {
#if MORPEH_DEBUG
                MLogger.LogError($"Can not create a world, as the number of worlds has reached the limit of {WorldConstants.MAX_WORLDS_COUNT}");
#endif
                return null;
            }

            return new World().Initialize();
        }

        private World() {
            this.threadIdLock = System.Threading.Thread.CurrentThread.ManagedThreadId;
            
            this.systemsGroups = new SortedList<int, SystemsGroup>();
            this.newSystemsGroups = new SortedList<int, SystemsGroup>();

            this.pluginSystemsGroups = new FastList<SystemsGroup>();
            this.newPluginSystemsGroups = new FastList<SystemsGroup>();

            this.filtersLookup = new LongHashMap<LongHashMap<Filter>>();
            this.freeFilterIDs = new IntStack();
        }

        [PublicAPI]
        public void Dispose() {
            if (this.IsDisposed) {
                return;
            }
            
            if (plugins != null) {
                foreach (var plugin in plugins) {
#if MORPEH_DEBUG
                    try {
#endif
                        plugin.Deinitialize(this);
#if MORPEH_DEBUG
                    }
                    catch (Exception e) {
                        MLogger.LogError($"Can not deinitialize world plugin {plugin.GetType()}");
                        MLogger.LogException(e);
                    }
#endif
                }
            }
            
            foreach (var systemsGroup in this.systemsGroups.Values) {
#if MORPEH_DEBUG
                try {
#endif
                    systemsGroup.Dispose();
#if MORPEH_DEBUG
                }
                catch (Exception e) {
                    MLogger.LogError($"Can not dispose system group {systemsGroup.GetType()}");
                    MLogger.LogException(e);
                }
#endif
            }

            this.newSystemsGroups.Clear();
            this.newSystemsGroups = null;
            
            this.systemsGroups.Clear();
            this.systemsGroups = null;
            
            foreach (var systemsGroup in this.pluginSystemsGroups) {
#if MORPEH_DEBUG
                try {
#endif
                    systemsGroup.Dispose();
#if MORPEH_DEBUG
                }
                catch (Exception e) {
                    MLogger.LogError($"Can not dispose plugin system group {systemsGroup.GetType()}");
                    MLogger.LogException(e);
                }
#endif
            }

            this.newPluginSystemsGroups.Clear();
            this.newPluginSystemsGroups = null;
            
            this.pluginSystemsGroups.Clear();
            this.pluginSystemsGroups = null;

            this.entities         = null;
            this.entitiesCount    = -1;
            this.entitiesLength   = -1;
            this.entitiesCapacity = -1;

            this.entitiesGens     = null;

            this.freeEntityIDs.Clear();
            this.freeEntityIDs = null;
            
            this.filterCount = 0;            
            this.filtersLookup = null;
            this.freeFilterIDs.Clear();

            foreach (var stash in this.stashes) {
#if MORPEH_DEBUG
                try {
#endif
                    stash?.Dispose();
#if MORPEH_DEBUG
                }
                catch (Exception e) {
                    MLogger.LogError($"Can not dispose stash with type id {stash.Type}");
                    MLogger.LogException(e);
                }
#endif
            }
            this.stashes = null;

            foreach (var archetype in this.archetypes) {
#if MORPEH_DEBUG
                try {
#endif
                    archetype.Dispose();
#if MORPEH_DEBUG
                }
                catch (Exception e) {
                    MLogger.LogError($"Cannot dispose archetype with hash {archetype.hash}");
                    MLogger.LogException(e);
                }
#endif
            }
            this.archetypes = null;
            
            this.emptyArchetypes      = null;
            this.emptyArchetypesCount = 0;
            
            this.archetypePool.Dispose();
            this.archetypePool = default;
            
            this.IsDisposed = true;

            this.ApplyRemoveWorld();
        }

        internal static void DisposeAllWorlds() {
            for (int i = worldsCount - 1; i >= 0; i--) {
                var world = worlds[i];
                if (!world.IsNullOrDisposed()) {
                    world.Dispose();
                }
            }
        }

        internal static void CleanupStatic() {
            DisposeAllWorlds();
            plugins?.Clear();
            worldsCount = 0;
            defaultWorld = null;
            Array.Clear(worldsGens, 0, WorldConstants.MAX_WORLDS_COUNT);
        }

        public struct Metrics {
            public int entities;
            public int archetypes;
            public int filters;
            public int systems;
            public int commits;
            public int migrations;
            public int stashResizes;
        }
    }
}
