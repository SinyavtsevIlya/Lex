#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;


namespace Nanory.Lex
{
    public struct EcsDebugGameobjectLink
    {
        public GameObject Value;
    }

    public static class EcsDebugExtensions
    {
        public static void LinkDebugGameobject(this EcsWorld world, int entity, GameObject gameObject)
        {
            world.Add<EcsDebugGameobjectLink>(entity).Value = gameObject;
        }
    }
}
#endif
