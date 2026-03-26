using UnityEngine;

namespace Nanory.Lex
{
    public struct UnbindEvent<TWidget> : IEmit where TWidget : MonoBehaviour
    {
        public TWidget Value;
    }
}