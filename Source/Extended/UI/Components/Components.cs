using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nanory.Lex
{
    public struct BindEvent<TWidget> where TWidget : MonoBehaviour
    {
        public TWidget Value;
    }

    public struct UnbindEvent<TWidget> where TWidget : MonoBehaviour
    {
        public TWidget Value;
    }

    public struct OpenEvent<TScreen> where TScreen : MonoBehaviour
    {
        public TScreen Value;
    }

    public struct CloseEvent<TScreen> : IEcsAutoReset<CloseEvent<TScreen>> where TScreen : MonoBehaviour
    {
        public TScreen Value;

        public void AutoReset(ref CloseEvent<TScreen> c)
        {
            // NOTE: We implement IEcsAutoReset to 
            // prevent automatic cleaning the component 
            // data.
        }
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