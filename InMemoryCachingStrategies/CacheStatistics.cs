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
            builder.AppendFormat("reloads :{0}, gets: {1}", reloads, gets);
            builder.AppendLine();
            if (this.Timings != null && this.Timings.Count > 0)
            {
                var all = Timings.Where(i=>i!=null).Select(i=>i.Item2);
                builder.AppendFormat("avg : {0}, min:{1}, max:{2}", all.Average(), all.Min(), all.Max());
                builder.AppendLine();
                builder.AppendFormat("50 : {0}, 500:{1}, 1100:{2}", all.Where(t => t < 50).Count(), all.Where(t => t < 500).Count(), all.Where(t => t < 1100).Count());
            }
            return builder.ToString();
        }
    }
}
