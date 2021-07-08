namespace MazerPlatformer
{
    /// <summary>
    /// Represents a failure for some reason
    /// </summary>
    public interface IFailure
    {
        /// <summary>
        /// Nature of the failure
        /// </summary>
        string Reason { get; set; }
    }
}
