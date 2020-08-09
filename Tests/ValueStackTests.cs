using System.Collections;
using System.Collections.Generic;
using LanguageExt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static LanguageExt.Prelude;

namespace Tests
{
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
        /// Create a new version of a person
        /// </summary>
        /// <param name="age"></param>
        /// <param name="name"></param>
        /// <param name="items"></param>
        /// <returns>A version of the person with the specified attributes</returns>
        public static Version<Person> Create(int age, string name, List<Item> items)
        {
            _age.Assign(age);
            _name.Assign(name);
            _items.Assign(items);

            return new Version<Person>(MakeSpecFrom(_age.Assign(), _name.Assign(), _items.Assign()), new PersonBuilder());
        }

        /// <summary>
        /// Modify an existing person (Mutate)
        /// </summary>
        /// <param name="newAge"></param>
        /// <param name="newName"></param>
        /// <param name="newItems"></param>
        /// <remarks>Shares data that was not modified. Data not modified is represented by None</remarks>
        /// <returns>A version that represents the modification</returns>
        public Version<Person> Modify(Option<int> newAge, Option<string> newName, Option<List<Item>> newItems)
        {
            var internalAge = newAge.Match(Some: age => _age.Assign(age), 
                                               None:() => _age.Assign());
            var internalName = newName.Match(Some: name => _name.Assign(name), 
                                                 None: () => _name.Assign());
            var internalItems = newItems.Match(Some:items => _items.Assign(items),
                                                   None: () => _items.Assign());

            return new Version<Person>(MakeSpecFrom(/*0*/internalAge, /*1*/ internalName, /*2*/internalItems), new PersonBuilder());
        }


        private static int[] MakeSpecFrom(params int[] indices) 
            => indices;

        // Some big modification exercise
        public Version<Person> BlankIt() 
            => Modify(0, "", null);

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
    public class ValueStackTests
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

            Version<Person> stuart = Person.Create(defaultAge, defaultName, defaultItems);
            Version<Person> nextYearStuart = stuart.With(34, None, None);
            Version<Person> fullNameStuart = stuart.With(None, fullname, None);
            Version<Person> stuartWithKnife = stuart.With(None, null, itemsWithKnife); // Note how a null is converted to an Option as a Option<T>.None:
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

    }
}
