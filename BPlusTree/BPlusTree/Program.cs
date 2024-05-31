using System;
using System.Linq;

namespace BPlusTree
{
    class Program
    {
        public static void Main(string[] args)
        {
            BPTreeList<int, string> tree =
                new BPTreeList<int, string>(Int32.Parse, internalNodeCapacity: 5, leafCapacity: 3);
            var s1 = 1;
            int count = 20;
            foreach (int _index in Enumerable.Range(s1, count))
            {
                if (_index % 2 == 0)
                {
                    var key = "a_" + _index;
                    var value = "1" + _index;
                    tree.Add(value);
                }
            }

            var index = 0;
            foreach (var value in tree)
            {
                Console.WriteLine("value1 index:{0}, Value={1} , Count={2}", index++, value, tree.Count);
            }

            Console.WriteLine("## IndexOf ######");
            foreach (int _index in Enumerable.Range(s1, count))
            {
                if (_index % 2 != 0)
                {
                    var key = "b_" + _index;
                    var value = "" + _index;
                    tree.Add(value);
                }
            }


            foreach (var value in tree)
            {
                var _index = tree.IndexOf(value);
                Console.WriteLine("value foreach index:{0}, Value={1} ", _index, value);
            }


            Console.WriteLine("### index #####");

            foreach (int _index in Enumerable.Range(0, tree.Count))
            {
                string value10 = tree[_index];
                Console.WriteLine("index index:{0}, Value={1} ", _index, value10);
            }


            string[] array1 = new string[tree.Count];
            tree.CopyTo(array1,0);
            Console.WriteLine("array :{0}", string.Join("\n",array1));
        }

        public static void Main1(string[] args)
        {
            BPTree<string, string> tree = new BPTree<string, string>();
            var s1 = 100;
            int count = 20;
            foreach (int _index in Enumerable.Range(s1, count))
            {
                if (_index % 2 == 0)
                {
                    var key = "a_" + _index;
                    var value = "a_" + _index;
                    tree.Add(key, value);
                }
            }

            var index = 0;
            foreach (var value in tree)
            {
                Console.WriteLine("value1 index:{0}, Value={1} ", index++, value);
            }

            Console.WriteLine("########");
            foreach (int _index in Enumerable.Range(s1, count))
            {
                if (_index % 2 != 0)
                {
                    var key = "b_" + _index;
                    var value = "a_" + _index;
                    tree.Add(key, value);
                }
            }


            index = 0;
            foreach (var value in tree)
            {
                Console.WriteLine("value2 index:{0}, Value={1} ", index++, value);
            }
        }

        public static void Main2(string[] args)
        {
            BPTree<int, string> WordLookup = new BPTree<int, string>.Builder().Build();
            // BTreeDictionary<string, string> WordLookup = new BTreeDictionary<string, string>();
            WordLookup.AddOrReplace(10, "Cool World!");
            WordLookup.AddOrReplace(10, "Cooler World!");
            WordLookup.AddOrReplace(9, "Fox!");
            WordLookup.AddOrReplace(8, "Bar");
            string v;
            if (WordLookup.TryGet(9, out v))
                Console.WriteLine("Key {1} was found! Value = {0}", v, 9);


            foreach (var keyValuePair in WordLookup.AsPairEnumerable())
            {
                Console.WriteLine("foreach Key:{0}, Value={1} ", keyValuePair.Item1, keyValuePair.Item2);
            }

            // WordLookup.Remove("Hello", out v);
            // WordLookup.Remove("Hello", out v);


            //
            // if (WordLookup.First)
            // {
            //     Console.WriteLine();
            //     Console.WriteLine("Words in Lookup:");
            //     do
            //     {
            //         Console.WriteLine("Key:{0}, Value={1} ", WordLookup.CurrentKey, WordLookup.CurrentValue);
            //     } while (WordLookup.MoveNext());
            // }

            // Console.ReadLine();
        }

        public static void Main3(string[] args)
        {
            BPTree<string, string> WordLookup = new BPTree<string, string>.Builder().Build();
            // BTreeDictionary<string, string> WordLookup = new BTreeDictionary<string, string>();
            WordLookup.Add("Hello", "Cool World!");
            WordLookup.Add("Hello1", "Cooler World!");
            WordLookup.Add("Brown", "Fox!");
            WordLookup.Add("Foo", "Bar");
            string v;
            if (WordLookup.TryGet("Foo", out v))
                Console.WriteLine("Foo was found! Value = {0}", v);


            foreach (var keyValuePair in WordLookup.AsPairEnumerable())
            {
                Console.WriteLine("tuple Key:{0}, Value={1} ", keyValuePair.Item1, keyValuePair.Item2);
            }


            int index = 0;
            foreach (var value in WordLookup)
            {
                Console.WriteLine("value index:{0}, Value={1} ", index++, value);
            }

            WordLookup.Remove("Hello", out v);
            WordLookup.Remove("Hello", out v);


            //
            // if (WordLookup.First)
            // {
            //     Console.WriteLine();
            //     Console.WriteLine("Words in Lookup:");
            //     do
            //     {
            //         Console.WriteLine("Key:{0}, Value={1} ", WordLookup.CurrentKey, WordLookup.CurrentValue);
            //     } while (WordLookup.MoveNext());
            // }

            // Console.ReadLine();
        }
    }
}