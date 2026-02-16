using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nanory.Lex
{
    public struct BindEvent<TWidget> : IEmit where TWidget : MonoBehaviour
    {
        public TWidget Value;
    }

    public struct UnbindEvent<TWidget> : IEmit where TWidget : MonoBehaviour
    {
        public TWidget Value;
    }

    public struct CloseScreenEvent : IEmit
 {
    }

    public struct Replaceables : IComponent, IDisposable
 {
        public List<MonoBehaviour> Elements;
        public MonoBehaviour ActiveElement;
        public Action Deactivation;

        public Replaceables(int capacity)
        {
            Elements = new List<MonoBehaviour>(capacity);
            ActiveElement = null;
            Deactivation = null;
        }

        public void Dispose()
        {
            Elements = null;
            ActiveElement = null;
            Deactivation?.Invoke();
            Deactivation = null;
        }
    }

    public struct Screens : IComponent, IDisposable
 {
        public Replaceables Value;
        public void Dispose()
        {
            Value.Dispose();
        }
    }
    
    public struct ScreensAdded : IEmit {}
    
    public struct Tabs : IComponent, IDisposable
 {
        public Replaceables Value;
        
        public void Dispose()
        {
            Value.Dispose();
        }
    }
}