#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace Nanory.Lex
{
    public static class EcsComponentsInfo
    {
        public static int Count;
    }

    public static class EcsComponent<T> where T : struct
    {
        public static int TypeIndex;

        static EcsComponent()
        {
            TypeIndex = EcsComponentsInfo.Count++;
        }
    }

    /// <summary>
    /// A generic event component that identifies 
    /// a change event that occurs on the TComponent.
    /// </summary>
    /// <typeparam name="TComponent"></typeparam>
    public struct Changed<TComponent> where TComponent : struct
    {

    }
}