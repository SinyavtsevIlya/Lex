using System;

namespace Nanory.Lecs
{
    public class EcsInstallerBase
    {
        private readonly EcsWorld _world;
        private readonly EcsSystems _systems;

        public EcsInstallerBase(EcsWorld world, EcsSystems systems)
        {
            _world = world;
            _systems = systems;
        }

        public void Install()
        {
        }
    }
}
