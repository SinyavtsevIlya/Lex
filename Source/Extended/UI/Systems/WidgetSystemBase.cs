namespace Nanory.Lex
{
    [UpdateInGroup(typeof(WidgetsSystemGroup))]
    public abstract class WidgetSystemBase : UiSystemBase
    {
    }

    public abstract class UiSystemBase : EcsSystemBase
    {
        protected EntityCommandBuffer BeginUiEcb { get; private set; }
        protected EntityCommandBuffer EndUiEcb { get; private set; }
        
        protected override void OnCreate()
        {
            BeginUiEcb = World.GetCommandBufferFrom<BeginUiBindingEcbSystem>();
            EndUiEcb = World.GetCommandBufferFrom<EndUiUnbindingEcbSystem>();
        }

        protected abstract void OnBind();
        protected abstract void OnUnbind();

        internal void Bind() => OnBind();
        internal void Unbind() => OnUnbind();
    }
}