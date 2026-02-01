namespace Nanory.Lex {
    public interface IWorldPlugin {
        void Initialize(World world);
        void Deinitialize(World world);
    }
}