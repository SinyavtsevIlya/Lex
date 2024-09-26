using System;
using System.Runtime.InteropServices;

namespace Nanory.Lex.Conversion
{
    /// <summary>
    /// Base class for serializable representation
    /// of any user-defined component. 
    /// is necessary to apply a desired changes to a passed entity.
    /// All Authoring components are normally stored in <see cref="AuthoringEntity"/>. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public abstract class AuthoringComponent
    {
        public abstract void Convert(int entity, ConvertToEntitySystem convertToEntitySystem);
    }
}