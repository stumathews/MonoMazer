using LanguageExt;

namespace MazerPlatformer
{
    internal class UninitializedFailure : IFailure
    {
        public UninitializedFailure(string what)
        {
            Reason = what;
        }
        public string Reason { get; set; }
        public static IFailure Create(string what) => new UninitializedFailure(what);
        public static Either<IFailure, T> Create<T>(string what) => new UninitializedFailure(what);
    }
}