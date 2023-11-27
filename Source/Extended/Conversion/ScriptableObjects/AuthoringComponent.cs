using System;

namespace Nanory.Lex.Conversion
{
    /// <summary>
    /// Base class for serializable representation
    /// of any user-defined component. Implementing <see cref="IConvertToEntity.Convert"/>
    /// is necessary to apply a desired changes to a passed entity.
    /// All Authoring components are normally stored in <see cref="AuthoringEntity"/>. 
    /// </summary>
    [Serializable]
    public abstract class AuthoringComponent : IConvertToEntity
    {
#if UNITY_EDITOR
        [UnityEngine.HideInInspector]
#endif
        public abstract void Convert(int entity, ConvertToEntitySystem convertToEntitySystem);
    }
}