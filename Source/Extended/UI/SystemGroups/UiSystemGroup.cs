using System.Collections.Generic;
using UnityEngine;

namespace Nanory.Lex
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class UiSystemGroup : EcsSystemGroup
    {
        private const int MaxDepth = 25;

        private EcsSystems _systems;
        private List<UiSystemBase> _uiSystems;
        
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
            _uiSystems = new List<UiSystemBase>(_runSystems.Count);
            _lockBeginBindingBuffer = new EntityCommandBuffer(world);
            _lockBeginUnbindingBuffer = new EntityCommandBuffer(world);
            _lockEndBindingBuffer = new EntityCommandBuffer(world);
            _lockEndUnbindingBuffer = new EntityCommandBuffer(world);

            foreach (var runSystem in _runSystems)
            {
                if (runSystem is ScreenSystemGroup or WidgetsSystemGroup)
                {
                    foreach (var system in (runSystem as EcsSystemGroup).Systems)
                    {
                        if (system is UiSystemBase uiSystem)
                            _uiSystems.Add(uiSystem);
                    }
                }

                if (runSystem is BeginUiEcbSystemGroup beginUiEcbSystemGroup)
                {
                    foreach (var system in beginUiEcbSystemGroup.Systems)
                    {
                        if (system is BeginUiBindingEcbSystem beginUiBindingEcbSystem)
                            _beginBingingEcbSystem = beginUiBindingEcbSystem;
                        if (system is BeginUiUnbindingEcbSystem beginUiUnbindingEcbSystem)
                            _beginUnbindingEcbSystem = beginUiUnbindingEcbSystem;
                    }

                    beginUiEcbSystemGroup.IsEnabled = false;
                }

                if (runSystem is EndUiEcbSystemGroup endUiEcbSystemGroup)
                {
                    foreach (var system in endUiEcbSystemGroup.Systems)
                    {
                        if (system is EndUiBindingEcbSystem endBindingEcbSystem)
                            _endBindingEcbSystem = endBindingEcbSystem;
                        if (system is EndUiUnbindingEcbSystem endUnbindingEcbSystem)
                            _endUnbindingEcbSystem = endUnbindingEcbSystem;
                    }

                    endUiEcbSystemGroup.IsEnabled = false;
                }
            }
        }

        protected override void OnUpdate(EcsSystems systems)
        {
            foreach (var runSystem in _runSystems) 
                runSystem.Run(systems);
            
            ResolveUiSystems(false, _endUnbindingEcbSystem, _beginUnbindingEcbSystem);
            ResolveUiSystems(true, _endBindingEcbSystem, _beginBingingEcbSystem);
        }

        private void ResolveUiSystems(bool bindPhase, EntityCommandBufferSystem endEcbSystem, EntityCommandBufferSystem beginEcbSystem)
        {
            var iterations = 0;
            
            do
            {
                beginEcbSystem.Run(_systems);
                ChangeLockState();
                
                foreach (var uiSystem in _uiSystems)
                {
                    if (!bindPhase)
                        uiSystem.Unbind();
                    else
                        uiSystem.Bind();
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
