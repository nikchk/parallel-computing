using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Lab
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            int n = 1000;
            int m = 1000;
            int l = 1000;

            int[] threadCounts = { 2, 4, 8, 16, 32 };

            bool showMatrices = (n <= 10 && m <= 10 && l <= 10);
            bool verifyCorrectness = true;

            Console.WriteLine("Множення матриць A(n x m) * B(m x l) = C(n x l) ");
            Console.WriteLine($"n = {n}, m = {m}, l = {l}\n");

            double[,] A = GenerateMatrix(n, m, seed: 1);
            double[,] B = GenerateMatrix(m, l, seed: 2);

            if (showMatrices)
            {
                Console.WriteLine("Матриця A:");
                DisplayMatrix(A);
                Console.WriteLine("Матриця B:");
                DisplayMatrix(B);
            }

            var sw = Stopwatch.StartNew();
            double[,] cSequential = MultiplySequential(A, B);
            sw.Stop();
            double tSeq = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine($"Послідовне множення: {tSeq:F3} мс");
            if (showMatrices)
            {
                Console.WriteLine("Матриця C (послідовно):");
                DisplayMatrix(cSequential);
            }

            Console.WriteLine();
            Console.WriteLine("k\tT_par(мс)\tПрискорення S\tЕфективність E");
            Console.WriteLine("-------------------------------------------------------------");

            foreach (int k in threadCounts)
            {
                sw.Restart();
                double[,] cParallel = MultiplyParallel(A, B, k);
                sw.Stop();
                double tPar = sw.Elapsed.TotalMilliseconds;

                double speedup = tSeq / tPar;
                double efficiency = speedup / k;

                Console.WriteLine(
                    $"{k}\t{tPar,10:F3}\t{speedup,10:F3}\t{efficiency,10:F3}");

                if (verifyCorrectness)
                {
                    bool same = MatricesEqual(cSequential, cParallel, 1e-6);
                    if (!same)
                        Console.WriteLine($"  !!! Результат для k={k} НЕ співпадає з послідовним !!!");
                }
            }

            Console.WriteLine("\nГотово.");
        }

        static double[,] GenerateMatrix(int rows, int cols, int seed)
        {
            var rnd = new Random(seed);
            var matrix = new double[rows, cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    matrix[i, j] = Math.Round(rnd.NextDouble() * 10, 2);
            return matrix;
        }

        static void DisplayMatrix(double[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                    Console.Write($"{matrix[i, j],7:F2} ");
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        static double[,] MultiplySequential(double[,] A, double[,] B)
        {
            int n = A.GetLength(0);
            int m = A.GetLength(1);
            int l = B.GetLength(1);

            var C = new double[n, l];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < l; j++)
                {
                    double sum = 0;
                    for (int t = 0; t < m; t++)
                        sum += A[i, t] * B[t, j];
                    C[i, j] = sum;
                }
            }
            return C;
        }

        static double[,] MultiplyParallel(double[,] A, double[,] B, int k)
        {
            int n = A.GetLength(0);
            int m = A.GetLength(1);
            int l = B.GetLength(1);

            var C = new double[n, l];

            (int start, int end)[] ranges = SplitRowsAmongThreads(n, k);

            var threads = new Thread[ranges.Length];

            for (int t = 0; t < ranges.Length; t++)
            {
                int startRow = ranges[t].start;
                int endRow = ranges[t].end;

                threads[t] = new Thread(() =>
                {
                    MultiplyRange(A, B, C, startRow, endRow, m, l);
                });
                threads[t].Start();
            }

            for (int t = 0; t < threads.Length; t++)
                threads[t].Join();

            return C;
        }

        static void MultiplyRange(double[,] A, double[,] B, double[,] C,
                                 int startRow, int endRow, int m, int l)
        {
            for (int i = startRow; i < endRow; i++)
            {
                for (int j = 0; j < l; j++)
                {
                    double sum = 0;
                    for (int t = 0; t < m; t++)
                        sum += A[i, t] * B[t, j];
                    C[i, j] = sum;
                }
            }
        }

        static (int start, int end)[] SplitRowsAmongThreads(int n, int k)
        {
            int effectiveK = Math.Min(k, n);
            if (effectiveK <= 0) effectiveK = 1;

            var ranges = new (int start, int end)[effectiveK];

            int baseRows = n / effectiveK;
            int remainder = n % effectiveK;

            int current = 0;
            for (int t = 0; t < effectiveK; t++)
            {
                int rowsForThisThread = baseRows + (t < remainder ? 1 : 0);
                ranges[t] = (current, current + rowsForThisThread);
                current += rowsForThisThread;
            }

            return ranges;
        }

        static bool MatricesEqual(double[,] X, double[,] Y, double eps)
        {
            int rows = X.GetLength(0);
            int cols = X.GetLength(1);
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    if (Math.Abs(X[i, j] - Y[i, j]) > eps)
                        return false;
            return true;
        }
    }
}
