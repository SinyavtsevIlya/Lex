using UnityEngine;

namespace Nanory.Lex
{
    public struct Mono<TMonoComponent> where TMonoComponent : Component
    {
        public TMonoComponent Value;
    }
}