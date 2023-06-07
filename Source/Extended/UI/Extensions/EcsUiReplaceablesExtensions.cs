using UnityEngine;
using UnityEngine.UI;

namespace Nanory.Lex
{
    public static class EcsUiReplaceablesExtensions
    {
        public static void Replace<TReplaceable>(this EcsSystemBase system, TReplaceable replaceable, int ownerEntity,
            ref Replaceables replaceables) where TReplaceable : MonoBehaviour
        {
            system.BindWidget(ownerEntity, replaceable);
            
            replaceables.ActiveElement = replaceable;
            replaceable.GetComponent<Canvas>().enabled = true;

            replaceables.Deactivation?.Invoke();
            replaceables.Deactivation = () =>
            {
                system.UnbindWidget(ownerEntity, replaceable);
                replaceable.GetComponent<Canvas>().enabled = false;
            };
        }
    }
}