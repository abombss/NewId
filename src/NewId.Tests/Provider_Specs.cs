using System.CodeDom;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MassTransit.NewIdTests
{
    using System;
    using System.Threading;
    using NewIdProviders;
    using NUnit.Framework;


    [TestFixture]
    public class StopwatchTickProvider_Specs
    {
        [Test, Explicit]
        public void Should_keep_accurate_time()
        {
            TimeSpan timeDelta = TimeSpan.FromSeconds(3);

            var timestamp = DateTime.UtcNow;
            var provider = new StopwatchTickProvider();
            long start = provider.Ticks;
            Thread.Sleep(timeDelta);
            long stop = provider.Ticks;

            var startTime = new DateTime(start);
            Console.WriteLine("Start time: {0}, Original: {1}", startTime, timestamp);


            long deltaTicks = Math.Abs(stop - start);
            // 0.01% acceptable delta
            var acceptableDelta = (long)(timeDelta.Ticks);

            Assert.Less(deltaTicks, acceptableDelta);
        }

        [Test, Explicit]
        public void Should_not_lag_time()
        {
            TimeSpan timeDelta = TimeSpan.FromMinutes(1);

            var startProvider = new StopwatchTickProvider();
            Thread.Sleep(timeDelta);
            var endProvider = new StopwatchTickProvider();


            long deltaTicks = Math.Abs(endProvider.Ticks - startProvider.Ticks);
            // 0.01% acceptable delta
            var acceptableDelta = (long)(timeDelta.Ticks * 0.0001);

            Assert.Less(deltaTicks, acceptableDelta);
        }
    }

    [TestFixture]
    public class WindowsKernelFileTimeTickProvider_Specs
    {
        [Test, Explicit]
        public void Should_Not_Drift()
        {
            var highResProvider = new WindowsKernelFileTimeTickProvider();
            var stopwatchProvider = new StopwatchTickProvider();
            
            long highResStart = highResProvider.Ticks;
            long stopwatchStart = stopwatchProvider.Ticks;

            for (int i = 0; i < 10; i++)
            {
                // warmup
                stopwatchStart = stopwatchProvider.Ticks;
                highResStart = highResProvider.Ticks;
            }

            var start = new DateTime(highResProvider.Ticks, DateTimeKind.Utc);

            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < 30)
            {
                var elapsedMilliseconds = sw.ElapsedMilliseconds;
                var date = DateTime.UtcNow;
                var highResDate = new DateTime(highResProvider.Ticks, DateTimeKind.Utc);
                var stopwatchDate = new DateTime(stopwatchProvider.Ticks, DateTimeKind.Utc);

                TimeSpan highResToStopwatch = highResDate - stopwatchDate;
                TimeSpan highResToDate = highResDate - date;
                TimeSpan stopwatchToDate = stopwatchDate - date;
                
                Console.WriteLine("{3:00000} Diff: HR-SW {0:0.000} ms, HR-D {1:0.000} ms, SW-D {2:0.000} ms", 
                    highResToStopwatch.TotalMilliseconds,
                    highResToDate.TotalMilliseconds,
                    stopwatchToDate.TotalMilliseconds,
                    elapsedMilliseconds);

                Thread.Sleep(1000);
            }
        }

        [Test, Explicit]
        public void Should_Keep_Accurate_Time()
        {
            TimeSpan duration = TimeSpan.FromSeconds(5);
            const int iterations = 10;
            long[] highResStops = new long[iterations];
            long[] stopwatchStops = new long[iterations];
            long[] highResStarts = new long[iterations];
            long[] stopwatchStarts = new long[iterations];

            TimeSpan timeDelta = TimeSpan.FromTicks(duration.Ticks / iterations);
            var highResProvider = new WindowsKernelFileTimeTickProvider();
            var stopwatchProvider = new StopwatchTickProvider();

            long highResStart = highResProvider.Ticks;
            long stopwatchStart = stopwatchProvider.Ticks;

            for (int i = 0; i < 10; i++)
            {
                // warmup
                stopwatchStart = stopwatchProvider.Ticks;
                highResStart = highResProvider.Ticks;                
            }
         

            var highResTask = Task.Run(
                () =>
                {
                    highResStart = highResProvider.Ticks;
                    for (int i = 0; i < iterations; i++)
                    {
                        highResStarts[i] = highResProvider.Ticks;
                        Thread.Sleep(timeDelta);
                        highResStops[i] = highResProvider.Ticks;

                    }
                });

            Thread.Sleep(199);

            var stopwatchTask = Task.Run(
                () =>
                {
                    stopwatchStart = stopwatchProvider.Ticks;
                    for (int i = 0; i < iterations; i++)
                    {
                        stopwatchStarts[i] = stopwatchProvider.Ticks;
                        Thread.Sleep(timeDelta);
                        stopwatchStops[i] = stopwatchProvider.Ticks;

                    }
                });

            Task.WhenAll(highResTask, stopwatchTask).Wait();

            var acceptableDelta = (long)(timeDelta.Ticks);

            for (int i = 0; i < iterations; i++)
            {
                Console.WriteLine(
                    "HR has a delta of {2:p} and total delta of {0:p}\nSW has a delta of {3:p} and total delta of {1:p}\n",
                    (double)(Math.Abs(highResStops[i] - (highResStart + (i * timeDelta.Ticks))) - timeDelta.Ticks)
                        / (double)acceptableDelta,
                    (double)(Math.Abs(stopwatchStops[i] - (stopwatchStart + (i * timeDelta.Ticks))) - timeDelta.Ticks)
                        / (double)acceptableDelta,
                    Math.Abs((double)(Math.Abs(highResStops[i] - highResStarts[i]) - timeDelta.Ticks) / (double)acceptableDelta),
                    Math.Abs((double)(Math.Abs(stopwatchStops[i] - stopwatchStarts[i]) - timeDelta.Ticks) / (double)acceptableDelta));
            }
        }
    }
}