namespace Nanory.Lex
{
    [UpdateInGroup(typeof(WidgetSystemGroup))]
    public abstract class WidgetSystemBase : EcsSystemBase
    {
        protected abstract void OnBind();
        protected abstract void OnUnbind();

        public void Bind() => OnBind();

        public void Unbind() => OnUnbind();
    }
}