using System;
using System.Collections.Generic;
using System.Linq;

namespace BPlusTree
{
    class Program
    {
        private static void Main(string[] args)
        {
            int BLOCK_CAPACITY_4_LST = 4;
            int COUNT = 4;

            IComparer<int> int32Cmp = Comparer<int>.Default;
            BPTreeList<int, int> bPlusTreeList =
                new BPTreeList<int, int>(o => o, int32Cmp, BLOCK_CAPACITY_4_LST, COUNT);

            int[] arr =
            {
                // 10, +7, +8, +1, +4, +5, +2 //, +9
                // +10, +7, +8, +1, +4, +5,+2, +9, +6, +3, +20, +79, +95, +55, +44, +60
                // +7,+9,+2,+8,+4,+5,+10,+6,+3,+1,+57,+65,+72,+84,+63,+14
                // +9,+2,+4,+5,+10,+6,+3,+1,+57,+65,+72,+84,+63,+14,-9,-10,+10,-10,+85,+53,+81
                +9, +2, +4, +5, +10, +6, +3, +1, +57, +65, +72, +84, +63, +14, +85, +53, +81, +58, +64, +54, +7, +8, +86, +38, +27, +100, +39, +68, +51, +18, +15, +59
            };


            foreach (var a in arr)
            {
                bPlusTreeList.Add(a);
                bPlusTreeList.Display();
                Console.WriteLine();
            }


            var index = 0;
            foreach (var i in bPlusTreeList)
            {
                Console.WriteLine("index:{0}, value:{1}", index++, i);
            }


            // bPlusTreeList.Remove(63);


            bPlusTreeList.Display();


            foreach (var i in arr)
            {
                Console.WriteLine("Remove {0}", i);
                bPlusTreeList.Remove(i);
                bPlusTreeList.Display();
            }

            Console.WriteLine("END");
        }

        public static void Main0(string[] args)
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
            tree.CopyTo(array1, 0);
            Console.WriteLine("array :{0}", string.Join("\n", array1));
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