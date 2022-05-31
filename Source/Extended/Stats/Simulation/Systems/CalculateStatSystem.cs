using System.Collections.Generic;

namespace Nanory.Lex.Stats
{
    [UpdateInGroup(typeof(StatSystemGroup))]
    public class CalculateStatSystem<TStatComponent> : EcsSystemBase
        where TStatComponent : struct, IStat
    {
        private EcsFilter _additiveStats;
        private EcsFilter _multiplyStats;

        private List<int> _eventsCache;

        protected override void OnCreate()
        {
            _additiveStats = Filter()
                .With<TStatComponent>()
                .With<StatReceiverLink>()
                .With<AdditiveStatTag>()
                .End();

            _multiplyStats = Filter()
                .With<TStatComponent>()
                .With<StatReceiverLink>()
                .With<MultiplyStatTag>()
                .End();

            _eventsCache = new List<int>(32);
        }

        protected override void OnUpdate()
        {
            CleanupStatReceivingEvents();

            #region TODO
            // 1) Можно ускорить. Вместо перебора всего фильтра
            // шерстить список Stats для стат-ресивера нужно
            // держать два списка: AdditiveStats и Mutliply-
            // -Stats чтобы проходить по ним отсортировано.

            // 2) После просчета результирующего стата, можно 
            // проверять, если ли на этой сущности StatReceiver-
            // -Link и в этом случае запускать перепросчет уже 
            // для этого линка. Таким образом будут рекурсивно 
            // обработы все Stat зависимости. Например:
            // На героя одет меч, но меч сломан и он дает 25% 
            // атаки. Получается граф: поломка -> меч -> игрок.
             
            // 3) Сделать отдельный метод
            // SetStatApplyed (statContext, statReceiver) и
            // SetStatChanged (statContext) 
            // Тогда во втором случае не нужно явно указывать
            // получателя.

            #endregion

            foreach (var changedStatEntity in Filter()
                .With<TStatComponent>()
                .With<StatsChangedEvent>()
                .With<StatReceiverLink>()
                .End())
            {
                ref var statReceiverLink = ref Get<StatReceiverLink>(changedStatEntity);
                if (statReceiverLink.Value.Unpack(World, out var statReceiverEntity))
                {
                    TryAddStatToReceiver(changedStatEntity, statReceiverEntity);
                    Calculate(statReceiverEntity);

                    if (TryGet<StatReceiverLink>(statReceiverEntity, out var nextStatReceiverLink))
                    {
                        if (nextStatReceiverLink.Value.Unpack(World, out var nextStatReceiverEntity))
                        {
                            Later.SetStatsChanged(statReceiverEntity, nextStatReceiverEntity);
                        }
                    }
                }
            }

            foreach (var removedStatEntity in Filter()
                .With<TStatComponent>()
                .With<StatsRemovedEvent>()
                .With<StatReceiverLink>()
                .End())
            {
                ref var statReceiverLink = ref Get<StatReceiverLink>(removedStatEntity);
                if (statReceiverLink.Value.Unpack(World, out var statReceiverEntity))
                {
                    Get<Stats>(statReceiverEntity).Buffer.Values.Remove(World.PackEntity(removedStatEntity));
                    Calculate(statReceiverEntity);

                    if (TryGet<StatReceiverLink>(statReceiverEntity, out var nextStatReceiverLink))
                    {
                        if (nextStatReceiverLink.Value.Unpack(World, out var nextStatReceiverEntity))
                        {
                            Later.SetStatsRemoved(statReceiverEntity);
                        }
                    }
                }
            }
        }

        private void TryAddStatToReceiver(int changedStatEntity, int statReceiverEntity)
        {
            var stats = Get<Stats>(statReceiverEntity).Buffer.Values;
            foreach (var e in stats)
            {
                if (e.Unpack(World, out var statE))
                {
                    if (statE == changedStatEntity)
                        return;
                }
            }
            stats.Add(World.PackEntity(changedStatEntity));
        }

        private void CleanupStatReceivingEvents()
        {
            foreach (var eventEntity in Filter()
            .With<StatReceivedEvent<TStatComponent>>()
            .End())
            {
                _eventsCache.Add(eventEntity);
            }

            if (_eventsCache.Count > 0)
            {
                foreach (var e in _eventsCache)
                {
                    Later.Del<StatReceivedEvent<TStatComponent>>(e);
                }
                _eventsCache.Clear();
            }
        }

        private void Calculate(int statReceiverEntity)
        {
            var totalStatValue = 0;

            foreach (var additiveStatEntity in _additiveStats)
            {
                ref var currentStatReceiverLink = ref Get<StatReceiverLink>(additiveStatEntity);
                if (currentStatReceiverLink.Value.Unpack(World, out var currentStatReceiverEntity))
                {
                    if (currentStatReceiverEntity == statReceiverEntity)
                    {
                        ref var stat = ref Get<TStatComponent>(additiveStatEntity);
                        totalStatValue += stat.StatValue;
                    }
                }
            }

            var totalMultiplierPercent = 100;

            foreach (var multiplyStatEntity in _multiplyStats)
            {
                ref var currentStatReceiverLink = ref Get<StatReceiverLink>(multiplyStatEntity);
                if (currentStatReceiverLink.Value.Unpack(World, out var currentStatReceiverEntity))
                {
                    if (currentStatReceiverEntity == statReceiverEntity)
                    {
                        ref var stat = ref Get<TStatComponent>(multiplyStatEntity);
                        totalMultiplierPercent += stat.StatValue;
                    }
                }
            }

            ref var receiverStat = ref Get<TStatComponent>(statReceiverEntity);
            receiverStat.StatValue = totalStatValue * totalMultiplierPercent / 100;

            Later.Add<StatReceivedEvent<TStatComponent>>(statReceiverEntity);
        }
    }
}
