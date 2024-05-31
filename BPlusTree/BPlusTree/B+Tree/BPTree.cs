using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BPlusTree
{
    // TODO Implement listener.
    // TODO make thread safe.
    /// <summary>
    /// represents an efficient dynamic B+ tree with builder support.
    /// </summary>
    public partial class BPTree<TKey, TValue> : IBPTree<TKey, TValue>
    {
        #region Properties

        /// <summary>
        /// maximum number of keys in internal node.
        /// </summary>
        public int InternalNodeCapacity { get; set; }

        /// <summary>
        /// maximum number of keys in leaf node.
        /// </summary>
        public int LeafCapacity { get; set; }

        /// <summary>
        /// current height of this tree.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// number of items in this tree.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// key comparer used in this tree to compare keys against each other.
        /// </summary>
        public IComparer<TKey> KeyComparer
        {
            get { return _comparer.KeyComparer; }
        }

        #endregion

        #region Fields

        private readonly NodeComparer _comparer;
        private Node Root;
        private LeafNode LinkList; // pointer to first leaf in tree.
        private LeafNode ReverseLinkList; // pointer to last leaf in tree.
        private int _version;

        #endregion

        #region Constructors

        /// <summary>
        /// initializes a new <see cref="BPTree{TKey, TValue}"/>.
        /// </summary>
        public BPTree(IComparer<TKey> keyComparer = null, int internalNodeCapacity = 32, int leafCapacity = 32)
        {
            if (internalNodeCapacity < 2)
                throw new ArgumentOutOfRangeException("internalNodeCapacity",
                    "internal node capacity must be greater than 1.");
            if (leafCapacity < 1)
                throw new ArgumentOutOfRangeException("leafCapacity", "leaf capacity must be greater than 0.");

            _comparer = new NodeComparer(keyComparer);

            InternalNodeCapacity = internalNodeCapacity;
            LeafCapacity = leafCapacity;
        }

        /// <summary>
        /// initializes a new <see cref="BPTree{TKey, TValue}"/>.
        /// </summary>
        public BPTree(IEnumerable<ValueTuple<TKey, TValue>> source, IComparer<TKey> keyComparer = null,
            int internalNodeCapacity = 32, int leafCapacity = 32)
            : this(keyComparer, internalNodeCapacity, leafCapacity)
        {
            new Builder(this, source).Build();
        }

        #endregion

        #region Add

        /// <summary>
        /// adds an specified key and value to the tree.
        /// </summary>
        /// <exception cref="InvalidOperationException">if item with the same key already exists.</exception>
        public void Add(TKey key, TValue value)
        {
            AddOrUpdate(key, value,
                _ => { throw new InvalidOperationException("item with same key already exists."); });
        }

        /// <summary>
        /// add value if key does not exists. returns true if value is added or false if key already exists.
        /// </summary>
        public bool TryAdd(TKey key, TValue value)
        {
            return AddOrUpdate(key, value, t => t.Item3);
        }

        /// <summary>
        /// adds an specified key and value to the tree, in case key is duplicate, old value is replaced with new one. returns true if item was added.
        /// </summary>
        public bool AddOrReplace(TKey key, TValue value)
        {
            return AddOrUpdate(key, value, t => t.Item2);
        }

        /// <summary>
        /// adds an specified key and value to the tree, in case key is duplicate, update function is used to update existing value. returns true if item was added.
        /// </summary>
        public bool AddOrUpdate(TKey key, TValue value, Func<ValueTuple<TKey, TValue, TValue>, TValue> updateFunction)
        {
            return AddOrUpdateFromArg(key, value, t => t.Item2, updateFunction);
        }

        /// <summary>
        /// adds an specified key and value to the tree. in case key is duplicate, update function is used to update existing value. 
        /// in case key is not duplicate, add function is used with an argument to produce a value. returns true if item was added.
        /// </summary>
        public bool AddOrUpdateFromArg<TArg>(TKey key, TArg arg, Func<ValueTuple<TKey, TArg>, TValue> addFunction,
            Func<ValueTuple<TKey, TArg, TValue>, TValue> updateFunction)
        {
            var args = new InsertArguments<TArg>(ref key, ref arg, ref addFunction, ref updateFunction, _comparer);
            AddOrUpdateCore(ref args);
            return args.Added;
        }

        /// <summary>
        /// core method used to add an item to the tree using <see cref="InsertArguments{TArg}"/>.
        /// </summary>
        private void AddOrUpdateCore<TArg>(ref InsertArguments<TArg> args)
        {
            if (Count == 0)
            {
                Root = new LeafNode(LeafCapacity); // first root is leaf
                LinkList = (LeafNode) Root;
                ReverseLinkList = LinkList;
                Height++;
            }

            // append optimization: if item key is in order, this may add item in O(1) operation.
            int order = Count == 0 ? 1 : args.Comparer.KeyComparer.Compare(args.Key, Last.Item1);
            if (order > 0 && !ReverseLinkList.IsFull)
            {
                ReverseLinkList.Items.PushLast(new KeyValueItem(args.Key, args.GetValue()));
                Count++;
                _version++;
                return;
            }
            else if (order == 0)
            {
                var item = ReverseLinkList.Items.Last;
                KeyValueItem.ChangeValue(ref item, args.GetUpdateValue(item.Value));
                ReverseLinkList.Items.Last = item;
                _version++;
                return;
            }

            // preppend optimization: if item key is in order, this may add item in O(1) operation.
            order = args.Comparer.KeyComparer.Compare(args.Key, First.Item1);
            if (order < 0 && !LinkList.IsFull)
            {
                LinkList.Items.PushFirst(new KeyValueItem(args.Key, args.GetValue()));
                Count++;
                _version++;
                return;
            }
            else if (order == 0)
            {
                var item = LinkList.Items.First;
                KeyValueItem.ChangeValue(ref item, args.GetUpdateValue(item.Value));
                LinkList.Items.First = item;
                _version++;
                return;
            }

            NodeRelatives nr = new NodeRelatives();
            var rightSplit = Root.Insert(ref args, ref nr);

            if (args.Added) Count++;
            _version++;

            // if split occured at root, make a new root and increase height.
            if (rightSplit is KeyNodeItem)
            {
                var middle = (KeyNodeItem) rightSplit;
                var newRoot = new InternalNode(InternalNodeCapacity) {Left = Root};
                newRoot.Items.Insert(0, middle);
                Root = newRoot;
                Height++;
            }

            if (ReverseLinkList.Next != null) // true if last leaf is split.
            {
                ReverseLinkList = ReverseLinkList.Next;
            }
        }

        #endregion

        #region Remove

        /// <summary>
        /// removes first item from the tree. returns true if the item was removed.
        /// </summary>
        public bool RemoveFirst(out TValue first)
        {
            first = default(TValue);
            if (Count == 0) return false;
            return Remove(First.Item1, out first);
        }

        /// <summary>
        /// removes last item from the tree. returns true if the item was removed.
        /// </summary>
        public bool RemoveLast(out TValue last)
        {
            last = default(TValue);
            if (Count == 0) return false;
            return Remove(Last.Item1, out last);
        }

        /// <summary>
        /// removes the value with specified key from the tree. returns true if the value was removed.
        /// </summary>
        public bool Remove(TKey key, out TValue value)
        {
            var args = new RemoveArguments(ref key, _comparer);
            RemoveCore(ref args);
            value = args.Value;
            return args.Removed;
        }

        /// <summary>
        /// core method used to remove an item from the tree using <see cref="RemoveArguments"/>.
        /// </summary>
        private void RemoveCore(ref RemoveArguments args)
        {
            if (Count == 0) return;

            // optimize for removing items from beginning
            int order = args.Comparer.KeyComparer.Compare(args.Key, First.Item1);
            if (order < 0) return;
            if (order == 0 && (Root == LinkList || LinkList.Items.Count > LinkList.Items.Capacity / 2))
            {
                args.SetRemovedValue(LinkList.Items.PopFirst().Value);
                Debug.Assert(Root == LinkList || LinkList.IsHalfFull);
                Count--;
                _version++;
                if (Count == 0)
                {
                    Root = LinkList = ReverseLinkList = null;
                    Height--;
                }

                return;
            }

            // optimize for removing items from end
            order = args.Comparer.KeyComparer.Compare(args.Key, Last.Item1);
            if (order > 0) return;
            if (order == 0 && (Root == ReverseLinkList ||
                               ReverseLinkList.Items.Count > ReverseLinkList.Items.Capacity / 2))
            {
                args.SetRemovedValue(ReverseLinkList.Items.PopLast().Value);
                Debug.Assert(Root == ReverseLinkList || ReverseLinkList.IsHalfFull);
                Count--; // here count never becomes zero.
                _version++;
                return;
            }

            var nr = new NodeRelatives();
            var merge = Root.Remove(ref args, ref nr);

            if (args.Removed)
            {
                Count--;
                _version++;
            }

            if (merge && Root.Length == 0)
            {
                Root = Root.GetChild(-1); // left most child becomes root. (returns null for leafs)
                if (Root == null)
                {
                    LinkList = null;
                    ReverseLinkList = null;
                }

                Height--;
            }

            if (ReverseLinkList != null && ReverseLinkList.Previous != null &&
                ReverseLinkList.Previous.Next == null) // true if last leaf is merged.
            {
                ReverseLinkList = ReverseLinkList.Previous;
            }
        }

        #endregion

        #region Clear

        /// <summary>
        /// removes all elements from the tree.
        /// </summary>
        public void Clear()
        {
            _version++;
            Count = 0;
            Height = 0;
            Root = null;
            LinkList = null;
            ReverseLinkList = null;
        }

        #endregion

        #region Indexer

        /// <summary>
        /// retrieves the value associated with the specified key.
        /// </summary>
        /// <exception cref="KeyNotFoundException"></exception>
        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (!TryGet(key, out value)) throw new KeyNotFoundException();
                return value;
            }
        }

        /// <summary>
        /// retrieves the value associated with the specified key.
        /// </summary>
        public bool TryGet(TKey key, out TValue value)
        {
            value = default(TValue);
            int index;
            var leaf = FindLeaf(key, out index);
            if (index >= 0) value = leaf.Items[index].Value;
            return index >= 0;
        }

        /// <summary>
        /// determines whether the tree contains the specified key.
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            TValue _;
            return TryGet(key, out _);
        }

        /// <summary>
        /// find the leaf and index of the key. if key is not found complement of the index is returned.
        /// </summary>
        private LeafNode FindLeaf(TKey key, out int index)
        {
            index = -1;
            if (Count == 0) return null;

            var node = Root;
            while (!node.IsLeaf) node = node.GetNearestChild(key, _comparer);
            index = node.Find(ref key, _comparer);
            return (LeafNode) node;
        }

        /// <summary>
        /// retrieves the value associated with nearest key to the specified key.
        /// </summary>
        public TValue NextNearest(TKey key) // todo should return int.
        {
            int leafIndex;
            var leaf = FindLeaf(key, out leafIndex);
            if (leafIndex < 0) leafIndex = ~leafIndex; // get nearest
            if (leafIndex >= leaf.Items.Count) leafIndex--;
            return leaf.Items[leafIndex].Value;
        }

        /// <summary>
        /// retrieves first item from the tree.
        /// </summary>
        /// <exception cref="InvalidOperationException">collection is empty.</exception>
        public ValueTuple<TKey, TValue> First
        {
            get
            {
                if (Count == 0) throw new InvalidOperationException("collection is empty.");
                var firstItem = LinkList.Items.First;
                return ValueTuple.Create(firstItem.Key, firstItem.Value);
            }
        }

        /// <summary>
        /// retrieves last item from the tree.
        /// </summary>
        /// <exception cref="InvalidOperationException">collection is empty.</exception>
        public ValueTuple<TKey, TValue> Last
        {
            get
            {
                if (Count == 0) throw new InvalidOperationException("collection is empty.");
                var lastItem = ReverseLinkList.Items.Last;
                return ValueTuple.Create(lastItem.Key, lastItem.Value);
            }
        }

        #endregion

        # region Indexer array index

        /// <summary>
        /// calculate the key index in the whole leaf node LinkList.
        /// </summary>
        protected int FindKeyIndex(TKey key)
        {
            int index;
            var leaf = FindLeaf(key, out index); // index in the current leaf, not include previous leaf.

            // plus Previous index size
            var leafPrevious = leaf.Previous;
            while (leafPrevious != null)
            {
                index += leafPrevious.Length;
                leafPrevious = leafPrevious.Previous;
            }

            return index;
        }

        protected bool TryGet(int index, out TValue value)
        {
            value = default(TValue);
            var _index = index;
            foreach (var leafNode in GetLeafNodeEnumerable())
            {
                int len = leafNode.Length;
                if (len > _index)
                {
                    value = leafNode.Items[_index].Value;
                    return true;
                }

                _index = _index - len;
            }

            return false;
        }


        private IEnumerable<LeafNode> GetLeafNodeEnumerable()
        {
            LeafNode leaf = LinkList;
            while (leaf != null)
            {
                yield return leaf;
                leaf = leaf.Next;
            }
        }

        # endregion

        #region AsEnumerable

        /// <summary>
        /// returns an enumerable for this tree.
        /// </summary>
        public IEnumerable<ValueTuple<TKey, TValue>> AsPairEnumerable(bool moveForward = true)
        {
            return GetEnumerable(moveForward ? LinkList : ReverseLinkList, moveForward ? 0 : ReverseLinkList.Length - 1,
                moveForward);
        }

        /// <summary>
        /// returns an enumerable for this tree.
        /// </summary>
        /// <typeparam name="TCast">target type to cast items while enumerating</typeparam>
        /// <param name="filter">if true is passed, filters the sequence otherwise casts the sequence values.</param>
        public IEnumerable<ValueTuple<TKey, TCast>> AsPairEnumerable<TCast>(bool filter = true, bool moveForward = true)
        {
            var enumerable = AsPairEnumerable(moveForward);
            if (filter) enumerable = enumerable.Where(x => x.Item2 is TCast);
            return enumerable.Select(x => ValueTuple.Create(x.Item1, (TCast) (object) x.Item2));
        }

        /// <summary>
        /// returns an enumerable for this tree.
        /// </summary>
        /// <param name="start">start of enumerable.</param>
        public IEnumerable<ValueTuple<TKey, TValue>> AsPairEnumerable(TKey start, bool moveForward = true)
        {
            int index;
            var leaf = FindLeaf(start, out index);
            if (index < 0)
            {
                index = ~index;
                if (!moveForward) index--;
            }

            return GetEnumerable(leaf, index, moveForward);
        }

        /// <summary>
        /// returns an enumerable for this tree.
        /// </summary>
        /// <typeparam name="TCast">target type to cast items while enumerating</typeparam>
        /// <param name="start">start of enumerable.</param>
        /// <param name="filter">if true is passed, filters the sequence otherwise casts the sequence values.</param>
        public IEnumerable<ValueTuple<TKey, TCast>> AsPairEnumerable<TCast>(TKey start, bool filter = true,
            bool moveForward = true)
        {
            var enumerable = AsPairEnumerable(start, moveForward);
            if (filter) enumerable = enumerable.Where(x => x.Item2 is TCast);
            return enumerable.Select(x => ValueTuple.Create(x.Item1, (TCast) (object) x.Item2));
        }

        private IEnumerable<ValueTuple<TKey, TValue>> GetEnumerable(LeafNode leaf, int index, bool moveForward)
        {
            var version = _version;

            if (moveForward)
            {
                while (leaf != null)
                {
                    for (; index < leaf.Items.Count; index++)
                    {
                        var item = leaf.Items[index];
                        yield return ValueTuple.Create(item.Key, item.Value);
                        if (version != _version) throw new InvalidOperationException("collection was modified.");
                    }

                    leaf = leaf.Next;
                    index = 0;
                }
            }
            else
            {
                while (leaf != null)
                {
                    for (; index >= 0; index--)
                    {
                        var item = leaf.Items[index];
                        yield return ValueTuple.Create(item.Key, item.Value);
                        if (version != _version) throw new InvalidOperationException("collection was modified.");
                    }

                    leaf = leaf.Previous;
                    index = leaf.Items.Count - 1;
                }
            }
        }

        /// <summary>
        /// returns an enumerable for this tree.
        /// </summary>
        public IEnumerable<TValue> AsEnumerable(bool moveForward = true)
        {
            return AsPairEnumerable(moveForward).Select(pair => pair.Item2);
        }

        /// <summary>
        /// returns an enumerable for this tree.
        /// </summary>
        /// <param name="start">start of enumerable.</param>
        public IEnumerable<TValue> AsEnumerable(TKey start, bool moveForward = true)
        {
            return AsPairEnumerable(start, moveForward).Select(pair => pair.Item2);
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            return AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}