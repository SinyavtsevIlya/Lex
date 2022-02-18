using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nanory.Lex.Stats
{
    public class CalculateStatSystem<TStatComponent> : IEcsRunSystem, IEcsInitSystem 
        where TStatComponent : struct, IStat
    {
        private EcsFilter _changedStats;
        private EcsFilter _removedStats;
        private EcsFilter _additiveStats;
        private EcsFilter _multiplyStats;

        public void Init(EcsSystems systems)
        {
            var world = systems.GetWorld();

            //_changedStats = world.Filter<TStatComponent>()
            //    .With<StatsChangedEvent>()
            //    .With<StatReceiverLink>()
            //    .End();

            //_removedStats = GetEntityQuery(
            //    ComponentType.ReadWrite<TStatComponent>(),
            //    ComponentType.ReadOnly<StatsRemovedEvent>(),
            //    ComponentType.ReadOnly<StatReceiverLink>());

            //_additiveStats = GetEntityQuery(
            //    ComponentType.ReadOnly<TStatComponent>(),
            //    ComponentType.ReadOnly<StatReceiverLink>(),
            //    ComponentType.ReadOnly<AdditiveStatTag>());

            //_multiplyStats = GetEntityQuery(
            //    ComponentType.ReadOnly<TStatComponent>(),
            //    ComponentType.ReadOnly<StatReceiverLink>(),
            //    ComponentType.ReadOnly<MultiplyStatTag>());
        }

        public void Run(EcsSystems systems)
        {
            //var changedStatsEntities = _changedStats.ToEntityArray(Allocator.TempJob);

            //for (var idx = 0; idx < changedStatsEntities.Length; idx++)
            //{
            //    var statReceiver = EntityManager.GetSharedComponentData<StatReceiverLink>(changedStatsEntities[idx]);
            //    Calculate(statReceiver);
            //}

            //changedStatsEntities.Dispose();

            //var removedStatsEntities = _removedStats.ToEntityArray(Allocator.TempJob);

            //for (var idx = 0; idx < removedStatsEntities.Length; idx++)
            //{
            //    var statEntity = removedStatsEntities[idx];
            //    var statReceiver = EntityManager.GetSharedComponentData<StatReceiverLink>(statEntity);
            //    EntityManager.SetSharedComponentData(statEntity, new StatReceiverLink() { Value = Entity.Null });
            //    Calculate(statReceiver);
            //}

            //removedStatsEntities.Dispose();
        }

        //private void Calculate(StatReceiverLink statReceiver)
        //{
        //    var buffer = new EntityCommandBuffer(Allocator.Temp);

        //    var receiverStat = GetComponent<TStatComponent>(statReceiver.Value);

        //    _additiveStats.SetSharedComponentFilter(statReceiver);
        //    _multiplyStats.SetSharedComponentFilter(statReceiver);

        //    var totalStatValue = 0f;

        //    var childrenStatEntites = _additiveStats.ToEntityArray(Allocator.TempJob);

        //    for (var statIdx = 0; statIdx < childrenStatEntites.Length; statIdx++)
        //    {
        //        var childStatEntity = childrenStatEntites[statIdx];
        //        var stat = GetComponent<TStatComponent>(childStatEntity);
        //        ref var value = ref InterpretUnsafeUtility.Retrieve<TStatComponent, float>(ref stat);
        //        totalStatValue += value;
        //    }
        //    childrenStatEntites.Dispose();

        //    var multiplyChildrenStatEntities = _multiplyStats.ToEntityArray(Allocator.TempJob);

        //    for (var statIdx = 0; statIdx < multiplyChildrenStatEntities.Length; statIdx++)
        //    {
        //        var childStatEntity = multiplyChildrenStatEntities[statIdx];
        //        var stat = GetComponent<TStatComponent>(childStatEntity);
        //        ref var value = ref InterpretUnsafeUtility.Retrieve<TStatComponent, float>(ref stat);
        //        totalStatValue *= value;
        //    }
        //    multiplyChildrenStatEntities.Dispose();

        //    ref var receiverStatValue = ref InterpretUnsafeUtility.Retrieve<TStatComponent, float>(ref receiverStat);
        //    receiverStatValue = totalStatValue;

        //    buffer.AppendToBuffer(statReceiver.Value, new StatRecievedElementEvent()
        //    {
        //        StatType = ComponentType.ReadOnly<TStatComponent>()
        //    });

        //    SetComponent(statReceiver.Value, receiverStat);

        //    buffer.Playback(EntityManager);
        //}
    }
}
