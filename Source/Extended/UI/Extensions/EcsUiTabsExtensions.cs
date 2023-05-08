using UnityEngine;

namespace Nanory.Lex
{
    public static class EcsUiTabsExtensions
    {
        public static void OpenTab<TTab>(this EcsSystemBase system, int ownerEntity, TTab tab) where TTab : MonoBehaviour
        {
            ref var tabs = ref system.World.Get<Tabs>(ownerEntity);
            system.Replace(tab, ownerEntity, ref tabs.Value);
        }
    }
}