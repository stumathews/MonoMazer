using System.Collections.Generic;
using LanguageExt;
using MazerPlatformer;

namespace ImmutableTests
{
    public static class PersonStatics
    {
        public static Version<Person> With(this Version<Person> version, Option<int> age, Option<string> name, Option<List<Item>> items, Option<List<Item>> immutableItems)
        {
            return version.Resolve().AssignmentModify(age, name, items, immutableItems);
        }

        public static Either<IFailure, Unit> Unlock(this Version<Person> version)
        {
            return version.Resolve().Unlock(version);
        }

        public static Either<IFailure, Unit> Lock(this Version<Person> version)
        {
            return version.Resolve().Lock(version);
        }
    }
}