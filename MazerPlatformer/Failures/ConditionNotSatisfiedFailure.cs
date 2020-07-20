namespace MazerPlatformer
{
    public class ConditionNotSatisfiedFailure : IFailure
    {
        public ConditionNotSatisfiedFailure(string caller)
        {
            Reason = $"Condition not satisfied. Caller: {caller}";
        }
        public string Reason { get; set; }
        public static IFailure Create(string caller) => new ConditionNotSatisfiedFailure(caller);
    }
}