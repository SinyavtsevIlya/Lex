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

    public struct ScreensStorage : IEcsAutoReset<ScreensStorage>
    {
        public Dictionary<Type, MonoBehaviour> ScreenByType;
        public MonoBehaviour ActiveScreen;
        public Action Deactivation;

        public ScreensStorage(int capacity)
        {
            ScreenByType = new Dictionary<Type, MonoBehaviour>(capacity);
            ActiveScreen = null;
            Deactivation = null;
        }

        public void AutoReset(ref ScreensStorage c)
        {
            c.ScreenByType = null;
            c.ActiveScreen = null;
        }
    }
}