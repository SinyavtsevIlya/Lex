using System;
using System.Collections.Generic;

namespace Nanory.Lex
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class UIPresentationSystemGroup : EcsSystemGroup 
    {
        private EntityCommandBuffer _widgetsCommandBuffer;

        protected override void OnCreate(EcsSystems systems)
        {
            base.OnCreate(systems);

            _widgetsCommandBuffer = CacheEntityCommandBuffer(systems.AllSystems);
        }

        private EntityCommandBuffer CacheEntityCommandBuffer(List<IEcsSystem> systems)
        {
            foreach (var system in systems)
            {
                if (system is EcsSystemGroup systemGroup)
                {
                    var result = CacheEntityCommandBuffer(systemGroup.Systems);
                    if (result != null)
                    {
                        return result;
                    }
                }
                else if (system is EcsSystemBase systemBase)
                {
                    return systemBase.GetCommandBufferFrom<WidgetEntityCommandBufferSystem>();
                }
            }
            return null;
        }

        protected override void OnUpdate(EcsSystems systems)
        {
            foreach (var system in _runSystems)
            {
                system.Run(systems);
                _widgetsCommandBuffer.Playback();
            }
        }
    }

    public static class UISystemTypesRegistry
    {
        public static Type[] Values = new Type[]
        {
            typeof(WidgetEntityCommandBufferSystem),
            typeof(EndPresentationEntityCommandBufferSystem),
            typeof(ScreenSystemGroup),
            typeof(WidgetSystemGroup),
            typeof(PrimaryWidgetSystemGroup),
            typeof(SecondaryWidgetSystemGroup),
        };
    }
}
