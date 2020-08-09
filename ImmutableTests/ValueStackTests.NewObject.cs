using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using LanguageExt;
using MazerPlatformer;

namespace ImmutableTests
{
    public partial class ValueStackTests
    {
        public class NewObject
        {
            static OrderedDictionary _stacks = new OrderedDictionary();

            public static int AddStackWithInitialValue<T>((string name, T value) attribute)
            {
                _stacks.Add(attribute.name, new StackDescriptor(new ValueStack<T>(), typeof(T), Option<StackOptions>.None)); ;
                return GetValueStack<T>(attribute.name).Assign(attribute.value);
            }

            private static ValueStack<T> GetValueStack<T>(string name)
            {
                var stackDescriptor = _stacks[name] as StackDescriptor;
                ValueStack<T> stack = stackDescriptor.valueStack as ValueStack<T>;
                return stack;
            }

            private static ValueStack3<T> GetValueStack3<T>(string name)
            {
                var stackDescriptor = _stacks[name] as StackDescriptor;
                ValueStack3<T> stack = stackDescriptor.valueStack as ValueStack3<T>;
                return stack;
            }
            public T Get<T>(string name) 
                => (T) GetValueStack<object>(name).ReadLatest();

            public int Set<T>(string name, T value) 
                => GetValueStack<object>(name).Assign(value);

            public int Set<T>((string name, T value) pair) 
                => GetValueStack<T>(pair.name).Assign(pair.value);

            
            //private static readonly ValueStack2<List<Item>> _immutableItems = new ValueStack2<List<Item>>();
            //public Either<IFailure, List<Item>> ImmutableItems
            //{
            //    get => _immutableItems.ReadLatest(GetCurrentVersion());
            //    set => value.Map(items =>
            //    {
            //        _immutableItems.Assign(items);
            //        _immutableItems.Lock(GetCurrentVersion());
            //        return new Unit();
            //    });
            //}

            public Either<IFailure, object> GetImmutable<T>(string name)
            {
                return GetValueStack3<object>(name).ReadLatest(GetCurrentSpec());
            }

            public Version3<NewObject> SetMany(params (string name, object value)[] valueStacks)
            {
                var spec = new (int, string)[_stacks.Count];
                for (var i = 0; i < _stacks.Count; i++)
                {
                    string[] keys = new string[_stacks.Count];
                    var stackDescriptor = _stacks[i] as StackDescriptor;
                    bool isLockable = stackDescriptor._options.IsSome;

                    _stacks.Keys.CopyTo(keys, 0);

                    // Loop through the stacks we've been asked to set
                    if (i < valueStacks.Length)
                    {
                        var (name, value) = valueStacks[i];
                        spec[i] = isLockable 
                            ? ((GetValueStack3<object>(name).Assign(value), name))
                            : (GetValueStack<object>(name).Assign(value), name);
                    }
                    else
                    {
                        // And include the existing stacks that were not set but should be included in the version spec
                        spec[i] = isLockable
                            ? (GetValueStack3<object>(keys[i]).Assign(), keys[i]) 
                            :(GetValueStack<object>(keys[i]).Assign(), keys[i]);
                    }

                    if (isLockable)
                        GetValueStack3<object>(keys[i]).Lock(new Version3<NewObject>(spec, new NewObjectBuilder()));
                }

                return new Version3<NewObject>(spec, new NewObjectBuilder());
                
            }

            public (int, string)[] GetCurrentSpec()
            {
                var spec = new (int, string)[_stacks.Count];
                for (var i = 0; i < _stacks.Count; i++)
                {
                    string[] keys = new string[_stacks.Count];
                    _stacks.Keys.CopyTo(keys, 0);
                    var stackDescriptor = _stacks[i] as StackDescriptor;
                    spec[i] = ((stackDescriptor.valueStack as IValueStack).GetLatestPointer(), keys[i]);
                }

                return spec;
            }

            public static void PurgeCache() => _stacks.Clear();

            public static Version2<NewObject> Create(params (string name, object value)[] valueStacks)
            {
                if(_stacks.Count > 0)
                    PurgeCache();

                (int,string)[] spec = new (int, string)[valueStacks.Length];
                for (var index = 0; index < valueStacks.Length; index++)
                {
                    var valueStack = valueStacks[index];
                    spec[index] = (AddStackWithInitialValue(valueStack), valueStack.name);
                }

                return new Version2<NewObject>(spec, new NewObjectBuilder());
            }

            public class NewObjectBuilder : IBuilder2<NewObject>
            {
                public NewObject Build((int, string)[] spec)
                {
                    for (int i = 0; i < spec.Length; i++)
                    {
                        var stackDescriptor = _stacks[spec[i].Item2] as StackDescriptor;
                        var stack = stackDescriptor.valueStack as IValueStack;
                        stack.SetLatestPointer(spec[0].Item1);
                    }
                    return new NewObject();
                }
            }

            public Either<IFailure, Unit> MakeLockableField(string name, StackOptions stackOptions)
            {
                
                var stackDescriptor = _stacks[name] as StackDescriptor;
                if (stackDescriptor._options.IsSome)
                {
                    // Only lockable fields have options
                    return LockedFailure.Create("Already a lockable field").ToEitherFailure<Unit>();
                }
                // For now until out stack is orderd, we cant get them out in order so lets just repalce everything 
                // when we make a field loackable
                var newStack = new ValueStack3<object>();
                stackDescriptor.valueStack = newStack;
                stackDescriptor._options = stackOptions;
                return new Unit();
            }
        }
    }
}