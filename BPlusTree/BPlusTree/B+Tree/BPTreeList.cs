using System;
using System.Collections.Generic;

namespace BPlusTree
{
    public class BPTreeList<TKey, TValue> : BPTree<TKey, TValue>, IList<TValue>
    {
        private Func<TValue, TKey> keyFunc;

        public BPTreeList(Func<TValue, TKey> keyFunc, IComparer<TKey> keyComparer = null, int internalNodeCapacity = 32,
            int leafCapacity = 32) :
            base(keyComparer ?? Comparer<TKey>.Default, internalNodeCapacity, leafCapacity)
        {
            this.keyFunc = keyFunc;
        }


        public void Add(TValue item)
        {
            var key = keyFunc(item);
            base.AddOrReplace(key, item);
        }

        public bool Contains(TValue item)
        {
            var key = keyFunc(item);
            return base.ContainsKey(key);
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            if (Count > array.Length - arrayIndex)
                throw new InvalidOperationException("target array is small.");
            var _arrayIndex = arrayIndex;
            foreach (var value in AsEnumerable())
            {
                array[_arrayIndex++] = value;
            }
        }

        public bool Remove(TValue item)
        {
            var key = keyFunc(item);
            TValue value;
            return base.Remove(key, out value);
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        # region IList impl

        public int IndexOf(TValue item)
        {
            if (item == null)
            {
                return -1;
            }

            var key = keyFunc(item);
            return FindKeyIndex(key);
        }

        public void Insert(int index, TValue item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            var item = this[index];
            Remove(item);
        }

        public TValue this[int index]
        {
            get
            {
                TValue value;
                if (!TryGet(index, out value)) throw new KeyNotFoundException();
                return value;
            }
            set { throw new NotImplementedException(); }
        }

        # endregion
    }
}