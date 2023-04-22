using UnityEngine;

namespace Nanory.Lex
{
    public struct InworldView : IEcsAutoReset<InworldView>
    {
        public GameObject Value;

        public void AutoReset(ref InworldView c)
        {
            c.Value = null;
        }
    }
}