using UnityEngine;

namespace Nanory.Lex
{
    /// <summary>
    /// Provides a reference to the gameObject that
    /// represents a primary tangible view in the game world. 
    /// </summary>
    public struct InworldView : IEcsAutoReset<InworldView>
    {
        public GameObject Value;

        public void AutoReset(ref InworldView c)
        {
            c.Value = null;
        }
    }
}