﻿using System;
using System.Diagnostics;
using System.Threading;

namespace Voxelmetric.Code.Utilities
{
    public static class Clock
    {
        private interface IStopwatch
        {
            bool IsRunning { get; }
            TimeSpan Elapsed { get; }

            void Start();
            void Stop();
            void Reset();
        }

        private class TimeWatch : IStopwatch
        {
            private readonly Stopwatch stopwatch = new Stopwatch();

            public TimeSpan Elapsed
            {
                get { return stopwatch.Elapsed; }
            }

            public bool IsRunning
            {
                get { return stopwatch.IsRunning; }
            }

            public TimeWatch()
            {
                if (!Stopwatch.IsHighResolution)
                    throw new NotSupportedException("Your hardware doesn't support high resolution counter");
                
                // Use the second core/processor for the test
                Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(2);

                // Prevent "Normal" processes from interrupting Threads
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

                // Prevent "Normal" threads from interrupting this thread
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
            }

            public void Start()
            {
                stopwatch.Start();
            }

            public void Stop()
            {
                stopwatch.Stop();
            }

            public void Reset()
            {
                stopwatch.Reset();
            }
        }

        private class CpuWatch : IStopwatch
        {
            private TimeSpan startTime;
            private TimeSpan endTime;

            public TimeSpan Elapsed
            {
                get
                {
                    if (IsRunning)
                        throw new NotImplementedException("Getting elapsed span while watch is running is not implemented");

                    return endTime - startTime;
                }
            }

            public bool IsRunning { get; private set; }

            public void Start()
            {
                startTime = Process.GetCurrentProcess().TotalProcessorTime;
                IsRunning = true;
            }

            public void Stop()
            {
                endTime = Process.GetCurrentProcess().TotalProcessorTime;
                IsRunning = false;
            }

            public void Reset()
            {
                startTime = TimeSpan.Zero;
                endTime = TimeSpan.Zero;
            }
        }

        private static double Benchmark<T>(Action action, int iterations) where T : IStopwatch, new()
        {
            // Clean Garbage
            GC.Collect();

            // Wait for the finalizer queue to empty
            GC.WaitForPendingFinalizers();

            // Clean Garbage
            GC.Collect();

            // Warm up
            action();

            var stopwatch = new T();
            stopwatch.Start();
            for (int j = 0; j < iterations; j++)
                action();
            stopwatch.Stop();

            return stopwatch.Elapsed.TotalMilliseconds;
        }

        public static double BenchmarkTime(Action action, int iterations = 10000)
        {
            return Benchmark<TimeWatch>(action, iterations);
        }

        public static double BenchmarkCpu(Action action, int iterations = 10000)
        {
            return Benchmark<CpuWatch>(action, iterations);
        }
    }
}
