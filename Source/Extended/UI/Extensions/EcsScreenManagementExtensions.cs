﻿using UnityEngine;
using System;
using System.Collections.Generic;

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
        public static void InitializeScreenStorage(this EcsSystemBase system, int ownerEntity, IEnumerable<MonoBehaviour> screens)
        {
            var world = system.World;
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
                DeactivateScreen(ownerEntity, system, screenStorage, screenStorage.ActiveScreen.GetType());
            }
            ActivateScreen<TScreen>(ownerEntity, system, screenStorage);
        } 
        #endregion

        #region Private
        private static void ActivateScreen<TScreen>(int ownerEntity, EcsSystemBase system, ScreensStorage screenStorage) where TScreen : MonoBehaviour
        {
            var world = system.World;
            var screen = world.GetScreen<TScreen>(ownerEntity); 
            world.Add<MonoScreen<TScreen>>(ownerEntity).Value = screen;
            world.Add<OpenEvent<TScreen>>(ownerEntity).Value = screen;
            screenStorage.ActiveScreen = screen;
            screen.gameObject.SetActive(true);
            world.TryRegisterComponentIndeces(ownerEntity, screen);
            var later = system.GetCommandBufferFrom<EndPresentationEntityCommandBufferSystem>();
            later.Del<OpenEvent<TScreen>>(ownerEntity);
        }

        private static void DeactivateScreen(int ownerEntity, EcsSystemBase system, ScreensStorage screenStorage, Type screenType)
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
        private static void TryRegisterComponentIndeces<TScreen>(this EcsWorld world, int ownerEntity, TScreen screen) where TScreen : MonoBehaviour
        {
            ref var screenStorage = ref world.Get<ScreensStorage>(ownerEntity);
            var areComponentIndecesRegistered = screenStorage.ComponentIndexByType.TryGetValue(typeof(TScreen), out var _);

            if (!areComponentIndecesRegistered)
            {
                screenStorage.ComponentIndexByType[typeof(TScreen)] = EcsComponent<MonoScreen<TScreen>>.TypeIndex;
                screenStorage.OpenEventComponentIndexByType[typeof(TScreen)] = EcsComponent<OpenEvent<TScreen>>.TypeIndex;
                screenStorage.CloseEventComponentIndexByType[typeof(TScreen)] = EcsComponent<CloseEvent<TScreen>>.TypeIndex;
            }
        }

        private static TScreen GetScreen<TScreen>(this EcsWorld world, int ownerEntity) where TScreen : MonoBehaviour
        {
            var screenStorage = world.Get<ScreensStorage>(ownerEntity);

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
    }
}
