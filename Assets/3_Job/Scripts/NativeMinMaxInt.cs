using static System.Threading.Interlocked;

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

enum MinMax
{
    Min,
    Max
}

[StructLayout(LayoutKind.Sequential)]
[NativeContainer]
unsafe struct NativeMinMaxInt
{
    MinMax m_Mode;

    [NativeDisableUnsafePtrRestriction]
    int* m_MinOrMax;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
    AtomicSafetyHandle m_Safety;

    [NativeSetClassTypeToNullOnSchedule]
    DisposeSentinel m_DisposeSentinel;
#endif

    Allocator m_AllocatorLabel;

    public NativeMinMaxInt(MinMax mode, Allocator allocatorLabel)
    {
        m_Mode = mode;
        m_AllocatorLabel = allocatorLabel;

        m_MinOrMax = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>(), 4, allocatorLabel);

        switch (mode)
        {
            case MinMax.Min:
                {
                    *m_MinOrMax = int.MaxValue;
                }
                break;
            case MinMax.Max:
                {
                    *m_MinOrMax = int.MinValue;
                }
                break;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, m_AllocatorLabel);
#endif
    }

    [WriteAccessRequired]
    public void SendCandidate(int candidate)
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

        switch (m_Mode)
        {
            case MinMax.Min:
                {
                    if (candidate < *m_MinOrMax)
                    {
                        *m_MinOrMax = candidate;
                    }
                }
                break;
            case MinMax.Max:
                {
                    if (candidate > *m_MinOrMax)
                    {
                        *m_MinOrMax = candidate;
                    }
                }
                break;
        }
    }

    public int Value
    {
        get
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return *m_MinOrMax;
        }
    }

    public bool IsCreated
    {
        get { return m_MinOrMax != null; }
    }

    [WriteAccessRequired]
    public void Dispose()
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

        UnsafeUtility.Free(m_MinOrMax, m_AllocatorLabel);
        m_MinOrMax = null;
    }

    [NativeContainer]
    [NativeContainerIsAtomicWriteOnly]
    unsafe internal struct Concurrent
    {
        MinMax m_Mode;

        [NativeDisableUnsafePtrRestriction]
        int* m_MinOrMax;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle m_Safety;
#endif

        public static implicit operator NativeMinMaxInt.Concurrent(NativeMinMaxInt nativeMinMaxInt)
        {
            NativeMinMaxInt.Concurrent concurrent;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(nativeMinMaxInt.m_Safety);
            concurrent.m_Safety = nativeMinMaxInt.m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref concurrent.m_Safety);
#endif

            concurrent.m_MinOrMax = nativeMinMaxInt.m_MinOrMax;
            concurrent.m_Mode = nativeMinMaxInt.m_Mode;
            return concurrent;
        }

        [WriteAccessRequired]
        public void SendCandidate(int candidate)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            int expectValue, realValue;

            switch (m_Mode)
            {
                case MinMax.Min:
                    {
                        do
                        {
                            expectValue = *m_MinOrMax;
                            realValue = expectValue;

                            if (candidate < expectValue)
                            {
                                realValue = CompareExchange(ref *m_MinOrMax, candidate, expectValue);
                            }
                        } while (expectValue != realValue);
                    }
                    break;
                case MinMax.Max:
                    {
                        do
                        {
                            expectValue = *m_MinOrMax;
                            realValue = expectValue;

                            if (candidate > expectValue)
                            {
                                realValue = CompareExchange(ref *m_MinOrMax, candidate, expectValue);
                            }
                        } while (expectValue != realValue);
                    }
                    break;
            }
        }
    }
}