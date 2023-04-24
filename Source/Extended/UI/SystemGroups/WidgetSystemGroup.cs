using System.Collections.Generic;
using UnityEngine;

namespace Nanory.Lex
{
    [UpdateInGroup(typeof(UIPresentationSystemGroup))]
    public class WidgetSystemGroup : EcsSystemGroup
    {
        private const int MaxDepth = 25;
        
        private List<WidgetSystemBase> _widgetSystems;
        private EcsFilter _dirtyFilter;
        private EcsPool<BindingDirtyTag> _dirtyPool;
        private EntityCommandBufferSystem _beginSyncPointSystem;
        private EntityCommandBufferSystem _endSyncPointCreationSystem;
        private EntityCommandBufferSystem _endSyncPointDestructionSystem;

        private EntityCommandBuffer _lockBeginBuffer;
        private EntityCommandBuffer _lockEndCreationBuffer;
        private EntityCommandBuffer _lockEndDestructionBuffer;

        protected override void OnCreate(EcsSystems systems)
        {
            base.OnCreate(systems);

            var world = systems.GetWorld();
            _dirtyFilter = world.Filter<BindingDirtyTag>().End();
            _dirtyPool = world.GetPool<BindingDirtyTag>();
            _widgetSystems = new List<WidgetSystemBase>(_runSystems.Count);
            _lockBeginBuffer = new EntityCommandBuffer(world);
            _lockEndCreationBuffer = new EntityCommandBuffer(world);
            _lockEndDestructionBuffer = new EntityCommandBuffer(world);

            foreach (var runSystem in _runSystems)
            {
                if (runSystem is WidgetSystemBase widgetSystem)
                    _widgetSystems.Add(widgetSystem);

                if (runSystem is BeginWidgetEntityCommandBufferSystem beginWidgetEntityCommandBufferSystem)
                    _beginSyncPointSystem = beginWidgetEntityCommandBufferSystem;

                if (runSystem is EndWidgetEntityCommandBuffersSystemGroup entityCommandBuffersSystemGroup)
                {
                    foreach (var system in entityCommandBuffersSystemGroup.Systems)
                    {
                        if (system is EndWidgetCreationEntityCommandBufferSystem creationSystem)
                            _endSyncPointCreationSystem = creationSystem;
                        if (system is EndWidgetDestructionEntityCommandBufferSystem destructionSystem)
                            _endSyncPointDestructionSystem = destructionSystem;
                    }
                }
            }
        }

        protected override void OnUpdate(EcsSystems systems)
        {
            var isDirty = false;
            
            var iterations = 0;
            
            do
            {
                _beginSyncPointSystem.Run(systems);
                ChangeLockState();
                foreach (var widgetSystem in _widgetSystems)
                {
                    widgetSystem.Unbind();
                    widgetSystem.Bind();
                }
                ChangeLockState();
                _endSyncPointDestructionSystem.Run(systems);
                _endSyncPointCreationSystem.Run(systems);
                ChangeLockState();
                
                isDirty = _dirtyFilter.GetEntitiesCount() > 0;
                
                foreach (var dirtyEntity in _dirtyFilter)
                    _dirtyPool.Del(dirtyEntity);

                if (++iterations > MaxDepth)
                {
                    Debug.LogError("Max iterations count was exceeded");
                    break;
                }
                
            } while (isDirty);
            
            Debug.Log($"Finished bindign phase in {iterations} iterations");

            for (var systemIdx = 1; systemIdx < _runSystems.Count - 1; systemIdx++)
            {
                _runSystems[systemIdx].Run(systems);
            }
        }

        private void ChangeLockState()
        {
            ChangeLockState(_beginSyncPointSystem, ref _lockBeginBuffer);
            ChangeLockState(_endSyncPointDestructionSystem, ref _lockEndDestructionBuffer);
            ChangeLockState(_endSyncPointCreationSystem, ref _lockEndCreationBuffer);
        }

        private void ChangeLockState(EntityCommandBufferSystem commandBufferSystem, ref EntityCommandBuffer buffer)
        {
            var tempBuffer = commandBufferSystem.GetBuffer();
            commandBufferSystem.SetBuffer(buffer);
            buffer = tempBuffer;
        }
    }
}
