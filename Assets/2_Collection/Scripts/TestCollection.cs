using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

class TestCollection
{
    private const int ARRAY_SIZE = 5;

    [RuntimeInitializeOnLoadMethod]
    static async void OnLoad()
    {
        if (SceneManager.GetActiveScene().name.Equals("Collection"))
        {
            Debug.Log("<color=red>UnsafeUtility</color>");
            TestUnsafeUtility();

            Debug.Log("<color=red>NativeArray</color>");
            NativeArray<int> persistent = await TestNativeArray();
            persistent.Dispose();
        }
    }

    private unsafe static void TestUnsafeUtility()
    {
        // Alocate
        int* a = (int*)(UnsafeUtility.Malloc(ARRAY_SIZE * UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<byte>(), Allocator.Temp));
        int* b = (int*)(UnsafeUtility.Malloc(ARRAY_SIZE * UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<byte>(), Allocator.Temp));

        for (int i = 0; i < ARRAY_SIZE; i++)
        {
            a[i] = i;
        }

        Print5IntArray(b);
        UnsafeUtility.MemCpy(b, a, ARRAY_SIZE * UnsafeUtility.SizeOf<int>());
        Print5IntArray(b);

        // Free
        UnsafeUtility.Free(a, Allocator.Temp);
        UnsafeUtility.Free(b, Allocator.Temp);
    }

    private static async Task<NativeArray<int>> TestNativeArray()
    {
        NativeArray<int> persistent = new NativeArray<int>(ARRAY_SIZE, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        NativeArray<int> temp = new NativeArray<int>(ARRAY_SIZE, Allocator.Temp);

        // Log value
        Debug.Log(temp[0]);
        Debug.Log(persistent[0]);

        // Set value
        temp[0] = 1;
        persistent[0] = 1;

        // Log value
        Debug.Log(temp[0]);
        Debug.Log(persistent[0]);

        temp.Dispose();
        await Task.Delay(100);
        return persistent;
    }

    private unsafe static void Print5IntArray(int* pInt)
    {
        Debug.Log($"{pInt[0]}, {pInt[1]}, {pInt[2]}, {pInt[3]}, {pInt[4]}");
    }
}
