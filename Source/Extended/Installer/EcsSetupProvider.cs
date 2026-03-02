namespace Nanory.Lex
{
    public static class EcsSetupProvider
    {
        public static IEcsSetup GetSetup(IFeatureCollection featureCollection)
        {
            if (GeneratedEcsSetupLookup.Values.TryGetValue(featureCollection.GetType(), out var ecsSetup))
                return ecsSetup;

            return new ReflectionEcsSetup(featureCollection);
        }
    }
}