using System.Threading.Tasks;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

enum Vowel
{
    A,
    E,
    I,
    O,
    U
}

[StructLayout(LayoutKind.Sequential)]
struct Int4
{
    int _0;
    int _1;
    int _2;
    int _3;

    public override string ToString()
    {
        return $"{this._0}, {this._1}, {this._2}, {this._3}";
    }
}

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

            Debug.Log("<color=red>NativeQueue</color>");
            TestNativeHashMap();
        }
    }

    private unsafe static void TestUnsafeUtility()
    {
        // Alocate
        void* a = UnsafeUtility.Malloc(ARRAY_SIZE * UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<byte>(), Allocator.Temp);
        void* b = UnsafeUtility.Malloc(ARRAY_SIZE * UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<byte>(), Allocator.Temp);

        for (int i = 0; i < ARRAY_SIZE; i++)
        {
            ((int*)a)[i] = i;
        }

        Print5IntArray((int*)b);
        UnsafeUtility.MemCpy(b, a, ARRAY_SIZE * UnsafeUtility.SizeOf<int>());
        Print5IntArray((int*)b);

        for (int i = 0; i < ARRAY_SIZE; i++)
        {
            UnsafeUtility.WriteArrayElement(b, i, -i);
        }

        Print5IntFromVoidArray(b);

        // Free
        UnsafeUtility.Free(a, Allocator.Temp);
        UnsafeUtility.Free(b, Allocator.Temp);

        Debug.Log($"Vowel.I : {UnsafeUtility.EnumToInt(Vowel.I)}");

        Int4 int4 = new Int4();
        Debug.Log(int4);
        ModifyInt4ByAddressOfRef(ref int4);
        Debug.Log(int4);
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

    private static void TestNativeHashMap()
    {
        System.IntPtr a = Marshal.StringToHGlobalAnsi("A");
        System.IntPtr b = Marshal.StringToHGlobalAnsi("B");
        System.IntPtr c = Marshal.StringToHGlobalAnsi("C");
        NativeHashMap<int, System.IntPtr> hashMap = new NativeHashMap<int, System.IntPtr>(3, Allocator.Persistent);
        {
            hashMap.TryAdd(0, a);
            hashMap.TryAdd(1, b);
            hashMap.TryAdd(2, c);

            System.IntPtr zero;
            System.IntPtr one;
            System.IntPtr two;
            hashMap.TryGetValue(0, out zero);
            hashMap.TryGetValue(1, out one);
            hashMap.TryGetValue(2, out two);
            Debug.Log(Marshal.PtrToStringAnsi(zero));
            Debug.Log(Marshal.PtrToStringAnsi(one));
            Debug.Log(Marshal.PtrToStringAnsi(two));
        }
        hashMap.Dispose();
        Marshal.FreeHGlobal(a);
        Marshal.FreeHGlobal(b);
        Marshal.FreeHGlobal(c);

    }

    private unsafe static void ModifyInt4ByAddressOfRef(ref Int4 value)
    {
        void* pVoid = UnsafeUtility.AddressOf(ref value);
        UnsafeUtility.WriteArrayElement(pVoid, 0, 4);
        UnsafeUtility.WriteArrayElement(pVoid, 1, 3);
        UnsafeUtility.WriteArrayElement(pVoid, 2, 2);
        UnsafeUtility.WriteArrayElement(pVoid, 3, 1);
    }

    private unsafe static void Print5IntArray(int* pInt)
    {
        Debug.Log($"{pInt[0]}, {pInt[1]}, {pInt[2]}, {pInt[3]}, {pInt[4]}");
    }

    private unsafe static void Print5IntFromVoidArray(void* pVoid)
    {
        Debug.Log(
            $"{UnsafeUtility.ReadArrayElement<int>(pVoid, 0)}, " +
            $"{UnsafeUtility.ReadArrayElement<int>(pVoid, 1)}, " +
            $"{UnsafeUtility.ReadArrayElement<int>(pVoid, 2)}, " +
            $"{UnsafeUtility.ReadArrayElement<int>(pVoid, 3)}, " +
            $"{UnsafeUtility.ReadArrayElement<int>(pVoid, 4)}");
    }
}
