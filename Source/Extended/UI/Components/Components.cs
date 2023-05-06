using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nanory.Lex
{
    public struct BindEvent<TWidget> where TWidget : MonoBehaviour
    {
        public TWidget Value;
    }

    public struct UnbindEvent<TWidget> : IEcsAutoReset<UnbindEvent<TWidget>> where TWidget : MonoBehaviour
    {
        public TWidget Value;
        
        // NOTE: REIMPLEMENT ASAP
        public void AutoReset(ref UnbindEvent<TWidget> c)
        {
            // NOTE: We implement IEcsAutoReset to 
            // prevent automatic cleaning the component 
            // data.
        }
    }

    public struct ScreensStorage : IEcsAutoReset<ScreensStorage>
    {
        public Dictionary<Type, MonoBehaviour> ScreenByType;
        public Dictionary<Type, int> ComponentIndexByType;
        public Dictionary<Type, int> BindEventComponentIndexByType;
        public Dictionary<Type, int> UnbindEventComponentIndexByType;
        public MonoBehaviour ActiveScreen;
        public MonoBehaviour PreviousScreen;

        public ScreensStorage(int capacity)
        {
            ScreenByType = new Dictionary<Type, MonoBehaviour>(capacity);
            ComponentIndexByType = new Dictionary<Type, int>(capacity);
            BindEventComponentIndexByType = new Dictionary<Type, int>(capacity);
            UnbindEventComponentIndexByType = new Dictionary<Type, int>(capacity);
            ActiveScreen = null;
            PreviousScreen = null;
        }

        public void AutoReset(ref ScreensStorage c)
        {
            c.ScreenByType = null;
            c.ComponentIndexByType = null;
            c.BindEventComponentIndexByType = null;
            c.UnbindEventComponentIndexByType = null;
            c.ActiveScreen = null;
            c.PreviousScreen = null;
        }
    }
}