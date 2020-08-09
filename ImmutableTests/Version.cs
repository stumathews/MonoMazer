namespace ImmutableTests
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

        public int[] Spec => _spec;

        protected bool Equals(Version<T> other)
        {
            return _spec.Equals(other._spec);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Version<T>) obj);
        }

        public override int GetHashCode()
        {
            return _spec.GetHashCode();
        }

        // Build the version of the object
        public T Resolve() => _builder.Build(_spec);
    }
}