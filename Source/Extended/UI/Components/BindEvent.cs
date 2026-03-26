using UnityEngine;

namespace Nanory.Lex
{
    public struct BindEvent<TWidget> : IEmit where TWidget : MonoBehaviour
    {
        public TWidget Value;
    }
}