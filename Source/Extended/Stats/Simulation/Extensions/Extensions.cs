namespace Nanory.Lex.Stats
{
    public static class UnityEcsStatsExtensions
    {
        public static void SetStatsChanged(this EntityCommandBuffer ecb, int statContextEntity, int statReceiverEntity)
        {
            var world = ecb.DstWorld;

            if (world.TryGet<Stats>(statContextEntity, out var stats))
            {
                foreach (var stat in stats.Buffer.Values)
                {
                    if (stat.Unpack(world, out var statEntity)) 
                    {
                        ecb.Add<StatsChangedEvent>(statEntity);
                        ecb.AddOrSet<StatReceiverLink>(statEntity).Value = world.PackEntity(statReceiverEntity);
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