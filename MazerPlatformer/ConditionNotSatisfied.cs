namespace MazerPlatformer
{
    public class ConditionNotSatisfied : IFailure
    {
        public ConditionNotSatisfied()
        {
            Reason = "Condition not satisfied";
        }
        public string Reason { get; set; }
    }
}