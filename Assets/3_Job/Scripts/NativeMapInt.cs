using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

[StructLayout(LayoutKind.Sequential)]
[NativeContainer]
[NativeContainerSupportsDeallocateOnJobCompletion]
unsafe struct NativeMapInt
{
    int m_Length;
    int m_DefaultValue;

    [NativeDisableUnsafePtrRestriction]
    int* m_Buffer;

    [NativeDisableUnsafePtrRestriction]
    int* m_Map;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
    AtomicSafetyHandle m_Safety;

    [NativeSetClassTypeToNullOnSchedule]
    DisposeSentinel m_DisposeSentinel;
#endif

    Allocator m_AllocatorLabel;

    public NativeMapInt(int length, int defaultValue, Allocator allocator)
    {
        m_Length = length;
        m_DefaultValue = defaultValue;
        m_AllocatorLabel = allocator;

        m_Buffer = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>() * length, 4, m_AllocatorLabel);
        m_Map = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>() * length, 4, m_AllocatorLabel);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, m_AllocatorLabel);
#endif
    }

    [WriteAccessRequired]
    public void Set(int index, int from, int to)
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

        if (index < m_Length)
        {
            m_Buffer[index] = from;
            m_Map[index] = to;
        }
    }

    public int Get(int from)
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif

        int value = m_DefaultValue;

        for (int i = 0; i < m_Length; i++)
        {
            if (from == UnsafeUtility.ReadArrayElement<int>(m_Buffer, i))
            {
                value = UnsafeUtility.ReadArrayElement<int>(m_Map, i);
                break;
            }
        }

        return value;
    }

    public void Dispose()
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

        UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);
        UnsafeUtility.Free(m_Map, m_AllocatorLabel);
        m_Buffer = null;
        m_Map = null;
    }

    [NativeContainer]
    [NativeContainerIsReadOnly]
    unsafe internal struct Concurrent
    {
        int m_Length;
        int m_DefaultValue;

        [NativeDisableUnsafePtrRestriction]
        int* m_Buffer;

        [NativeDisableUnsafePtrRestriction]
        int* m_Map;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle m_Safety;
#endif

        public static implicit operator NativeMapInt.Concurrent(NativeMapInt nativeMapInt)
        {
            NativeMapInt.Concurrent concurrent;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(nativeMapInt.m_Safety);
            concurrent.m_Safety = nativeMapInt.m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref concurrent.m_Safety);
#endif

            concurrent.m_Buffer = nativeMapInt.m_Buffer;
            concurrent.m_DefaultValue = nativeMapInt.m_DefaultValue;
            concurrent.m_Length = nativeMapInt.m_Length;
            concurrent.m_Map = nativeMapInt.m_Map;
            return concurrent;
        }

        public int Get(int from)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif

            int value = m_DefaultValue;

            for (int i = 0; i < m_Length; i++)
            {
                if (from == UnsafeUtility.ReadArrayElement<int>(m_Buffer, i))
                {
                    value = UnsafeUtility.ReadArrayElement<int>(m_Map, i);
                    break;
                }
            }

            return value;
        }
    }
}