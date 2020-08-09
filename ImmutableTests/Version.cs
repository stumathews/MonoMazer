using System;
using System.Linq;

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
            => _spec.GetHashCode();

        // Build the version of the object
        public T Resolve() => _builder.Build(_spec);
    }

    public class Version2<T>
    {
        // This represents the version of the object
        private readonly (int, string)[] _spec;
        private readonly IBuilder2<T> _builder;

        public Version2((int, string)[] spec, IBuilder2<T> builder)
        {
            _spec = spec;
            _builder = builder;
        }

        public override string ToString() 
            => string.Join(",", Spec.Select((tuple, i) => $"{tuple.Item2}=>{tuple.Item1}"));

        public (int, string)[] Spec => _spec;
        
        // Build the version of the object
        public T Resolve() 
            => _builder.Build(_spec);
    }

    public class Version3<T>
    {
        // This represents the version of the object
        private readonly (int, string)[] _spec;
        private readonly IBuilder2<T> _builder;

        public Version3((int, string)[] spec, IBuilder2<T> builder)
        {
            _spec = spec;
            _builder = builder;
        }

        public override string ToString()
            => string.Join(",", Spec.Select((tuple, i) => $"{tuple.Item2}=>{tuple.Item1}"));

        public (int, string)[] Spec => _spec;

        // Build the version of the object
        public T Resolve()
            => _builder.Build(_spec);
    }
}