using UnityEngine;

namespace Nanory.Lex
{
    public static class EcsUiReplaceablesExtensions
    {
        public static void Replace<TReplaceable>(this EcsSystemBase system, TReplaceable replaceable, int ownerEntity,
            ref Replaceables replaceables) where TReplaceable : MonoBehaviour
        {
            system.BindWidget(ownerEntity, replaceable);
            
            replaceables.ActiveElement = replaceable;
            replaceable.gameObject.SetActive(true);

            replaceables.Deactivation?.Invoke();
            replaceables.Deactivation = () =>
            {
                system.UnbindWidget(ownerEntity, replaceable);
                replaceable.gameObject.SetActive(false);
            };
        }
    }
}