using System.Collections.Generic;
using LanguageExt;

namespace Tests
{
    public static class PersonStatics
    {
        public static Version<Person> With(this Version<Person> version, Option<int> age, Option<string> name, Option<List<Item>> items)
        {
            return version.Resolve().Modify(age, name, items);
        }
    }
}