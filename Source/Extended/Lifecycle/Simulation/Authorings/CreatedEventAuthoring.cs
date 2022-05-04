using UnityEngine;
using Nanory.Lex;
using Nanory.Lex.Conversion;

namespace Nanory.Lex.Lifecycle
{
    public class CreatedEventAuthoring : AuthoringComponent
    {
        public override void Convert(int entity, ConvertToEntitySystem сonvertToEntitySystem)
        {
            var later = сonvertToEntitySystem.GetCommandBufferFrom<BeginSimulationECBSystem>();
            later.Add<CreatedEvent>(entity);
        }
    }
}
