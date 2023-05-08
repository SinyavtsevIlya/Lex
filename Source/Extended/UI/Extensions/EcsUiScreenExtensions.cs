using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nanory.Lex
{
    public static class EcsUiScreenExtensions
    {
        #region API
        public static void InitializeScreens(this EcsWorld world, int ownerEntity, IEnumerable<MonoBehaviour> screenInstances)
        {
            ref var screens = ref world.Add<Screens>(ownerEntity);
            screens.Value = new Replaceables(16);
            
            foreach (var screenInstance in screenInstances) 
                screens.Value.Elements.Add(screenInstance);
        }
        
        public static void OpenScreen<TScreen>(this EcsSystemBase system, int ownerEntity) where TScreen : MonoBehaviour
        {
            ref var screens = ref system.World.Get<Screens>(ownerEntity);
            var screen = system.GetScreen<TScreen>(ownerEntity);
            system.Replace(screen, ownerEntity, ref screens.Value);
        }

        public static TScreen GetScreen<TScreen>(this EcsSystemBase system, int ownerEntity) where TScreen : MonoBehaviour
        {
            ref var screens = ref system.World.Get<Screens>(ownerEntity);

            foreach (var screen in screens.Value.Elements)
            {
                if (screen is TScreen tScreen)
                    return tScreen;
            }

            throw new Exception($"No screen {typeof(TScreen).Name} is registered for entity-{ownerEntity}");
        }
        #endregion
    }
}
