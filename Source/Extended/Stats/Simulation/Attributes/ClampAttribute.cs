using System;

namespace Nanory.Lex.Stats
{
    public class ClampAttribute : Attribute
    {
        public Type ClampStatType;
        public ClampAttribute(Type clampStatType) => ClampStatType = clampStatType;
    }

    public interface IClamp<TMaxStat> where TMaxStat : struct, IStat
    {
    }
}
