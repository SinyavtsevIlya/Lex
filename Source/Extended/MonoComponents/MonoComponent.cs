using System;
using UnityEngine;

namespace Nanory.Lex
{
    public struct Mono<TMonoComponent> : IComponent, IDisposable where TMonoComponent : Component
    {
        public TMonoComponent Value;

        public void Dispose()
        {
            Value = null;
        }
    }
}