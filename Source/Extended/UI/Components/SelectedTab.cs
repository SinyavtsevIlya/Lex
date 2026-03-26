using System;
using UnityEngine;

namespace Nanory.Lex
{
    public struct SelectedTab : IComponent, IDisposable
    {
        public MonoBehaviour Value;

        public void Dispose()
        {
            Value = null;
        }
    }
}