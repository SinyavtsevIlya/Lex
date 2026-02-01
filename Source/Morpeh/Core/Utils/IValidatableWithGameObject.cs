namespace Nanory.Lex {
    using UnityEngine;
    
    public interface IValidatableWithGameObject {
        void OnValidate(GameObject gameObject);
    }
}