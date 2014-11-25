using InMemoryCachingStrategies.Strategy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InMemoryCachingStrategies
{
    public class Runner
    {
        public class RunResult
        {
            public bool Success { get; set; }
            public string RunName { get; set; }
            public CacheStatistics Stats { get; set; }

            public void Display()
            {
                Console.WriteLine("=========================");
                Console.WriteLine(string.Format("{0} : {1}", this.RunName, this.Success));
                Console.WriteLine(this.Stats.ToString());
                Console.WriteLine("=========================");
                Console.WriteLine();

                File.WriteAllText(this.RunName + ".csv", string.Join(System.Environment.NewLine, this.Stats.Timings.Select(t => string.Format("{0};{1}", t.Item1, t.Item2))));
            }
        }

        private Stopwatch watcher;
        private CacheStatistics stats;

        private const string CACHE_KEY = "myitem";
        private const int CACHE_TTL = 5;

        public Runner()
        {
            this.watcher = new Stopwatch();
        }

        public RunResult Run(ICacheStrategy strategy)
        {
            var tokenSource = new CancellationTokenSource();
            this.stats = new CacheStatistics();

            //this is the sample method
            Action stuff = () =>
            {
                strategy.Get<DateTime>(CACHE_KEY, () =>
                {
                    Interlocked.Increment(ref this.stats.reloads);
                    return Task.Delay(1000).ContinueWith(t => { return DateTime.UtcNow; }).Result;
                }, CACHE_TTL);
            };

            var wks = this.CreateWorkers(stuff, tokenSource.Token, 30);

            //preload : avoid cold-start
            stuff();
            watcher.Restart();

            var runTask = this.RunAsync(wks, tokenSource, 60);

            runTask.Wait();

            watcher.Stop();
            return new RunResult()
            {
                Success = runTask.Status == TaskStatus.RanToCompletion,
                RunName = strategy.GetType().Name,
                Stats = this.stats
            };
        }

        private List<Task> CreateWorkers(Action stuff, CancellationToken token, int nbWorkers = 15)
        {
            var tasks = new List<Task>();
            foreach (var i in Enumerable.Range(1, nbWorkers))
            {
                Task task = new Task(
                    () =>
                    {
                        var random = new Random();
                        while (true)
                        {
                            if (token.IsCancellationRequested)
                                return;

                            long t1 = watcher.ElapsedMilliseconds;
                            stuff();
                            long t2 = watcher.ElapsedMilliseconds;
                            //stats
                            Interlocked.Increment(ref this.stats.gets);
                            this.stats.Timings.Add(Tuple.Create<long, long>(t1, t2 - t1));
                            //wait a little
                            Thread.Sleep(random.Next(50, 200));
                        };

                    }, token, TaskCreationOptions.LongRunning);
                tasks.Add(task);
            }

            return tasks;
        }

        private async Task RunAsync(List<Task> tasks, CancellationTokenSource tokenSource, int timeout = 30)
        {
            tasks.ForEach(t => t.Start());
            var res = Task.WaitAll(tasks.ToArray(), timeout * 1000, tokenSource.Token);
            if (!res)
            {
                tokenSource.Cancel();
                //wait cancellation
                while (!tasks.All(t => t.Status == TaskStatus.Canceled || t.Status == TaskStatus.RanToCompletion))
                {
                    await Task.Delay(500);
                }
            }
        }
    }
}
