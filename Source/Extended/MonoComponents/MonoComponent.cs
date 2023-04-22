using UnityEngine;

namespace Nanory.Lex
{
    public struct Mono<TMonoComponent> : IEcsAutoReset<Mono<TMonoComponent>> where TMonoComponent : Component
    {
        public TMonoComponent Value;

        public void AutoReset(ref Mono<TMonoComponent> c)
        {
            c.Value = null;
        }
    }
}