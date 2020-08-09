namespace Tests
{
    /// <summary>
    /// Represents a version of an Object. That object which can be resolved.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Version<T>
    {
        // This represents the version of the object
        private readonly int[] _spec;
        private readonly IBuilder<T> _builder;

        public Version(int[] spec, IBuilder<T> builder)
        {
            _spec = spec;
            _builder = builder;
        }

        // Build the version of the object
        public T Resolve() => _builder.Build(_spec);
    }
}