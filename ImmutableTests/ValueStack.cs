using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using MazerPlatformer;

namespace ImmutableTests
{

    public interface IValueStack
    {
        void SetLatestPointer(int pointer);
        int GetLatestPointer();
    }

    public class ValueStack<T> : IValueStack
    {
        readonly List<T> _values = new List<T>();
        public int _latestPointer = 0;

        public int GetLatestPointer() => _latestPointer;

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
        private readonly bool _forceLockOnAccessIfUnlocked;
        private readonly bool _noForceOverwiteLock;

        public ValueStack2(bool forceLockOnAccessIfUnlocked = false, bool noForceOverwiteLock = false)
        {
            _forceLockOnAccessIfUnlocked = forceLockOnAccessIfUnlocked;
            _noForceOverwiteLock = noForceOverwiteLock;
        }

        public int Assign(T newItem)
        {
            _values.Add(newItem);
            _latestPointer = _values.Count - 1;
            return _latestPointer;
        }

        public Either<IFailure, Unit> Lock<T>(Version<T> version)
        {
            if (_locked && !VersionsAreSame(_lockedBy, version.Spec) && _noForceOverwiteLock)
                return NotCheckedOut.Create("NoForceOverwiteLock enabled: must explicitly unlock before locking").ToEitherFailure<Unit>();
            // Take over ownership of the access to stack
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
            // If unlocked then the first process to access the field will aquire the lock
            if (!_locked && _forceLockOnAccessIfUnlocked)
                Lock(currentVersion);

            return _locked && !VersionsAreSame(currentVersion.Spec, _lockedBy)
                ? NotCheckedOut.Create($"Locked by {_lockedBy}").ToEitherFailure<T>()
                : _values[_latestPointer];
        }

    }

    public class ValueStack3<T> : IValueStack
    {
        readonly List<T> _values = new List<T>();
        private int _latestPointer = 0;
        private bool _locked;
        private (int, string)[] _lockedBy;
        private Option<StackOptions> _options;

        public ValueStack3(Option<StackOptions> options)
        {
            _options = options;
        }

        public int Assign(T newItem)
        {
            _values.Add(newItem);
            _latestPointer = _values.Count - 1;
            return _latestPointer;
        }

        public Either<IFailure, Unit> Lock<T>(Version3<T> version)
        {
            if (_locked && !VersionsAreSame(_lockedBy, version.Spec) && _options.Map(options => options.NoForceOverwriteLock)
                                                                                       .Match(Some: b => b, None: () => false))
                return NotCheckedOut.Create("NoForceOverwiteLock enabled: must explicitly unlock before locking").ToEitherFailure<Unit>();
            // Take over ownership of the access to stack
            _lockedBy = version.Spec;
            _locked = true;
            return new Unit();
        }

        public Either<IFailure, Unit> Lock2((int, string)[] spec)
        {
            if (_locked && !VersionsAreSame(_lockedBy, spec) && _options.Map(options => options.NoForceOverwriteLock)
                .Match(Some: b => b, None: () => false))
                return NotCheckedOut.Create("NoForceOverwiteLock enabled: must explicitly unlock before locking").ToEitherFailure<Unit>();
            // Take over ownership of the access to stack
            _lockedBy = spec;
            _locked = true;
            return new Unit();
        }

        private bool VersionsAreSame((int, string)[] one, (int,string)[] two)
        {
            return one.SequenceEqual(two);
        }

        public Either<IFailure, Unit> Unlock<T>(Version3<T> versionKey)
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

        public int GetLatestPointer()
        {
            return _latestPointer;
        }

        public Either<IFailure, T> Read(int pointer)
            => _values[pointer];

        public Either<IFailure, T> ReadLatest((int, string)[] spec)
        {
            // If unlocked then the first process to access the field will aquire the lock
            if (!_locked && _options.Map(options => options.ForceLockOnAccessIfUnlocked).Match(Some: b => b, None: () => false))
                Lock2(spec);

            return _locked && !VersionsAreSame(spec, _lockedBy)
                ? NotCheckedOut.Create($"Locked by {_lockedBy}").ToEitherFailure<T>()
                : _values[_latestPointer];
        }

        public Either<IFailure, T> ReadLatest2((int, string)[] spec)
        {
            // If unlocked then the first process to access the field will aquire the lock
            if (!_locked && _options.Map(options => options.ForceLockOnAccessIfUnlocked).Match(Some: b => b, None: () => false))
                Lock2(spec);

            return _locked && !VersionsAreSame(spec, _lockedBy)
                ? NotCheckedOut.Create($"Locked by {_lockedBy}").ToEitherFailure<T>()
                : _values[_latestPointer];
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