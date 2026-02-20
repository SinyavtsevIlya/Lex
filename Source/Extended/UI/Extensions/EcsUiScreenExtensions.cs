using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nanory.Lex
{
    public static class EcsUiScreenExtensions
    {
        public static void InitializeScreens(this World world, Entity ownerEntity, IEnumerable<MonoBehaviour> screenInstances)
        {
            ref var screens = ref world.Add<Screens>(ownerEntity);
            screens.Value = new Replaceables(16);
            
            foreach (var screenInstance in screenInstances) 
                screens.Value.Elements.Add(screenInstance);
            
            world.Emit<ScreensAdded>(ownerEntity);
        }
        
        public static void OpenScreen<TScreen>(this EcsSystemBase system, Entity ownerEntity) where TScreen : MonoBehaviour
        {
            ref var screens = ref system.World.GetStash<Screens>().Get(ownerEntity);
            var screen = system.GetScreen<TScreen>(ownerEntity);
            system.Replace(screen, ownerEntity, ref screens.Value);
        }

        public static TScreen GetScreen<TScreen>(this EcsSystemBase system, Entity ownerEntity) where TScreen : MonoBehaviour
        {
            ref var screens = ref system.World.Get<Screens>(ownerEntity);

            foreach (var screen in screens.Value.Elements)
            {
                if (screen is TScreen tScreen)
                    return tScreen;
            }

            throw new Exception($"No screen {typeof(TScreen).Name} is registered for entity-{ownerEntity}");
        }
        
        public static void CloseScreen(this EcsSystemBase system, Entity ownerEntity)
        {
            system.Emit<CloseScreenEvent>(ownerEntity);
        }
    }
}
