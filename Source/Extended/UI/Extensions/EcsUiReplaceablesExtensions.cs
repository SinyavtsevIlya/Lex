using UnityEngine;
using UnityEngine.UI;

namespace Nanory.Lex
{
    public static class EcsUiReplaceablesExtensions
    {
        public static void Replace<TReplaceable>(this EcsSystemBase system, TReplaceable replaceable, int ownerEntity,
            ref Replaceables replaceables) where TReplaceable : MonoBehaviour
        {
            replaceables.Deactivation?.Invoke();
            
            system.BindWidget(ownerEntity, replaceable);
            
            replaceables.ActiveElement = replaceable;
            replaceable.GetComponent<Canvas>().enabled = true;
            
            replaceables.Deactivation = () =>
            {
                if (!system.World.IsAlive())
                    return;

                if (replaceable == null)
                {
                    Debug.LogError("Unable to unbind widget. Replaceable is destroyed");
                    return;
                }
                
                system.UnbindWidget(ownerEntity, replaceable);
                replaceable.GetComponent<Canvas>().enabled = false;
            };
        }
    }
}