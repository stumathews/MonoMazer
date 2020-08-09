using System;

namespace ImmutableTests
{
    public interface IBuilder<T>
    {
         T Build(int[] spec);
    }

    public interface IBuilder2<T>
    {
        T Build((int, string)[] spec);
    }
}