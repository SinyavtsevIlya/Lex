using System;
using System.Collections.Generic;

namespace Nanory.Lex
{
    public interface IFeatureCollection
    {
        IEnumerable<Type> FeatureTypes { get; }
    }
}