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
            var world = system.World;
            ref var screenStorage = ref world.Get<ScreensStorage>(ownerEntity);

            var hasActiveScreen = screenStorage.ActiveScreen != null;

            if (hasActiveScreen)
            {
                DeactivateScreen(ownerEntity, system, ref screenStorage, screenStorage.ActiveScreen.GetType());
            }
            ActivateScreen<TScreen>(ownerEntity, system, ref screenStorage);
        }

        public static TScreen GetScreen<TScreen>(this EcsSystemBase system, int ownerEntity) where TScreen : MonoBehaviour
        {
            var world = system.World;
            ref var screenStorage = ref world.Get<ScreensStorage>(ownerEntity);

            if (screenStorage.ScreenByType.TryGetValue(typeof(TScreen), out var screen))
            {
                return screen as TScreen;
            }
            else
            {
                throw new Exception($"No screen {typeof(TScreen).Name} is registered for entity-{ownerEntity}");
            }
        }
        #endregion

        #region Private
        private static void ActivateScreen<TScreen>(int ownerEntity, EcsSystemBase system, ref ScreensStorage screenStorage) where TScreen : MonoBehaviour
        {
            var world = system.World;
            var screen = system.GetScreen<TScreen>(ownerEntity);
            world.Add<MonoScreen<TScreen>>(ownerEntity).Value = screen;
            world.Add<OpenEvent<TScreen>>(ownerEntity).Value = screen;
            screenStorage.ActiveScreen = screen;
            screen.gameObject.SetActive(true);
            world.TryCacheComponentData(ownerEntity, screen);
            var later = system.GetCommandBufferFrom<EndPresentationEntityCommandBufferSystem>();
            later.Del<OpenEvent<TScreen>>(ownerEntity);
        }

        private static void DeactivateScreen(int ownerEntity, EcsSystemBase system, ref ScreensStorage screenStorage, Type screenType)
        {

            var world = system.World;
            var screen = screenStorage.ScreenByType[screenType];
            var closeEventComponentIndex = screenStorage.CloseEventComponentIndexByType[screenType];
            var screenComponentIndex = screenStorage.ComponentIndexByType[screenType];
            world.PoolsSparse[closeEventComponentIndex].Activate(ownerEntity);
            world.PoolsSparse[screenComponentIndex].Del(ownerEntity);
            screenStorage.PreviousScreen = screen;
            screen.gameObject.SetActive(false);
            var later = system.GetCommandBufferFrom<EndPresentationEntityCommandBufferSystem>();
            later.Del(ownerEntity, closeEventComponentIndex);
        }

        // There is no way to get Component Index without explicit Generic declaration.
        // And we don't want to enforce user to type screen types manually. 
        // Thats why we fill Component-Indeces map in a lazy manner.
        private static void TryCacheComponentData<TScreen>(this EcsWorld world, int ownerEntity, TScreen screen) where TScreen : MonoBehaviour
        {
            ref var screenStorage = ref world.Get<ScreensStorage>(ownerEntity);
            var areComponentIndecesRegistered = screenStorage.ComponentIndexByType.TryGetValue(typeof(TScreen), out var _);

            if (!areComponentIndecesRegistered)
            {
                screenStorage.ComponentIndexByType[typeof(TScreen)] = EcsComponent<MonoScreen<TScreen>>.TypeIndex;
                screenStorage.OpenEventComponentIndexByType[typeof(TScreen)] = EcsComponent<OpenEvent<TScreen>>.TypeIndex;
                screenStorage.CloseEventComponentIndexByType[typeof(TScreen)] = EcsComponent<CloseEvent<TScreen>>.TypeIndex;

                // We have to somehow pass a "screen" value into the CloseEvent component
                // and the easiest way is to add and immediately delete the component.
                // even when the component is removed it's value is still inside it, 
                // because we have overridden the AutoReset.
                world.Add<CloseEvent<TScreen>>(ownerEntity).Value = screen;
                world.Del<CloseEvent<TScreen>>(ownerEntity);
            }
        }
        #endregion
    }
}
