namespace MazerPlatformer
{
    public class ConditionNotSatisfied : IFailure
    {
        public ConditionNotSatisfied(string caller)
        {
            Reason = $"Condition not satisfied. Caller: {caller}";
        }
        public string Reason { get; set; }
        public static IFailure Create(string caller) => new ConditionNotSatisfied(caller);
    }
}