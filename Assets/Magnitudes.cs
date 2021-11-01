using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class Magnitudes : MonoBehaviour
{
    [SerializeField] int arrayLength = 100;
    NativeArray<Vector2> input;
    NativeArray<float> output;

    void Start()
    {
        input = new NativeArray<Vector2>(arrayLength, Allocator.TempJob);
        output = new NativeArray<float>(arrayLength, Allocator.TempJob);

        for (int i = 0; i < input.Length; i++)
            input[i] = new Vector2(UnityEngine.Random.Range(-9f, 9f), UnityEngine.Random.Range(-9f, 9f));

        var magnitudeJob = new MagnitudesJob { Input = input, Output = output };
        JobHandle magnitudeHandle = magnitudeJob.Schedule(input.Length, 4);
        magnitudeHandle.Complete();
        Debug.Log("magnitude of LAST vector = " + output[arrayLength-1]);
        input.Dispose();
        output.Dispose();
    }
}

struct MagnitudesJob : IJobParallelFor
{
    public NativeArray<Vector2> Input;
    public NativeArray<float> Output;

    public void Execute(int index)
    {
        Output[index] = Input[index].magnitude;
        Debug.Log($"Magnitude of {index} vector is {Output[index]}");
    }
}
