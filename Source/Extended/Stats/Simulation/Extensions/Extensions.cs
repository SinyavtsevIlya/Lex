namespace Nanory.Lex.Stats
{
    public static class UnityEcsStatsExtensions
    {
        /// <summary>
        /// Applies all Stats of stat-context entity to <see cref="StatReceiverTag">Stat-Receiver entity</see> 
        /// </summary>
        /// <param name="ecb"></param>
        /// <param name="statContextEntity"></param>
        /// <param name="statReceiverEntity"></param>
        public static void SetStatApplied(this EntityCommandBuffer ecb, int statContextEntity, int statReceiverEntity)
        {
            var world = ecb.DstWorld;

            if (world.TryGet<Stats>(statContextEntity, out var stats))
            {
                foreach (var stat in stats.Buffer.Values)
                {
                    if (stat.Unpack(world, out var statEntity)) 
                    {
                        ecb.Add<StatsChangedEvent>(statEntity);
                        ecb.Add<StatReceiverLink>(statEntity).Value = world.PackEntity(statReceiverEntity);
                    }
                }
            } 
        }

        /// <summary>
        /// Recalculates stats of <see cref="StatReceiverTag">Stat-Receiver entity</see> that 
        /// depends on a given Stat-Context entity
        /// </summary>
        /// <param name="ecb"></param>
        /// <param name="statContextEntity"></param>
        public static void SetStatsChanged(this EntityCommandBuffer ecb, int statContextEntity)
        {
            var world = ecb.DstWorld;

            if (world.TryGet<Stats>(statContextEntity, out var stats))
            {
                foreach (var stat in stats.Buffer.Values)
                {
                    if (stat.Unpack(world, out var statEntity))
                    {
                        ecb.Add<StatsChangedEvent>(statEntity);
                    }
                }
            }
        }

        public static void SetStatsRemoved(this EntityCommandBuffer ecb, int statContextEntity)
        {
            var world = ecb.DstWorld;

            if (world.TryGet<Stats>(statContextEntity, out var stats))
            {
                foreach (var stat in stats.Buffer.Values)
                {
                    if (stat.Unpack(world, out var statEntity))
                    {
                        ecb.Add<StatsRemovedEvent>(statEntity);
                    }
                }
            }
        }
    }
}