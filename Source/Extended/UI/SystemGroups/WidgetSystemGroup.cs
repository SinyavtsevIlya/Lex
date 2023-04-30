using System.Collections.Generic;
using UnityEngine;

namespace Nanory.Lex
{
    [UpdateInGroup(typeof(UIPresentationSystemGroup))]
    public class WidgetSystemGroup : EcsSystemGroup
    {
        private const int MaxDepth = 25;

        private EcsSystems _systems;
        private List<WidgetSystemBase> _widgetSystems;
        
        private EntityCommandBufferSystem _beginBingingEcbSystem;
        private EntityCommandBufferSystem _beginUnbindingEcbSystem;
        
        private EntityCommandBufferSystem _endBindingEcbSystem;
        private EntityCommandBufferSystem _endUnbindingEcbSystem;

        private EntityCommandBuffer _lockBeginBindingBuffer;
        private EntityCommandBuffer _lockBeginUnbindingBuffer;
        private EntityCommandBuffer _lockEndBindingBuffer;
        private EntityCommandBuffer _lockEndUnbindingBuffer;

        protected override void OnCreate(EcsSystems systems)
        {
            base.OnCreate(systems);

            var world = systems.GetWorld();
            _systems = systems;
            _widgetSystems = new List<WidgetSystemBase>(_runSystems.Count);
            _lockBeginBindingBuffer = new EntityCommandBuffer(world);
            _lockBeginUnbindingBuffer = new EntityCommandBuffer(world);
            _lockEndBindingBuffer = new EntityCommandBuffer(world);
            _lockEndUnbindingBuffer = new EntityCommandBuffer(world);

            foreach (var runSystem in _runSystems)
            {
                if (runSystem is WidgetSystemBase widgetSystem)
                    _widgetSystems.Add(widgetSystem);
                
                if (runSystem is BeginWidgetEcbSystemGroup beginWidgetEcbSystemGroup)
                {
                    foreach (var system in beginWidgetEcbSystemGroup.Systems)
                    {
                        if (system is BeginWidgetBindingEcbSystem beginWidgetBindingEcbSystem)
                            _beginBingingEcbSystem = beginWidgetBindingEcbSystem;
                        if (system is BeginWidgetUnbindingEcbSystem beginWidgetUnbindingEcbSystem)
                            _beginUnbindingEcbSystem = beginWidgetUnbindingEcbSystem;
                    }

                    beginWidgetEcbSystemGroup.IsEnabled = false;
                }

                if (runSystem is EndWidgetEcbSystemGroup endWidgetEcbSystemGroup)
                {
                    foreach (var system in endWidgetEcbSystemGroup.Systems)
                    {
                        if (system is EndWidgetBindingEcbSystem endBindingEcbSystem)
                            _endBindingEcbSystem = endBindingEcbSystem;
                        if (system is EndWidgetUnbindingEcbSystem endUnbindingEcbSystem)
                            _endUnbindingEcbSystem = endUnbindingEcbSystem;
                    }

                    endWidgetEcbSystemGroup.IsEnabled = false;
                }
            }
        }

        protected override void OnUpdate(EcsSystems systems)
        {
            ResolveWidgetSystems(false, _endUnbindingEcbSystem, _beginUnbindingEcbSystem);
            ResolveWidgetSystems(true, _endBindingEcbSystem, _beginBingingEcbSystem);

            foreach (var runSystem in _runSystems) 
                runSystem.Run(systems);
        }

        private void ResolveWidgetSystems(bool bindPhase, EntityCommandBufferSystem endEcbSystem, EntityCommandBufferSystem beginEcbSystem)
        {
            var iterations = 0;
            
            do
            {
                beginEcbSystem.Run(_systems);
                ChangeLockState();
                
                foreach (var widgetSystem in _widgetSystems)
                {
                    if (!bindPhase)
                        widgetSystem.Unbind();
                    else
                        widgetSystem.Bind();
                }

                ChangeLockState();
                endEcbSystem.Run(_systems);
                ChangeLockState();

                if (++iterations > MaxDepth)
                {
                    Debug.LogError("Max iterations count was exceeded");
                    break;
                }
            } while (!endEcbSystem.GetBuffer().IsEmpty());
        }

        private void ChangeLockState()
        {
            ChangeLockState(_beginUnbindingEcbSystem, ref _lockBeginUnbindingBuffer);
            ChangeLockState(_beginBingingEcbSystem, ref _lockBeginBindingBuffer);
            ChangeLockState(_endUnbindingEcbSystem, ref _lockEndUnbindingBuffer);
            ChangeLockState(_endBindingEcbSystem, ref _lockEndBindingBuffer);
        }

        private void ChangeLockState(EntityCommandBufferSystem commandBufferSystem, ref EntityCommandBuffer buffer)
        {
            var tempBuffer = commandBufferSystem.GetBuffer();
            commandBufferSystem.SetBuffer(buffer);
            buffer = tempBuffer;
        }
    }
}
