﻿#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace Nanory.Lecs
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
}