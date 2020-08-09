namespace Tests
{
    public interface IBuilder<T>
    {
        public T Build(int[] spec);
    }
}