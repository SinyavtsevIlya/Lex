using Nanory.Lex.Conversion;

namespace Nanory.Lex.Lifecycle
{
    public class CreatedEventAuthoring : AuthoringComponent
    {
        public override void Convert(int entity, ConvertToEntitySystem convertToEntitySystem)
        {
            var later = convertToEntitySystem.GetCommandBufferFrom<BeginSimulationECBSystem>();
            later.Add<CreatedEvent>(entity);
        }
    }
}
