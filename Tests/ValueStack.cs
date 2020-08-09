using System.Collections.Generic;

namespace Tests
{
    public class ValueStack<T>
    {
        readonly List<T> values = new List<T>();
        private int _latestPointer = 0;

        public int Assign(T newItem)
        {
            values.Add(newItem);
            _latestPointer = values.Count-1;
            return _latestPointer;
        }

        public int Assign()
        {
            return _latestPointer;
        }

        public void SetLatestPointer(int pointer)
        {
            _latestPointer = pointer;
        }

        public T Read(int pointer)
        {
            return values[pointer];
        }

        public T ReadLatest()
        {
            return values[_latestPointer];
        }
    }
}