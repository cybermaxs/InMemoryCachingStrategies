using InMemoryCachingStrategies.Strategy;
using System;
using System.Collections.Generic;

namespace InMemoryCachingStrategies
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("*** In memory caching stategy benchmarks ***");

            Console.WriteLine("Press any key to start...");
            Console.ReadKey();

            var runner = new Runner();
            var strategies = new List<ICacheStrategy>();
            strategies.Add(new BasicCacheStrategy());
            strategies.Add(new SimpleLockCacheStrategy());
            strategies.Add(new DoubleLockCacheStrategy());
            strategies.Add(new RefreshAheadCacheStrategy());

            foreach (var strategy in strategies)
            {
                var res = runner.Run(strategy);
                res.Display();
            }

            Console.WriteLine("DONE");
            Console.ReadKey();
            Console.WriteLine("Press any key to exit");

        }
    }
}
