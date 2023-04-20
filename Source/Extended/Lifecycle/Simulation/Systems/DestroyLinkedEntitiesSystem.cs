namespace Nanory.Lex.Lifecycle
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class DestroyLinkedEntitiesSystem : EcsSystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var destroyedEntity in Filter()
                         .With<DestroyedEvent>()
                         .With<LinkedEntities>()
                         .End())
            {
                TryDestroyLinkedEntities(this, destroyedEntity);
            }
        }

        private static void TryDestroyLinkedEntities(EcsSystemBase system, int entity)
        {
            if (system.TryGet<LinkedEntities>(entity, out var linkedEntities))
            {
                foreach (var linkedPackedEntity in linkedEntities.Buffer.Values)
                {
                    if (system.TryUnpack(linkedPackedEntity, out var linkedEntity))
                    {
                        system.GetOrAdd<DestroyedEvent>(linkedEntity);
                        TryDestroyLinkedEntities(system, linkedEntity);
                    } 
                }
            }
        }
    }
}