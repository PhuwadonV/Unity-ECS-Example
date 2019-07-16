using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Burst;

enum CompareType
{
    Equal,
    NotEqual,
    MoreThan,
    MoreThanOrEqual,
    LessThan,
    LessThanOrEqual
}

[BurstCompile]
struct AddAllJob : IJob
{
    public float adder;
    // ReadWrite
    public NativeArray<float4> values;

    public void Execute()
    {
        for (int i = 0; i < values.Length; i++)
        {
            values[i] += adder;
        }
    }
}

[BurstCompile]
struct AddJob : IJob
{
    [ReadOnly]
    public NativeArray<float4> a;
    [ReadOnly]
    public NativeArray<float4> b;
    [WriteOnly]
    public NativeArray<float4> result;

    public void Execute()
    {
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = a[i] + b[i];
        }
    }
}

[BurstCompile]
struct AddAllParallelForJob : IJobParallelFor
{
    public float adder;
    // ReadWrite
    public NativeArray<float4> values;

    public void Execute(int index)
    {
        values[index] += adder;
    }
}

[BurstCompile]
struct AddParallelForJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float4> a;
    [ReadOnly]
    public NativeArray<float4> b;
    [WriteOnly]
    public NativeArray<float4> result;

    public void Execute(int index)
    {
        result[index] = a[index] + b[index];
    }
}

[BurstCompile]
struct HeavyJob : IJobParallelFor
{
    public uint weight;
    // ReadWrite
    public NativeArray<float> values;

    public void Execute(int index)
    {
        for (int i = 0; i <= weight; i++)
        {
            values[index] += 1;
        }

        values[index] -= weight;
    }
}

[BurstCompile]
struct FilterParallelForJob : IJobParallelFor
{
    [ReadOnly]
    public CompareType compareType;
    [ReadOnly]
    public float compareValue;
    [ReadOnly]
    public NativeArray<float> src;
    [WriteOnly]
    public NativeQueue<float>.Concurrent results;

    public void Execute(int index)
    {
        float value = src[index];
        switch (compareType)
        {
            case CompareType.Equal:
                {
                    if (value == compareValue)
                    {
                        results.Enqueue(value);
                    }
                }
                break;
            case CompareType.NotEqual:
                {
                    if (value != compareValue)
                    {
                        results.Enqueue(value);
                    }
                }
                break;
            case CompareType.MoreThan:
                {
                    if (value > compareValue)
                    {
                        results.Enqueue(value);
                    }
                }
                break;
            case CompareType.MoreThanOrEqual:
                {
                    if (value >= compareValue)
                    {
                        results.Enqueue(value);
                    }
                }
                break;
            case CompareType.LessThan:
                {
                    if (value < compareValue)
                    {
                        results.Enqueue(value);
                    }
                }
                break;
            case CompareType.LessThanOrEqual:
                {
                    if (value <= compareValue)
                    {
                        results.Enqueue(value);
                    }
                }
                break;
        }
    }
}

[BurstCompile]
struct MinMaxIntParallelForJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<int> candidates;

    [WriteOnly]
    public NativeMinMaxInt.Concurrent result;

    public void Execute(int index)
    {
        result.SendCandidate(candidates[index]);
    }
}

[BurstCompile]
struct MapIntParallelForJob : IJobParallelFor
{
    [ReadOnly]
    [DeallocateOnJobCompletion]
    public NativeArray<int> from;

    [ReadOnly]
    [DeallocateOnJobCompletion]
    public NativeMapInt map;

    [WriteOnly]
    public NativeArray<int> to;

    public void Execute(int index)
    {
        to[index] = map.Get(from[index]);
    }
}

[BurstCompile]
struct AddWithIntJob : IJobWithInt
{
    // Read-Write
    public NativeArray<int> result;

    public void Execute(int value)
    {
        for (int i = 0; i < result.Length; i++)
        {
            result[i] += value;
        }
    }
}

[BurstCompile]
struct WriteJobIndexParallelForJob : IJobParallelForWithJobIndex
{
    [WriteOnly]
    public NativeArray<int> result;

    public void Execute(int index, int jobIndex)
    {
        result[index] = jobIndex;
    }
}

class TestJob
{
    private const int ARRAY_SIZE = 5;
    private const int BIG_ARRAY_SIZE = 1024;

    [RuntimeInitializeOnLoadMethod]
    static void OnLoad()
    {
        if (SceneManager.GetActiveScene().name.Equals("Job"))
        {
            Debug.Log($"Cahce Line : {JobsUtility.CacheLineSize} Bytes");
            Debug.Log($"Max Job Thread Count : {JobsUtility.MaxJobThreadCount}");

            Debug.Log("<color=red>IJob</color>");
            TestIJob();

            Debug.Log("<color=red>IJobParallelFor</color>");
            TestIJobParallelFor();

            Debug.Log("<color=red>ScheduleBatchedJobs</color>");
            TestScheduleBatchedJobs();

            Debug.Log("<color=red>NativeContainer.Concurrent</color>");
            TestNativeContainerConcurrent();

            Debug.Log("<color=red>Custom NativeContainer</color>");
            TestCustomNativeContainerConcurrent();

            Debug.Log("<color=red>DeallocateOnJobCompletion</color>");
            TestDeallocateOnJobCompletion();

            Debug.Log("<color=red>Custom Job</color>");
            TestCustomJob();

            Debug.Log("<color=red>Custom JobParallelFor</color>");
            TestCustomJobParallelFor();
        }
    }

    private static void TestIJob()
    {
        TestJobOrJobParallelFor((values1, values2, results) =>
        {

            AddAllJob addAllJob1 = new AddAllJob();
            addAllJob1.adder = 1;
            addAllJob1.values = values1;
            JobHandle addAllJob1Handle = addAllJob1.Schedule();

            AddAllJob addAllJob2 = new AddAllJob();
            addAllJob2.adder = 1;
            addAllJob2.values = values2;
            JobHandle addAllJob2Handle = addAllJob2.Schedule();

            AddJob addJob = new AddJob();
            addJob.a = values1;
            addJob.b = values2;
            addJob.result = results;

            return addJob.Schedule(JobHandle.CombineDependencies(addAllJob1Handle, addAllJob2Handle));
        });
    }

    private static void TestIJobParallelFor()
    {
        TestJobOrJobParallelFor((values1, values2, results) =>
        {
            AddAllParallelForJob addAllJob1 = new AddAllParallelForJob();
            addAllJob1.adder = 1;
            addAllJob1.values = values1;
            JobHandle addAllJob1Handle = addAllJob1.Schedule(ARRAY_SIZE, 1);

            AddAllParallelForJob addAllJob2 = new AddAllParallelForJob();
            addAllJob2.adder = 1;
            addAllJob2.values = values2;
            JobHandle addAllJob2Handle = addAllJob2.Schedule(ARRAY_SIZE, 1);

            AddParallelForJob addJob = new AddParallelForJob();
            addJob.a = values1;
            addJob.b = values2;
            addJob.result = results;

            return addJob.Schedule(ARRAY_SIZE, 1, JobHandle.CombineDependencies(addAllJob1Handle, addAllJob2Handle));
        });
    }

    private static void TestScheduleBatchedJobs()
    {
        var stopwatch = new System.Diagnostics.Stopwatch();
        NativeArray<float> values = new NativeArray<float>(BIG_ARRAY_SIZE, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        for (int i = 0; i < ARRAY_SIZE; i++)
        {
            values[i] = i;
        }

        HeavyJob heavyJob = new HeavyJob();
        heavyJob.weight = 500;
        heavyJob.values = values;
        JobHandle heavyJobHandle = heavyJob.Schedule(BIG_ARRAY_SIZE, 16);

        JobHandle.ScheduleBatchedJobs();

        System.Threading.Thread.Sleep(5);
        stopwatch.Start();
        heavyJobHandle.Complete();
        stopwatch.Stop();

        Debug.Log($"Time : {stopwatch.ElapsedMilliseconds}");
        Print5FloatNativeArray(values);

        values.Dispose();
    }

    private static void TestJobOrJobParallelFor(Func<NativeArray<float4>, NativeArray<float4>, NativeArray<float4>, JobHandle> func)
    {
        NativeArray<float4> values1 = new NativeArray<float4>(ARRAY_SIZE, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeArray<float4> values2 = new NativeArray<float4>(ARRAY_SIZE, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeArray<float4> results = new NativeArray<float4>(ARRAY_SIZE, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        for (int i = 0; i < ARRAY_SIZE; i++)
        {
            results[i] = 0;
            values1[i] = new float4(1 + i, 2 + i, 3 + i, 4 + i);
            values2[i] = new float4(1 + ARRAY_SIZE - i, 2 + ARRAY_SIZE - i, 3 + ARRAY_SIZE - i, 4 + ARRAY_SIZE - i); ;
        }

        JobHandle addJobHandle = func(values1, values2, results);

        addJobHandle.Complete();

        Print5Float4NativeArray(results);

        values1.Dispose();
        values2.Dispose();
        results.Dispose();
    }

    private static void TestNativeContainerConcurrent()
    {
        NativeArray<float> values = new NativeArray<float>(ARRAY_SIZE, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeQueue<float> result = new NativeQueue<float>(Allocator.Persistent);
        result.Enqueue(10);
        result.Enqueue(11);
        result.Enqueue(12);

        for (int i = 0; i < ARRAY_SIZE; i++)
        {
            values[i] = i;
        }

        FilterParallelForJob filterJob = new FilterParallelForJob();
        filterJob.compareType = CompareType.MoreThan;
        filterJob.compareValue = 2;
        filterJob.src = values;
        filterJob.results = result.ToConcurrent();

        JobHandle filterJobHandle = filterJob.Schedule(ARRAY_SIZE, 1);
        filterJobHandle.Complete();

        Print5FloatNativeQueue(result);

        values.Dispose();
        result.Dispose();
    }

    private static void TestCustomNativeContainerConcurrent()
    {
        NativeArray<int> candidates = new NativeArray<int>(ARRAY_SIZE, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeMinMaxInt result = new NativeMinMaxInt(MinMax.Max, Allocator.Persistent);

        for (int i = 0; i < ARRAY_SIZE; i++)
        {
            candidates[i] = (i * 10) + 10;
        }

        MinMaxIntParallelForJob minMaxintJob = new MinMaxIntParallelForJob();
        minMaxintJob.candidates = candidates;
        minMaxintJob.result = result;

        JobHandle minMaxIntJobHandle = minMaxintJob.Schedule(ARRAY_SIZE, 1);
        minMaxIntJobHandle.Complete();

        Debug.Log(result.Value);

        candidates.Dispose();
        result.Dispose();
    }

    private static void TestDeallocateOnJobCompletion()
    {
        NativeArray<int> from = new NativeArray<int>(ARRAY_SIZE, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeArray<int> to = new NativeArray<int>(ARRAY_SIZE, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        NativeMapInt map = new NativeMapInt(3, -1, Allocator.Persistent);

        map.Set(0, 1, 5);
        map.Set(1, 2, 7);
        map.Set(2, 4, 9);

        for (int i = 0; i < ARRAY_SIZE; i++)
        {
            from[i] = i;
        }

        MapIntParallelForJob mapIntJob = new MapIntParallelForJob();
        mapIntJob.from = from;
        mapIntJob.map = map;
        mapIntJob.to = to;

        JobHandle mapIntJobHandle = mapIntJob.Schedule(ARRAY_SIZE, 1);
        mapIntJobHandle.Complete();

        Print5IntNativeArray(to);

        to.Dispose();
    }

    private static void TestCustomJob()
    {
        NativeArray<int> result = new NativeArray<int>(ARRAY_SIZE, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        for (int i = 0; i < ARRAY_SIZE; i++)
        {
            result[i] = i;
        }

        AddWithIntJob addWithIntJob = new AddWithIntJob();
        addWithIntJob.result = result;

        JobHandle addWithIntJobHandle = addWithIntJob.ScheduleWithInt(ARRAY_SIZE);
        addWithIntJobHandle.Complete();

        Print5IntNativeArray(result);

        result.Dispose();
    }

    private static void TestCustomJobParallelFor()
    {
        NativeArray<int> result = new NativeArray<int>(ARRAY_SIZE, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        WriteJobIndexParallelForJob writeJobIndexJob = new WriteJobIndexParallelForJob();
        writeJobIndexJob.result = result;

        JobHandle writeJobIndexJobHandle = writeJobIndexJob.Schedule(ARRAY_SIZE, 2);
        writeJobIndexJobHandle.Complete();

        Print5IntNativeArray(result);

        result.Dispose();
    }

    private static void Print5Float4NativeArray(NativeArray<float4> float4s)
    {
        Debug.Log(AddAllFloat4(float4s[0]) + ", " +
            AddAllFloat4(float4s[1]) + ", " +
            AddAllFloat4(float4s[2]) + ", " +
            AddAllFloat4(float4s[3]));
    }

    private static float AddAllFloat4(float4 f4)
    {
        return f4.w + f4.x + f4.y + f4.z;
    }

    private static void Print5FloatNativeArray(NativeArray<float> floats)
    {
        Debug.Log($"{floats[0]}, {floats[1]}, {floats[2]}, {floats[3]}, {floats[4]}");
    }

    private static void Print5IntNativeArray(NativeArray<int> ints)
    {
        Debug.Log($"{ints[0]}, {ints[1]}, {ints[2]}, {ints[3]}, {ints[4]}");
    }

    private static void Print5FloatNativeQueue(NativeQueue<float> queue)
    {
        Debug.Log($"{queue.Dequeue()}, {queue.Dequeue()}, {queue.Dequeue()}, {queue.Dequeue()}, {queue.Dequeue()}");
    }
}
