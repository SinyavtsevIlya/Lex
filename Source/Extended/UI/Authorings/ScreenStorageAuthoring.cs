using UnityEngine;
using Nanory.Lex.Conversion;

namespace Nanory.Lex
{
    public class ScreenStorageAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField] private Transform _root;
        [SerializeField] private MonoBehaviour[] _screensPrefabs;
        [SerializeField] private Canvas _canvasPrefab;

        public void Convert(int ownerEntity, GameObjectConversionSystem converstionSystem)
        {
            var canvasInstance = Instantiate(_canvasPrefab, _root);
            MonoBehaviour[] screenInstances = new MonoBehaviour[_screensPrefabs.Length];
            for (int idx = 0; idx < _screensPrefabs.Length; idx++)
            {
                screenInstances[idx] = Instantiate(_screensPrefabs[idx], canvasInstance.transform);
                screenInstances[idx].gameObject.SetActive(false);
            }

            converstionSystem.World.Dst.InitializeScreenStorage(ownerEntity, screenInstances);
        }
    }
}
