namespace MazerPlatformer
{
    public class ConditionNotSatisfied : IFailure
    {
        public ConditionNotSatisfied(string caller)
        {
            Reason = $"Condition not satisfied. Caller: {caller}";
        }
        public string Reason { get; set; }
    }
}