namespace ImmutableTests
{
    public interface IBuilder<T>
    {
         T Build(int[] spec);
    }
}