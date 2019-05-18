﻿using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;

delegate R Func<R, A0, A1, A2>(A0 a0, A1 a1, A2 a2);

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
struct AddAllJobbParallelFor : IJobParallelFor
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
struct AddJobParallelFor : IJobParallelFor
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

class TestJob
{
    private const int ARRAY_SIZE = 5;
    private const int BIG_ARRAY_SIZE = 1024;

    [RuntimeInitializeOnLoadMethod]
    static void OnLoad()
    {
        if (SceneManager.GetActiveScene().name.Equals("Job"))
        {
            Debug.Log("<color=red>IJob</color>");
            TestIJob();

            Debug.Log("<color=red>IJobParallelFor</color>");
            TestIJobParallelFor();

            Debug.Log("<color=red>ScheduleBatchedJobs</color>");
            TestScheduleBatchedJobs();
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
            AddAllJobbParallelFor addAllJob1 = new AddAllJobbParallelFor();
            addAllJob1.adder = 1;
            addAllJob1.values = values1;
            JobHandle addAllJob1Handle = addAllJob1.Schedule(ARRAY_SIZE, 1);

            AddAllJobbParallelFor addAllJob2 = new AddAllJobbParallelFor();
            addAllJob2.adder = 1;
            addAllJob2.values = values2;
            JobHandle addAllJob2Handle = addAllJob2.Schedule(ARRAY_SIZE, 1);

            AddJobParallelFor addJob = new AddJobParallelFor();
            addJob.a = values1;
            addJob.b = values2;
            addJob.result = results;

            return addJob.Schedule(ARRAY_SIZE, 1, JobHandle.CombineDependencies(addAllJob1Handle, addAllJob2Handle));
        });
    }

    private static void TestScheduleBatchedJobs()
    {
        var stopwatch = new System.Diagnostics.Stopwatch();
        NativeArray<float> values = new NativeArray<float>(BIG_ARRAY_SIZE, Allocator.Persistent);

        for (int i = 0; i < ARRAY_SIZE; i++)
        {
            values[i] = i;
        }

        HeavyJob heavyJob = new HeavyJob();
        heavyJob.weight = 1000;
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

    private static void TestJobOrJobParallelFor(Func<JobHandle, NativeArray<float4>, NativeArray<float4>, NativeArray<float4>> func)
    {
        NativeArray<float4> values1 = new NativeArray<float4>(ARRAY_SIZE, Allocator.TempJob);
        NativeArray<float4> values2 = new NativeArray<float4>(ARRAY_SIZE, Allocator.TempJob);
        NativeArray<float4> results = new NativeArray<float4>(ARRAY_SIZE, Allocator.Persistent);

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

    private static void Print5Float4NativeArray(NativeArray<float4> float4s)
    {
        Debug.Log(addAllFloat4(float4s[0]) + ", " +
            addAllFloat4(float4s[1]) + ", " +
            addAllFloat4(float4s[2]) + ", " +
            addAllFloat4(float4s[3]));
    }

    private static float addAllFloat4(float4 f4)
    {
        return f4.w + f4.x + f4.y + f4.z;
    }

    private static void Print5FloatNativeArray(NativeArray<float> floats)
    {
        Debug.Log($"{floats[0]}, {floats[1]}, {floats[2]}, {floats[3]}, {floats[4]}");
    }
}
