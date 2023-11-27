using System.Threading.Tasks;

namespace Nanory.Lex.Conversion
{
    public interface IConversionStrategy
    {
        Task Convert(AuthoringEntity authoringEntity, int entity, ConvertToEntitySystem system);
    }

    public class DefaultConversionStrategy : IConversionStrategy
    {
        public Task Convert(AuthoringEntity authoringEntity, int entity, ConvertToEntitySystem system)
        {
            foreach (var component in authoringEntity.Components) 
                component.Convert(entity, system);
            
            return Task.CompletedTask;
        }

        public static readonly DefaultConversionStrategy Value = new();
    }
}