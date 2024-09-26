using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Nanory.Lex.Conversion
{
    [StructLayout(LayoutKind.Sequential)]
    public class AuthoringEntity
    {
        public string Id;
        public List<AuthoringComponent> Components;

        public Action<string, string> IdChanged;
    }
}
