#if MORPEH_BURST
namespace Nanory.Lex.Native {
    using Unity.Collections.LowLevel.Unsafe;

    public struct NativeFastList<TNative> where TNative : unmanaged {
        [NativeDisableUnsafePtrRestriction]
        public unsafe TNative* data;
        
        [NativeDisableUnsafePtrRestriction]
        public unsafe int* lengthPtr;
        
        [NativeDisableUnsafePtrRestriction]
        public unsafe int* capacityPtr;
    }
}
#endif