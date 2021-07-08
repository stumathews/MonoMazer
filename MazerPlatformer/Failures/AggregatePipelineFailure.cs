using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MazerPlatformer
{
    public class AggregatePipelineFailure : IFailure
    {
        public AggregatePipelineFailure(IEnumerable<IFailure> failures)
        {
            var failureNames = failures.GroupBy(o => o.GetType().Name + o.Reason);
            var sb = new StringBuilder();
            foreach (var name in failureNames)
            {
                sb.Append($"Failure name: {name.Key} Count: {name.Count()}\n");
            }

            Reason = sb.ToString();
        }
        public string Reason { get; set; }
        public static IFailure Create(IEnumerable<IFailure> failures) => new AggregatePipelineFailure(failures);
    }
}