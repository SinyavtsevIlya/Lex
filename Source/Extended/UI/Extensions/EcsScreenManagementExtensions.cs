using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nanory.Lex
{
    public static class EcsScreenManagementExtensions
    {
        #region API
        /// <summary>
        /// Use this to prepare an entity for having user-interface screens.
        /// </summary>
        /// <param name="system"></param>
        /// <param name="ownerEntity"></param>
        /// <param name="screens"></param>
        public static void InitializeScreenStorage(this EcsWorld world, int ownerEntity, IEnumerable<MonoBehaviour> screens)
        {
            ref var screenStorage = ref world.Add<ScreensStorage>(ownerEntity);
            screenStorage = new ScreensStorage(16);
            foreach (var screen in screens)
            {
                screenStorage.ScreenByType[screen.GetType()] = screen;
            }
        }

        /// <summary>
        /// Use this method to open a certain screen for the entity.
        /// Previous screen will be closed automatically.
        /// </summary>
        /// <typeparam name="TScreen"></typeparam>
        /// <param name="system"></param>
        /// <param name="ownerEntity"></param>
        public static void OpenScreen<TScreen>(this EcsSystemBase system, int ownerEntity) where TScreen : MonoBehaviour
        {
            ref var screenStorage = ref system.World.Get<ScreensStorage>(ownerEntity);
            screenStorage.Deactivation?.Invoke();
            var screen = system.GetScreen<TScreen>(ownerEntity);
            ActivateScreen<TScreen>(ownerEntity, system, ref screenStorage, screen);
        }

        public static TScreen GetScreen<TScreen>(this EcsSystemBase system, int ownerEntity) where TScreen : MonoBehaviour
        {
            var world = system.World;
            ref var screenStorage = ref world.Get<ScreensStorage>(ownerEntity);

            if (screenStorage.ScreenByType.TryGetValue(typeof(TScreen), out var screen))
            {
                return screen as TScreen;
            }

            throw new Exception($"No screen {typeof(TScreen).Name} is registered for entity-{ownerEntity}");
        }
        #endregion

        #region Private
        private static void ActivateScreen<TScreen>(int ownerEntity, EcsSystemBase system, ref ScreensStorage screenStorage, TScreen screen) where TScreen : MonoBehaviour
        {
            system.BindWidget(ownerEntity, screen);
            
            screenStorage.ActiveScreen = screen;
            screen.gameObject.SetActive(true);

            screenStorage.Deactivation = () =>
            {
                system.UnbindWidget(ownerEntity, screen);
                screen.gameObject.SetActive(false);
            };
        }
        #endregion
    }
}
