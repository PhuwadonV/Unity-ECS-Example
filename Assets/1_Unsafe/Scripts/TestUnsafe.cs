using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

[StructLayout(LayoutKind.Explicit)]
struct Union
{
    [FieldOffset(0)]
    public int i;

    [FieldOffset(0)]
    public float f;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
struct PointPack4
{
    public int x;
    private byte _0;
    public int y;
    private byte _1;
    public int z;

    public override string ToString()
    {
        return $"X = {x}, Y = {y}, Z = {z}";
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
struct PointPack2
{
    public int x;
    private byte _0;
    public int y;
    private byte _1;
    public int z;

    public override string ToString()
    {
        return $"X = {x}, Y = {y}, Z = {z}";
    }
}

unsafe struct ColorStruct
{
    public float r;
    public float g;
    public float* b;
}

unsafe class ColorClass
{
    public float r = 0;
    public float g = 0;
    public float* b = null;
}

class TestUnsafe
{
    [RuntimeInitializeOnLoadMethod]
    static void Start()
    {
        TestPointer(100);
        Debug.Log("<color=red>Union</color>");
        TestUnion();
        Debug.Log("<color=red>PointPack4</color>");
        TestPointPack4();
        Debug.Log("<color=red>PointPack2</color>");
        TestPointPack2();
    }

    private static async void TestPointer(int count)
    {
        for (int i = 0; Application.isPlaying && i < count; i++)
        {
            TestStructPointer();
            await Task.Delay(1000);
            TestClassPointer();
            await Task.Delay(1000);
        }

        Camera.main.backgroundColor = Color.clear;
    }

    private unsafe static void TestStructPointer()
    {
        ColorStruct color = new ColorStruct();
        float b = 0.5f;
        color.b = &b;

        ModifyStruct(&color);
        ModifyFloat(&color.g);

        Camera.main.backgroundColor = new Color(color.r, color.g, *color.b, 0);
    }

    private unsafe static void TestClassPointer()
    {
        ColorClass color = new ColorClass();
        float b = 0.5f;
        color.b = &b;

        fixed (float* pClassGreen = &color.g)
        {
            ModifyFloat(pClassGreen);
        }

        Camera.main.backgroundColor = new Color(color.r, color.g, *color.b, 0);
    }

    private static void TestUnion()
    {
        Union union = new Union();
        union.i = 5;
        Debug.Log(union.i);

        union.f = 5.5f;
        Debug.Log(union.f);
        Debug.Log(union.i);
    }

    private static void TestPointPack4()
    {
        PointPack4 point = new PointPack4
        {
            x = 1,
            y = 2,
            z = 3
        };

        Debug.Log(point);

        unsafe
        {
            byte* pAddr = (byte*)&point;
            *(int*)(pAddr + 0) = 10;
            *(int*)(pAddr + 8) = 20;
            *(int*)(pAddr + 16) = 30;
        }

        Debug.Log(point);
    }

    private static void TestPointPack2()
    {
        PointPack2 point = new PointPack2
        {
            x = 1,
            y = 2,
            z = 3
        };

        Debug.Log(point);

        unsafe
        {
            byte* pAddr = (byte*)&point;
            *(int*)(pAddr + 0) = 10;
            *(int*)(pAddr + 6) = 20;
            *(int*)(pAddr + 12) = 30;
        }

        Debug.Log(point);
    }

    private unsafe static void ModifyStruct(ColorStruct* pColor)
    {
        pColor->r = 1;
    }

    private unsafe static void ModifyFloat(float* pFloat)
    {
        *pFloat = 1;
    }
}
