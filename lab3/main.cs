using System;
using System.Diagnostics;
using System.Threading;

namespace LabParallelSLAR
{
    class Program
    {
        static void Main(string[] args)
        {
            int[] sizes = { 20, 400, 800, 1200 };

            int maxCores = Environment.ProcessorCount;
            int[] coreCounts = BuildCoreCounts(maxCores);
            Console.WriteLine("Logical processors on machine: " + maxCores);
            Console.WriteLine(new string('-', 75));
            Console.WriteLine("{0,-8}{1,-20}{2,-10}{3,-20}{4,-15}",
                "N", "Sequential, ms", "Cores", "Parallel, ms", "Speedup");
            Console.WriteLine(new string('-', 75));

            foreach (int n in sizes)
            {
                double[,] A = GenerateMatrix(n);
                double[] b = GenerateVector(n);

                double[,] Aseq = (double[,])A.Clone();
                double[] bseq = (double[])b.Clone();

                var swSeq = Stopwatch.StartNew();
                double[] xSeq = GaussSequential(Aseq, bseq);
                swSeq.Stop();
                long tSeq = swSeq.ElapsedMilliseconds;

                Console.WriteLine("{0,-8}{1,-20}{2,-10}{3,-20}{4,-15}",
                    n, tSeq, "-", "-", "-");

                foreach (int cores in coreCounts)
                {
                    double[,] Apar = (double[,])A.Clone();
                    double[] bpar = (double[])b.Clone();

                    var swPar = Stopwatch.StartNew();
                    double[] xPar = GaussParallel(Apar, bpar, cores);
                    swPar.Stop();
                    long tPar = swPar.ElapsedMilliseconds;

                    double speedup = tPar > 0 ? (double)tSeq / tPar : double.NaN;

                    Console.WriteLine("{0,-8}{1,-20}{2,-10}{3,-20}{4,-15:F2}",
                        "", "", cores, tPar, speedup);
                }

                Console.WriteLine(new string('-', 75));
            }

            Console.WriteLine("Done");
            Console.ReadKey();
        }

        static int[] BuildCoreCounts(int maxCores)
        {
            var list = new System.Collections.Generic.List<int>();
            int c = 1;
            while (c < maxCores)
            {
                list.Add(c);
                c *= 2;
            }
            list.Add(maxCores);
            return list.ToArray();
        }

        static double[,] GenerateMatrix(int n)
        {
            var rnd = new Random(42);
            double[,] A = new double[n, n];

            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    A[i, j] = rnd.NextDouble() * 10.0 - 5.0;

            for (int i = 0; i < n; i++)
                A[i, i] += n * 10.0;

            return A;
        }

        static double[] GenerateVector(int n)
        {
            var rnd = new Random(123);
            double[] b = new double[n];
            for (int i = 0; i < n; i++)
                b[i] = rnd.NextDouble() * 100.0;
            return b;
        }

        static double[] GaussSequential(double[,] A, double[] b)
        {
            int n = b.Length;

            for (int k = 0; k < n - 1; k++)
            {
                double pivot = A[k, k];
                for (int i = k + 1; i < n; i++)
                {
                    double factor = A[i, k] / pivot;
                    for (int j = k; j < n; j++)
                        A[i, j] -= factor * A[k, j];
                    b[i] -= factor * b[k];
                }
            }

            return BackSubstitution(A, b);
        }

        static double[] GaussParallel(double[,] A, double[] b, int threadCount)
        {
            int n = b.Length;

            for (int k = 0; k < n - 1; k++)
            {
                double pivot = A[k, k];
                double bk = b[k];

                int rowsLeft = n - (k + 1);
                if (rowsLeft <= 0) continue;

                int actualThreads = Math.Min(threadCount, rowsLeft);
                if (actualThreads < 1) actualThreads = 1;

                Thread[] threads = new Thread[actualThreads];
                int chunkSize = (int)Math.Ceiling(rowsLeft / (double)actualThreads);

                for (int t = 0; t < actualThreads; t++)
                {
                    int startRow = k + 1 + t * chunkSize;
                    int endRow = Math.Min(startRow + chunkSize, n);

                    if (startRow >= endRow)
                    {
                        threads[t] = null;
                        continue;
                    }

                    threads[t] = new Thread(() =>
                    {
                        for (int i = startRow; i < endRow; i++)
                        {
                            double factor = A[i, k] / pivot;
                            for (int j = k; j < n; j++)
                                A[i, j] -= factor * A[k, j];
                            b[i] -= factor * bk;
                        }
                    });

                    threads[t].Start();
                }

                for (int t = 0; t < actualThreads; t++)
                {
                    if (threads[t] != null)
                        threads[t].Join();
                }
            }

            return BackSubstitution(A, b);
        }

        static double[] BackSubstitution(double[,] A, double[] b)
        {
            int n = b.Length;
            double[] x = new double[n];

            for (int i = n - 1; i >= 0; i--)
            {
                double sum = b[i];
                for (int j = i + 1; j < n; j++)
                    sum -= A[i, j] * x[j];
                x[i] = sum / A[i, i];
            }

            return x;
        }
    }
}
