namespace Nanory.Lex
{
    [UpdateInGroup(typeof(WidgetsSystemGroup))]
    public abstract class WidgetSystemBase : UiSystemBase
    {
    }

    public abstract class UiSystemBase : EcsSystemBase
    {
        protected EntityCommandBuffer BeginUiEcb => World.GetCommandBufferFrom<BeginUiBindingEcbSystem>();
        protected EntityCommandBuffer EndUiEcb => World.GetCommandBufferFrom<EndUiUnbindingEcbSystem>();
        
        protected override void OnCreate()
        {
        }

        protected abstract void OnBind();
        protected abstract void OnUnbind();

        internal void Bind() => OnBind();
        internal void Unbind() => OnUnbind();
    }
}