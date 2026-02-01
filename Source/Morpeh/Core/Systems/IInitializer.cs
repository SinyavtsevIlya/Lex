namespace Nanory.Lex {
    using System;
    
    public interface IInitializer : IDisposable {
        World World { get; set; }

        void OnAwake();
    }
}