namespace Nanory.Lex.Lifecycle
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class DestroyLinkedEntitiesSystem : EcsSystemBase, IReact<DestroyedEvent>
    {
        public void React(DestroyedEvent _, Entity entity)
        {
            if (!Has<LinkedEntities>(entity))
                return;
            
            TryDestroyLinkedEntities(this, entity);

        }

        private static void TryDestroyLinkedEntities(EcsSystemBase system, Entity entity)
        {
            if (!system.TryGet<LinkedEntities>(entity, out var linkedEntities))
                return;
            
            foreach (var linkedPackedEntity in linkedEntities.Buffer)
            {
                if (!system.TryUnpack(linkedPackedEntity, out var linkedEntity)) 
                    continue;
                
                system.Emit<DestroyRequest>(linkedEntity);
                TryDestroyLinkedEntities(system, linkedEntity);
            }
        }
    }
}