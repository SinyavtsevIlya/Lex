using System;
using System.Collections.Generic;

namespace Nanory.Lex
{
    public abstract class SystemTypesProviderBase
    {
        public abstract IEnumerable<Type> GetSystemTypes(EcsTypesScanner scanner);
    }
}