using System;
using UnityEngine;

namespace Nanory.Lex
{
    /// <summary>
    /// Provides a reference to the gameObject that
    /// represents a primary tangible view in the game world. 
    /// </summary>
    public struct InworldView : IComponent, IDisposable
 {
        public GameObject Value;

        public void Dispose()
        {
            Value = null;
        }
    }
}

namespace Nanory.Lex.View
{
    public struct InworldViewBindEvent : IComponent, IEmit
 {
    }

    public struct InworldViewUnbindEvent : IComponent, IEmit
 {
    }
}