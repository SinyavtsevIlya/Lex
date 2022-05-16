#if UNITY_EDITOR
using UnityEngine;


namespace Nanory.Lex.UnityEditorIntegration
{
    public struct EcsDebugGameobjectLink
    {
        public GameObject Value;
    }

    public static class EcsDebugExtensions
    {
        public static void LinkDebugGameobject(this EcsSystems ecsSystems, EcsWorld world, int entity, GameObject gameObject)
        {
            world.Add<EcsDebugGameobjectLink>(entity).Value = gameObject;
            foreach (var system in ecsSystems.AllSystems)
            {
                if (system is EcsWorldDebugSystem ecsWorldDebugSystem)
                {
                    gameObject.AddComponent<EcsDubugEntityLink>().Value = ecsWorldDebugSystem.GetEntityDebugView(entity);
                    break;
                }
            }
        }
    }
}
#endif
