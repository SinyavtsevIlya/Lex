using UnityEngine;
using Nanory.Lex;
using Nanory.Lex.Conversion;

namespace Nanory.Lex.Lifecycle
{
    public class CreatedEventAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(int entity, GameObjectConversionSystem converstionSystem)
        {
            var later = converstionSystem.GetCommandBufferFrom<BeginSimulationECBSystem>();
            later.Add<CreatedEvent>(entity);
        }
    }
}
