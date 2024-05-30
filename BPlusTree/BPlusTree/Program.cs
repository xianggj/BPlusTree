using System;

namespace BPlusTree
{
    class Program
    {
       
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
                Console.WriteLine("Key {1} was found! Value = {0}", v,9);


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
        public static void Main(string[] args)
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