#if MORPEH_BURST
using Nanory.Lex.Collections;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Nanory.Lex.Native {
    /// <summary>
    /// Reduced version to fit NativeStash into cache line size
    /// </summary>
    public struct NativeIntSlotMap {
        [NativeDisableUnsafePtrRestriction]
        public unsafe int* capacityMinusOnePtr;

        [NativeDisableUnsafePtrRestriction]
        public unsafe int* buckets;

        [NativeDisableUnsafePtrRestriction]
        public unsafe IntHashMapSlot* slots;
    }
}
#endif