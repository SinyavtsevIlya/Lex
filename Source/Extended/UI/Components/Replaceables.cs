using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nanory.Lex
{
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
}