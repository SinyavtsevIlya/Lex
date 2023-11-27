namespace Nanory.Lex.Conversion
{
    public static class ConversionExtensions
    {
        public static void Convert(this EcsWorld world, IConvertToEntity convertToEntity, ConversionMode conversionMode)
        {
            ref var requestEntity = ref world.Add<ConvertRequest>(world.NewEntity());
            requestEntity.Value = convertToEntity;
            requestEntity.Mode = conversionMode;
        }
    }
}