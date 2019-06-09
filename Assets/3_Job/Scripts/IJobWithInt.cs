using System;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;

[JobProducerType(typeof(IJobWithIntExtensions.JobWithIntStruct<>))]
interface IJobWithInt
{
    void Execute(int value);
}

static class IJobWithIntExtensions
{
    internal struct JobDataWithInt<T>
    {
        public int value;
        public T jobData;
    }

    internal struct JobWithIntStruct<T> where T : struct, IJobWithInt
    {
        public static IntPtr jobReflectionData;

        public static IntPtr Initialize()
        {
            if (jobReflectionData == IntPtr.Zero)
                jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(JobDataWithInt<T>), JobType.Single, (ExecuteJobWithIndexFunction)Execute);
            return jobReflectionData;
        }

        public delegate void ExecuteJobWithIndexFunction(ref JobDataWithInt<T> jobDataWithInt, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
        public static void Execute(ref JobDataWithInt<T> jobDataWithInt, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
        {
            jobDataWithInt.jobData.Execute(jobDataWithInt.value);
        }
    }

    public unsafe static JobHandle ScheduleWithInt<T>(this T jobData, int value, JobHandle dependsOn = new JobHandle()) where T : struct, IJobWithInt
    {
        JobDataWithInt<T> jobDataWithInt = new JobDataWithInt<T>
        {
            value = value,
            jobData = jobData
        };
        var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobDataWithInt), JobWithIntStruct<T>.Initialize(), dependsOn, ScheduleMode.Batched);
        return JobsUtility.Schedule(ref scheduleParams);
    }

    public unsafe static void RunWithInt<T>(this T jobData, int value) where T : struct, IJobWithInt
    {
        JobDataWithInt<T> jobDataWithInt = new JobDataWithInt<T>
        {
            value = value,
            jobData = jobData
        };
        var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobDataWithInt), JobWithIntStruct<T>.Initialize(), new JobHandle(), ScheduleMode.Run);
        JobsUtility.Schedule(ref scheduleParams);
    }
}