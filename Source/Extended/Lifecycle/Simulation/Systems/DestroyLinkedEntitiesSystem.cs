namespace Nanory.Lex.Lifecycle
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class DestroyLinkedEntitiesSystem : EcsSystemBase, IReact<DestroyedEvent, LinkedEntities>
    {
        public void React(DestroyedEvent _, Entity entity)
        {
            TryDestroyLinkedEntities(this, entity);
        }

        private void TryDestroyLinkedEntities(EcsSystemBase system, Entity entity)
        {
            foreach (var linkedPackedEntity in Get<LinkedEntities>(entity).Buffer)
            {
                if (!system.TryUnpack(linkedPackedEntity, out var linkedEntity)) 
                    continue;
                
                system.Emit<DestroyRequest>(linkedEntity);
                TryDestroyLinkedEntities(system, linkedEntity);
            }
        }
    }
}