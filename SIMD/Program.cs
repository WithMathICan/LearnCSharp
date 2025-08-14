
using System;
using System.Diagnostics;
using System.Numerics;

class Program {
    static void Main1() {
        int x = 500;
        Console.WriteLine(x.GetHashCode());


        const int size = 10000000;  // 10 million elements
        float[] a = new float[size];
        float[] b = new float[size];
        float[] result = new float[size];

        Array.Fill(a, 1.0f);
        Array.Fill(b, 2.0f);
        int i = 0;
        // Non-SIMD version
        var stopwatch = Stopwatch.StartNew();
        for (i = 0; i < size; i++) {
            result[i] = a[i] + b[i];
        }
        stopwatch.Stop();
        Console.WriteLine($"Non-SIMD time: {stopwatch.ElapsedMilliseconds} ms");

        // SIMD version
        stopwatch = Stopwatch.StartNew();
        int vectorSize = Vector<float>.Count;  // e.g., 8 for AVX2
        
        for (i = 0; i + vectorSize <= size; i += vectorSize) {
            Vector<float> va = new Vector<float>(a, i);
            Vector<float> vb = new Vector<float>(b, i);
            (va + vb).CopyTo(result, i);
        }
        // Handle remainder
        for (; i < size; i++) {
            result[i] = a[i] + b[i];
        }
        stopwatch.Stop();
        Console.WriteLine($"SIMD time: {stopwatch.ElapsedMilliseconds} ms");

        // Verify a sample result
        Console.WriteLine($"Sample result: {result[0]}");  // Should be 3.0
    }
}