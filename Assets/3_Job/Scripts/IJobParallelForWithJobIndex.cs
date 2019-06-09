using System;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;

[JobProducerType(typeof(IJobParallelForWithJobIndexExtensions.ParallelForWithJobIndexJobStruct<>))]
interface IJobParallelForWithJobIndex
{
    void Execute(int index, int jobIndex);
}

static class IJobParallelForWithJobIndexExtensions
{
    internal struct ParallelForWithJobIndexJobStruct<T> where T : struct, IJobParallelForWithJobIndex
    {
        public static IntPtr jobReflectionData;

        public static IntPtr Initialize()
        {
            if (jobReflectionData == IntPtr.Zero)
                jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(T), JobType.ParallelFor, (ExecuteJobWithJobIndexFunction)Execute);
            return jobReflectionData;
        }

        public delegate void ExecuteJobWithJobIndexFunction(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
        public unsafe static void Execute(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
        {
            while (true)
            {
                int begin;
                int end;
                if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                {
                    return;
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), begin, end - begin);
#endif

                for (var i = begin; i < end; ++i)
                {
                    jobData.Execute(i, jobIndex);
                }
            }
        }
    }

    public unsafe static JobHandle Schedule<T>(this T jobData, int arrayLength, int innerloopBatchCount, JobHandle dependsOn = new JobHandle()) where T : struct, IJobParallelForWithJobIndex
    {
        var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), ParallelForWithJobIndexJobStruct<T>.Initialize(), dependsOn, ScheduleMode.Batched);
        return JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, innerloopBatchCount);
    }

    public unsafe static void Run<T>(this T jobData, int arrayLength) where T : struct, IJobParallelForWithJobIndex
    {
        var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), ParallelForWithJobIndexJobStruct<T>.Initialize(), new JobHandle(), ScheduleMode.Run);
        JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, arrayLength);
    }
}