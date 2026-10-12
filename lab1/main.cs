using System;
using System.Diagnostics;
using System.Threading;

class MatrixAdditionLabThread
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        int size = 3000;
        int threadCount = 4;

        double[,] matrixA = GenerateMatrix(size, size);
        double[,] matrixB = GenerateMatrix(size, size);

        double[,] resultSequential = new double[size, size];
        double[,] resultParallel = new double[size, size];

        Console.WriteLine($"Розмір матриць: {size}x{size} ({size * size:N0} елементів).");
        Console.WriteLine($"Кількість потоків для паралельного алгоритму: {threadCount}\n");

        Stopwatch stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                resultSequential[i, j] = matrixA[i, j] + matrixB[i, j];
            }
        }

        stopwatch.Stop();
        long sequentialMs = stopwatch.ElapsedMilliseconds;
        Console.WriteLine($"Послідовний час виконання: {sequentialMs} мс");

        stopwatch.Restart();

        Thread[] threads = new Thread[threadCount];
        int rowsPerThread = size / threadCount;

        for (int t = 0; t < threadCount; t++)
        {
            int startRow = t * rowsPerThread;
            int endRow = (t == threadCount - 1) ? size : (t + 1) * rowsPerThread;

            threads[t] = new Thread(() =>
            {
                for (int i = startRow; i < endRow; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        resultParallel[i, j] = matrixA[i, j] + matrixB[i, j];
                    }
                }
            });

            threads[t].Start();
        }

        for (int t = 0; t < threadCount; t++)
        {
            threads[t].Join();
        }

        stopwatch.Stop();
        long parallelMs = stopwatch.ElapsedMilliseconds;
        Console.WriteLine($"Паралельний час виконання:  {parallelMs} ms ({parallelMs} мс)");

        if (parallelMs > 0)
        {
            double speedup = (double)sequentialMs / parallelMs;
            Console.WriteLine($"\nКоефіцієнт прискорення (Speedup): {speedup:F2}x");
        }

        bool isEqual = VerifyMatrices(resultSequential, resultParallel, size);
        Console.WriteLine($"Результати збігаються: {isEqual}");
    }

    static double[,] GenerateMatrix(int rows, int cols)
    {
        double[,] matrix = new double[rows, cols];
        Random rand = new Random();
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                matrix[i, j] = rand.NextDouble() * 100;
            }
        }
        return matrix;
    }

    static bool VerifyMatrices(double[,] m1, double[,] m2, int size)
    {
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (Math.Abs(m1[i, j] - m2[i, j]) > 1e-9)
                    return false;
            }
        }
        return true;
    }
}
