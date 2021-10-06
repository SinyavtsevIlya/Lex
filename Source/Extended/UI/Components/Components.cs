using UnityEngine;
using System;
using System.Collections.Generic;

namespace Nanory.Lex
{
    public struct BindEvent<T> where T : MonoBehaviour
    {
        public T Value;
    }

    public struct UnbindEvent<T> where T : MonoBehaviour
    {
        public T Value;
    }

    public struct OpenEvent<T> where T : MonoBehaviour 
    {
        public T Value;
    }

    public struct CloseEvent<T> where T : MonoBehaviour 
    {
        public T Value;
    }

    public struct MonoScreen<TMonoComponent> where TMonoComponent : Component
    {
        public TMonoComponent Value;
    }

    public struct ScreensStorage : IEcsAutoReset<ScreensStorage>
    {
        public Dictionary<Type, MonoBehaviour> ScreenByType;
        public Dictionary<Type, int> ComponentIndexByType;
        public Dictionary<Type, int> OpenEventComponentIndexByType;
        public Dictionary<Type, int> CloseEventComponentIndexByType;
        public MonoBehaviour ActiveScreen;
        public MonoBehaviour PreviousScreen;

        public ScreensStorage(int capacity)
        {
            ScreenByType = new Dictionary<Type, MonoBehaviour>(capacity);
            ComponentIndexByType = new Dictionary<Type, int>(capacity);
            OpenEventComponentIndexByType = new Dictionary<Type, int>(capacity);
            CloseEventComponentIndexByType = new Dictionary<Type, int>(capacity);
            ActiveScreen = null;
            PreviousScreen = null;
        }

        public void AutoReset(ref ScreensStorage c)
        {
            c.ScreenByType = null;
            c.ComponentIndexByType = null;
            c.OpenEventComponentIndexByType = null;
            c.CloseEventComponentIndexByType = null;
            c.ActiveScreen = null;
            c.PreviousScreen = null;
        }
    }
}