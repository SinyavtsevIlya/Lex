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

    public struct Replaceables : IEcsAutoReset<Replaceables>
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

        public void AutoReset(ref Replaceables c)
        {
            c.Elements = null;
            c.ActiveElement = null;
            c.Deactivation?.Invoke();
            c.Deactivation = null;
        }
    }

    public struct Screens : IEcsAutoReset<Screens>
    {
        public Replaceables Value;
        public void AutoReset(ref Screens c)
        {
            c.Value.AutoReset(ref c.Value);
        }
    }
    
    public struct Tabs : IEcsAutoReset<Tabs>
    {
        public Replaceables Value;
        
        public void AutoReset(ref Tabs c)
        {
            c.Value.AutoReset(ref c.Value);
        }
    }
}