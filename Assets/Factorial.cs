using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class Factorial : MonoBehaviour
{

    void Start()
    {
        var br = new NativeArray<int>(2, Allocator.TempJob);
        br[0] = 11;
        var f = new FactorialJob { Bridge = br};
        var p = new PowJob { Bridge = br };

        Debug.Log("before job");

        JobHandle factorialHandle = f.Schedule();
        factorialHandle.Complete();
        Debug.Log($"Factorial of {br[0]} is {br[1]}");
        
        JobHandle powHandle = p.Schedule(factorialHandle);
        powHandle.Complete();
        Debug.Log($"Pow of {br[0]} in .5f is {br[1]}");

        br.Dispose(); // только после Complete
        
        Debug.Log("after job");
    }
}

struct FactorialJob : IJob
{

    public NativeArray<int> Bridge;

    public void Execute()
    {
        Bridge[1] = getFactorial(Bridge[0]);
    }

    int getFactorial(int value)
    {
        if (value == 0) return 1;
        return value * getFactorial(value - 1);
    }
}

struct PowJob : IJob
{
    public NativeArray<int> Bridge;

    public void Execute()
    {
        Bridge[1] = getPow(Bridge[0]);
    }

    int getPow(int f)
    {
        return (int)Mathf.Pow(f, .5f);
    }
}
