using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LanguageExt;
using MazerPlatformer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static LanguageExt.Prelude;

namespace ImmutableTests
{

    public class NotCheckedOut : IFailure
    {
        private NotCheckedOut(string reason)
        {
            Reason = reason;
        }
        public string Reason { get; set; }
        public static IFailure Create(string reason) => new NotCheckedOut(reason);
    }

    public class Person
    {
        /* Fields backed by static ValueStack that contain shared data */
        internal static ValueStack<int> _age = new ValueStack<int>();
        public int Age
        {
            get => _age.ReadLatest();
            set => _age.Assign(value);
        }

        internal static ValueStack<string> _name = new ValueStack<string>();
        public string Name
        {
            get => _name.ReadLatest();
            set => _name.Assign(value);
        }

        private static readonly ValueStack<List<Item>> _items = new ValueStack<List<Item>>();
        public List<Item> Items
        {
            get => _items.ReadLatest();
            set => _items.Assign(value);
        }

        /// <summary>
        /// Restricts read-access to field if the field is locked, only giving access to version that locked
        /// </summary>
        private static readonly ValueStack2<List<Item>> _immutableItems = new ValueStack2<List<Item>>();
        public Either<IFailure, List<Item>> ImmutableItems
        {
            get => _immutableItems.ReadLatest(GetCurrentVersion());
            set => value.Map(items =>
            {
                _immutableItems.Assign(items);
                _immutableItems.Lock(GetCurrentVersion());
                return new Unit();
            });
        }

        /// <summary>
        /// Create a new version of a person
        /// </summary>
        /// <param name="age"></param>
        /// <param name="name"></param>
        /// <param name="items"></param>
        /// <param name="immutableItems"></param>
        /// <returns>A version of the person with the specified attributes</returns>
        public static Version<Person> Create(int age, string name, List<Item> items, List<Item> immutableItems)
        {
            _age.Assign(age);
            _name.Assign(name);
            _items.Assign(items);
            _immutableItems.Assign(immutableItems);

            var currentVersion = GetCurrentVersion();

            _immutableItems.Lock(currentVersion);

            return GetCurrentVersion();
        }

        public Either<IFailure, Unit> Unlock<T>(Version<T> versionKey)
        {
            return _immutableItems.Unlock(versionKey);
        }
        public Either<IFailure, Unit> Lock<T>(Version<T> versionKey)
        {
            return _immutableItems.Lock(versionKey);
        }

        public static Version<Person> GetCurrentVersion()
        {
            return new Version<Person>(MakeSpecFrom(_age.Assign(), _name.Assign(), _items.Assign(), _immutableItems.Assign()), new PersonBuilder());
        }

        /// <summary>
        /// Assign a new attribute value to an existing person (AssignmentModify)
        /// </summary>
        /// <param name="newAge"></param>
        /// <param name="newName"></param>
        /// <param name="newItems"></param>
        /// <remarks>Shares data that was not modified. Data not modified is represented by None</remarks>
        /// <returns>A version that represents the modification</returns>
        public Version<Person> AssignmentModify(Option<int> newAge, Option<string> newName, Option<List<Item>> newItems, Option<List<Item>> newImmutableItems)
        {
            var internalAge = newAge.Match(Some: age => _age.Assign(age), 
                                               None:() => _age.Assign());
            var internalName = newName.Match(Some: name => _name.Assign(name), 
                                                 None: () => _name.Assign());
            var internalItems = newItems.Match(Some:items => _items.Assign(items),
                                                   None: () => _items.Assign());

            var internalImmutableItems = newImmutableItems.Match(Some: items => _items.Assign(items),
                None: () => _immutableItems.Assign());
            _immutableItems.Lock(GetCurrentVersion());

            return new Version<Person>(MakeSpecFrom(/*0*/internalAge, /*1*/ internalName, /*2*/internalItems, /*3*/ internalImmutableItems), new PersonBuilder());
        }


        private static int[] MakeSpecFrom(params int[] indices) 
            => indices;

        // Some big modification exercise
        public Version<Person> BlankIt() 
            => AssignmentModify(0, "", null, null);

        /// <summary>
        /// Builds a Person from a version specification
        /// </summary>
        public class PersonBuilder : IBuilder<Person>
        {
            public Person Build(int[] spec)
            {
                _age.SetLatestPointer(spec[0]);
                _name.SetLatestPointer(spec[1]);
                _items.SetLatestPointer(spec[2]);
                _immutableItems.SetLatestPointer(spec[3]);
                return new Person(); ;
            }
        }


    }

    public  class Item
    {
        private string _name;
        private int _value;

        public Item(string name, int value)
        {
            _name = name;
            _value = value;
        }
    }

    [TestClass]
    public partial class ValueStackTests
    {

        [TestMethod]
        public void BasicTest()
        {
            var stuartItems = new List<Item>
            {
                new Item("Calculator", 22),
                new Item("Ruler", 1)
            };

            var itemsWithKnife = new List<Item>
            {
                new Item("Calculator", 22),
                new Item("Ruler", 1),
                new Item("Knife", 55)
            };

            var defaultAge = 33;
            var defaultName = "stuart";
            var defaultItems = stuartItems;
            var fullname = "Stuart Robert Charles";

            Version<Person> stuart = Person.Create(defaultAge, defaultName, defaultItems, new List<Item>());
            Version<Person> nextYearStuart = stuart.With(34, None, None, None);
            Version<Person> fullNameStuart = stuart.With(None, fullname, None, None);
            Version<Person> stuartWithKnife = stuart.With(None, null, itemsWithKnife, None); // Note how a null is converted to an Option as a Option<T>.None:
            Version<Person> blankVersion = stuart.Resolve().BlankIt();
            
            // Original can resolve correctly
            Assert.AreEqual(defaultAge, stuart.Resolve().Age);
            Assert.AreEqual(defaultName, stuart.Resolve().Name);
            Assert.AreEqual(defaultItems, stuart.Resolve().Items);
            Assert.AreEqual(defaultItems.Count, stuart.Resolve().Items.Count);

            // Check new changes
            Assert.AreEqual(nextYearStuart.Resolve().Age, stuart.Resolve().Age +1);
            Assert.AreEqual(nextYearStuart.Resolve().Name, stuart.Resolve().Name);
            Assert.AreEqual(nextYearStuart.Resolve().Items, stuart.Resolve().Items);

            // Check for more new changes
            Assert.AreEqual(fullname, fullNameStuart.Resolve().Name);
            Assert.AreEqual(blankVersion.Resolve().Age, 0);
            Assert.AreEqual(itemsWithKnife.Count, stuartWithKnife.Resolve().Items.Count);

            // Original version still has not changed
            Assert.AreEqual(defaultAge, stuart.Resolve().Age);
            Assert.AreEqual(defaultName, stuart.Resolve().Name);
            Assert.AreEqual(defaultItems, stuart.Resolve().Items);
            Assert.AreEqual(defaultItems.Count, stuart.Resolve().Items.Count);

        }

        [TestMethod]
        public void ExclusivityTests()
        {

            var list1 = new List<Item> { new Item("Keyboard", 55) };
            var list2 = new List<Item> { new Item("Fish", 22), new Item("Fish Tank", 100) };
            var list3 = new List<Item> { new Item("armour", 100), new Item("Sword", 170), new Item("Helmet", 130) };
            var immutableList = new List<Item> {new Item("ring", 1000)};

            // Lock immutable list on create
            var stuart = Person.Create(33, "Stuart", list1, immutableList);
            
            // Create a new version, locks immutable list for only new version (earlier versions can't write to it)
            var stuart1 = stuart.With(34, Option<string>.None, Option<List<Item>>.None, None);
           
            // ... locks for only new version
            var stuart2 = stuart.With(35, Option<string>.None, list2, None);
            // ... locks for only new version
            var stuart3 = stuart.With(36, Option<string>.None, list3, None);

            // original version no longer has access to it and cannot thus modify it the underlying data that subsequent versions rely on
            Assert.AreEqual(true, stuart.Resolve().ImmutableItems.IsLeft);

            // Current version should be able to access the data now
            Assert.AreEqual(false, stuart3.Resolve().ImmutableItems.IsLeft);

            // Original version cannot unlock it, its already owned by subsequent version
            Assert.AreEqual(true, stuart.Unlock().IsLeft);

            // still locked
            Assert.AreEqual(true, stuart.Resolve().ImmutableItems.IsLeft);
            // now let me change it

            // As the current owner of the lock I can access it
            Assert.AreEqual(false, stuart3.Resolve().ImmutableItems.IsLeft);

            // Owner should be able to unlock it now
            Assert.AreEqual(false, stuart3.Unlock().IsLeft);
            // And read it
            Assert.AreEqual(false, stuart3.Resolve().ImmutableItems.IsLeft);

            // Now can the orignal version access it as its still unlocked
            Assert.AreEqual(false, stuart.Resolve().ImmutableItems.IsLeft);
             
            // Convention: if you're going to write to the values, lock access
            // Ask for exclusive access, as we're going to modify next line
            Assert.AreEqual(false, stuart.Lock().IsLeft);
           
            stuart.Resolve().ImmutableItems.Map(items =>
            {
                items.Add(new Item("Crown", 2000));
                return new Unit();
            });

            // Should now be locked to orignal again as on access the lock was aquired
            Assert.AreEqual(false, stuart.Resolve().ImmutableItems.IsLeft);
            

            // Make sure stuart3 cant access it because lock is still unlocked (loophole circumvented aquiring lock)
            Assert.AreEqual(true, stuart3.Resolve().ImmutableItems.IsLeft);

            // Assert

            Assert.AreEqual(list1.Count, stuart.Resolve().Items.Count);
            Assert.AreEqual(list1.Count, stuart1.Resolve().Items.Count);

            Assert.AreEqual(list2.Count, stuart2.Resolve().Items.Count);
            Assert.AreEqual(list3.Count, stuart3.Resolve().Items.Count);
            Assert.AreEqual(36, stuart3.Resolve().Age);

        }


        [TestMethod]
        public void FieldBuilderTests()
        {
            // Setup a new Object and define its initial version-able fields and values
            Version2<NewObject> initialVersion = NewObject.Create(
                ("age", 33),
                ("name", "stuart"),
                ("money", 22.5f));

            // Fetching them ok?
            Assert.AreEqual(33, initialVersion.Resolve().Get<int>("age"));
            Assert.AreEqual("stuart", initialVersion.Resolve().Get<string>("name"));
            Assert.AreEqual(22.5, initialVersion.Resolve().Get<float>("money"));

            // Make a new version
            var nextVersion2 = initialVersion.Resolve().SetMany(("age", 100), ("name", "stuart mathews"), ("money", 44.8f));

            // Fecthing them ok
            Assert.AreEqual(100, nextVersion2.Resolve().Get<int>("age"));
            Assert.AreEqual("stuart mathews", nextVersion2.Resolve().Get<string>("name"));
            Assert.AreEqual(44.8f, nextVersion2.Resolve().Get<float>("money"));

            // Still can fetch the orignal ok?
            Assert.AreEqual(33, initialVersion.Resolve().Get<int>("age"));
            Assert.AreEqual("stuart", initialVersion.Resolve().Get<string>("name"));
            Assert.AreEqual(22.5, initialVersion.Resolve().Get<float>("money"));

            // Should be able to make a lockable field
            Assert.AreEqual(false, nextVersion2.Resolve().MakeLockableField("age", new StackOptions()).IsLeft);
            var result = initialVersion.Resolve().SetMany(("age", 0));
            
            // This version should not be able to set it;
            Assert.AreEqual(true, nextVersion2.Resolve().GetImmutable<int>("age").IsLeft);

            // Originator should be able to set it...todo
        }

    }

    public class StackOptions
    {
        private bool IsLockableField { get; set; }
    }
}
