using UnityEngine;
using Nanory.Lex.Conversion;

namespace Nanory.Lex.Stats
{
    public class StatReceiverTagAuthoring : AuthoringComponent
    {
        public override void Convert(int entity, ConvertToEntitySystem сonvertToEntitySystem)
        {
            сonvertToEntitySystem.World.Add<StatReceiverTag>(entity);
        }
    }

    public abstract class AdditiveStatAuthoringBase<TStatComponent> : AuthoringComponent
        where TStatComponent : struct, IStat
    {
        [SerializeField] private int _value;

        public override void Convert(int statContextEntity, ConvertToEntitySystem сonvertToEntitySystem)
        {
            var world = сonvertToEntitySystem.World;
            var statEntity = StatsAuthorizingHelpers.AuthorizeStatEntity<TStatComponent>(statContextEntity, world.Dst, _value);
            world.Add<AdditiveStatTag>(statEntity);
        }
    }

    public abstract class MultiplyStatAuthoringBase<TStatComponent> : AuthoringComponent
    where TStatComponent : struct, IStat
    {
        [SerializeField] private int _percent;

        public override void Convert(int entity, ConvertToEntitySystem сonvertToEntitySystem)
        {
            var world = сonvertToEntitySystem.World;
            var statEntity = StatsAuthorizingHelpers.AuthorizeStatEntity<TStatComponent>(entity, world.Dst, _percent);
            world.Add<MultiplyStatTag>(statEntity);
        }
    }

    public static class StatsAuthorizingHelpers
    {
        public static int AuthorizeStatEntity<TStatComponent>(int statContextEntity, EcsWorld world, int value)
            where TStatComponent : struct, IStat
        {
            var stat = new TStatComponent();
            stat.StatValue = value;

            var statEntity = world.NewEntity();
            
            world.Add<TStatComponent>(statEntity) = stat;
            world.Add<TStatComponent>(statContextEntity) = stat;

            if (!world.TryGet<Stats>(statContextEntity, out var stats))
            {
                stats = new Stats();
                stats.Buffer.Values = Buffer<EcsPackedEntity>.Pool.Pop();
                world.Add<Stats>(statContextEntity) = stats;
            }

            stats.Buffer.Values.Add(world.PackEntity(statEntity));

            if (world.Has<StatReceiverTag>(statContextEntity))
            {
                world.Add<StatReceiverLink>(statEntity).Value = world.PackEntity(statContextEntity);
            }

            return statEntity;
        }
    }
}
