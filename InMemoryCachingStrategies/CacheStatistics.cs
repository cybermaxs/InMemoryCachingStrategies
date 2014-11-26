using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InMemoryCachingStrategies
{
    /// <summary>
    /// Used to keep run statistics.
    /// </summary>
    public class CacheStatistics
    {
        public List<Tuple<long, long>> Timings = new List<Tuple<long, long>>(5000);     //all timings
        public long reloads;    //number of reloads
        public long gets;      //number of gets

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("Number of reloads :{0}, gets: {1}", reloads, gets);
            builder.AppendLine();
            if (this.Timings != null && this.Timings.Count > 0)
            {
                var all = Timings.Where(i=>i!=null).Select(i=>i.Item2);
                builder.AppendFormat("avg : {0:N2} ms, min:{1}, max:{2:}", all.Average(), all.Min(), all.Max());
                builder.AppendLine();

                var ms = 100;
                var current = 0D;
                while (current!=1D)
                {
                    current = this.NbLowerThan(all, ms);
                    builder.AppendFormat("{0:P2} <= {1} ms", current, ms);
                    builder.AppendLine();
                    ms+=100;
                }
            }
            return builder.ToString();
        }

        private double NbLowerThan(IEnumerable<long> sequence, long value)
        {
            var res = (double)sequence.Where(s => s <= value).Count() / (double)sequence.Count();
            return res;
        }

        private long Median(IEnumerable<long> sequence)
        {
            var res = sequence.OrderBy(s => s).ElementAt(sequence.Count() / 2);
            return res;
        }

        private double Percentile(long[] sequence, double excelPercentile)
        {
            Array.Sort(sequence);
            int N = sequence.Length;
            double n = (N - 1) * excelPercentile + 1;
            // Another method: double n = (N + 1) * excelPercentile;
            if (n == 1d) return sequence[0];
            else if (n == N) return sequence[N - 1];
            else
            {
                int k = (int)n;
                double d = n - k;
                return sequence[k - 1] + d * (sequence[k] - sequence[k - 1]);
            }
        }
    }
}
