namespace Nanory.Lex
{
    [UpdateInGroup(typeof(WidgetsSystemGroup))]
    public abstract class WidgetSystemBase : UiSystemBase
    {
    }

    public abstract class UiSystemBase : EcsSystemBase
    {
        protected abstract void OnBind();
        protected abstract void OnUnbind();

        internal void Bind() => OnBind();

        internal void Unbind() => OnUnbind();
    }
}