using System.Threading.Tasks;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

#region Declaration
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
#endregion

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

    #region Test
    private unsafe static void TestUnsafeUtility()
    {
        // Allocate
        void* a = UnsafeUtility.Malloc(
            size: ARRAY_SIZE * UnsafeUtility.SizeOf<int>(),
            alignment: UnsafeUtility.AlignOf<byte>(),
            allocator: Allocator.Temp);
        void* b = UnsafeUtility.Malloc(
            size: ARRAY_SIZE * UnsafeUtility.SizeOf<int>(),
            alignment: UnsafeUtility.AlignOf<byte>(),
            allocator: Allocator.Temp);

        for (int i = 0; i < ARRAY_SIZE; i++)
        {
            ((int*)a)[i] = i;
        }

        Print5IntArray((int*)b);
        UnsafeUtility.MemCpy(destination: b, source: a, size: ARRAY_SIZE * UnsafeUtility.SizeOf<int>());
        Print5IntArray((int*)b);

        for (int i = 0; i < ARRAY_SIZE; i++)
        {
            UnsafeUtility.WriteArrayElement(
                destination: b,
                index: i,
                value: -i);
        }

        Print5IntFromVoidArray(b);

        // Free
        UnsafeUtility.Free(memory: a, allocator: Allocator.Temp);
        UnsafeUtility.Free(memory: b, allocator: Allocator.Temp);

        Debug.Log($"Vowel.I : {UnsafeUtility.EnumToInt(Vowel.I)}");

        Int4 int4 = new Int4();
        Debug.Log(int4);
        ModifyInt4ByAddressOfRef(ref int4);
        Debug.Log(int4);
    }

    private static async Task<NativeArray<int>> TestNativeArray()
    {
        NativeArray<int> persistent = new NativeArray<int>(length: ARRAY_SIZE, allocator: Allocator.Persistent, options: NativeArrayOptions.UninitializedMemory);
        NativeArray<int> temp = new NativeArray<int>(length: ARRAY_SIZE, allocator: Allocator.Temp);

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
        NativeHashMap<int, System.IntPtr> hashMap = new NativeHashMap<int, System.IntPtr>(capacity: 3, allocator: Allocator.Persistent);
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
    #endregion

    #region Helper
    private unsafe static void ModifyInt4ByAddressOfRef(ref Int4 value)
    {
        void* pVoid = UnsafeUtility.AddressOf(ref value);
        UnsafeUtility.WriteArrayElement(destination: pVoid, index: 0, value: 4);
        UnsafeUtility.WriteArrayElement(destination: pVoid, index: 1, value: 3);
        UnsafeUtility.WriteArrayElement(destination: pVoid, index: 2, value: 2);
        UnsafeUtility.WriteArrayElement(destination: pVoid, index: 3, value: 1);
    }

    private unsafe static void Print5IntArray(int* pInt)
    {
        Debug.Log($"{pInt[0]}, {pInt[1]}, {pInt[2]}, {pInt[3]}, {pInt[4]}");
    }

    private unsafe static void Print5IntFromVoidArray(void* pVoid)
    {
        Debug.Log(
            $"{UnsafeUtility.ReadArrayElement<int>(source: pVoid, index: 0)}, " +
            $"{UnsafeUtility.ReadArrayElement<int>(source: pVoid, index: 1)}, " +
            $"{UnsafeUtility.ReadArrayElement<int>(source: pVoid, index: 2)}, " +
            $"{UnsafeUtility.ReadArrayElement<int>(source: pVoid, index: 3)}, " +
            $"{UnsafeUtility.ReadArrayElement<int>(source: pVoid, index: 4)}");
    }
    #endregion
}
