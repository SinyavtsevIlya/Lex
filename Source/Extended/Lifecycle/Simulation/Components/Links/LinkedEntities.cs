namespace Nanory.Lex.Lifecycle
{
    public struct LinkedEntities : IEcsAutoReset<LinkedEntities>
    {
        public Buffer<EcsPackedEntity> Buffer;
        public void AutoReset(ref LinkedEntities c)
        {
            c.Buffer.AutoReset(ref c.Buffer);
        }
    }
}