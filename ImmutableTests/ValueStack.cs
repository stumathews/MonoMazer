using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using MazerPlatformer;

namespace ImmutableTests
{
    public class ValueStack<T>
    {
        readonly List<T> _values = new List<T>();
        private int _latestPointer = 0;

        public int Assign(T newItem)
        {
            _values.Add(newItem);
            _latestPointer = _values.Count-1;
            return _latestPointer;
        }

        public int Assign() 
            => _latestPointer;

        public void SetLatestPointer(int pointer) 
            => _latestPointer = pointer;

        public T Read(int pointer)
            => _values[pointer];

        public T ReadLatest() 
            => _values[_latestPointer];
    }

    public class ValueStack2<T>
    {
        readonly List<T> _values = new List<T>();
        private int _latestPointer = 0;
        private bool _locked;
        private int[] _lockedBy;

        public int Assign(T newItem)
        {
            _values.Add(newItem);
            _latestPointer = _values.Count - 1;
            return _latestPointer;
        }

        public Either<IFailure, Unit> Lock<T>(Version<T> version)
        {
            //if (_locked)
              //  return LockedFailure.Create($"Already Locked").ToEitherFailure<Unit>();

            _lockedBy = version.Spec;
            _locked = true;
            return new Unit();
        }

        private bool VersionsAreSame(int[] one, int[] two)
        {
            return one.SequenceEqual(two);
        }

        public Either<IFailure, Unit> Unlock<T>(Version<T> versionKey)
        {
            if (_locked && VersionsAreSame(_lockedBy, versionKey.Spec))
            {
                // same
                _locked = false;
                return new Unit();
            }

            // Can't unlock it if you don't own it
            return LockedFailure.Create($"Locked by {_lockedBy}").ToEitherFailure<Unit>();
        }

        public int Assign()
            => _latestPointer;

        public void SetLatestPointer(int pointer)
            => _latestPointer = pointer;

        public Either<IFailure, T> Read(int pointer)
            => _values[pointer];

        public Either<IFailure,T> ReadLatest(Version<Person> currentVersion)
        {
            var result = _locked && !VersionsAreSame(currentVersion.Spec, _lockedBy)
                ? NotCheckedOut.Create("Locked").ToEitherFailure<T>()
                : _values[_latestPointer];
            return result;
        }

    }

    public class LockedFailure : IFailure
    {
        private LockedFailure(string reason)
        {
            Reason = reason;
        }
        public string Reason { get; set; }
        public static IFailure Create(string reason) => new LockedFailure(reason);
    }
}