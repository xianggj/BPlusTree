using System;
using System.Collections.Generic;
using System.Linq;

namespace BPlusTree
{
    /// <summary>
    /// represents an sparse array of <see cref="T"/>. supports duplicate items to be inserted in one key.
    /// </summary>
    public class SparseArray<K, T> : BPTree<K, RingArray<T>>
    {
        #region Fields/Properties

        private static readonly Func<ValueTuple<K, T>, RingArray<T>> _add = t =>
            RingArray<T>.NewArray(Enumerable.Repeat(t.Item2, 1), 4);

        private static readonly Func<ValueTuple<K, T, RingArray<T>>, RingArray<T>> _update = t =>
        {
            t.Item3.Add(t.Item2);
            return t.Item3;
        };

        public new ValueTuple<K, RingArray<T>> Last => base.Last;
        public new ValueTuple<K, RingArray<T>> First => base.First;

        #endregion

        #region Constructors

        /// <summary>
        /// initializes a new <see cref="SparseArray{T}"/>.
        /// </summary>
        public SparseArray(IComparer<K> keyComparer = null, int internalNodeCapacity = 32, int leafCapacity = 32)
            : base(keyComparer, internalNodeCapacity, leafCapacity)
        {
        }

        /// <summary>
        /// initializes a new <see cref="SparseArray{T}"/>.
        /// </summary>
        public SparseArray(IEnumerable<ValueTuple<K, T>> source, IComparer<K> keyComparer = null,
            int internalNodeCapacity = 32, int leafCapacity = 32)
            : this(keyComparer, internalNodeCapacity, leafCapacity)
        {
            var builder = new Builder(this);
            foreach (ValueTuple<K, T> t in source) builder.Add(t.Item1, t.Item2);
            builder.Build();
        }

        #endregion

        #region Get/Add

        /// <summary>
        /// Add single item to sparse array.
        /// </summary>
        public void Add(K key, T item)
        {
            AddOrUpdateFromArg(key, item, _add, _update);
        }

        #endregion

        #region AsEnumerable

        /// <summary>
        /// returns an enumerable for this sparse array.
        /// </summary>
        public new IEnumerable<ValueTuple<K, T>> AsPairEnumerable(bool moveForward = true)
        {
            return GetEnumerable(base.AsPairEnumerable(moveForward), moveForward);
        }

        /// <summary>
        /// returns an enumerable for this sparse array.
        /// </summary>
        /// <typeparam name="TCast">target type to cast items while enumerating</typeparam>
        /// <param name="filter">if true is passed, filters the sequence otherwise casts the sequence values.</param>
        public new IEnumerable<ValueTuple<K,TCast>> AsPairEnumerable<TCast>(bool filter = true,
            bool moveForward = true)
        {
            var enumerable = AsPairEnumerable(moveForward);
            if (filter) enumerable = enumerable.Where(x => x.Item2 is TCast);
            return enumerable.Select(x => ValueTuple.Create(x.Item1, (TCast) (object) x.Item2));
        }

        /// <summary>
        /// returns an enumerable for this sparse array.
        /// </summary>
        /// <param name="start">start of enumerable.</param>
        public new IEnumerable<ValueTuple<K,T>> AsPairEnumerable(K start, bool moveForward = true)
        {
            return GetEnumerable(base.AsPairEnumerable(start, moveForward), moveForward);
        }

        /// <summary>
        /// returns an enumerable for this sparse array.
        /// </summary>
        /// <typeparam name="TCast">target type to cast items while enumerating</typeparam>
        /// <param name="start">start of enumerable.</param>
        /// <param name="filter">if true is passed, filters the sequence otherwise casts the sequence values.</param>
        public new IEnumerable<ValueTuple<K,TCast>> AsPairEnumerable<TCast>(K start, bool filter = true,
            bool moveForward = true)
        {
            var enumerable = AsPairEnumerable(start, moveForward);
            if (filter) enumerable = enumerable.Where(x => x.Item2 is TCast);
            return enumerable.Select(x => ValueTuple.Create(x.Item1, (TCast) (object) x.Item2));
        }

        private IEnumerable<ValueTuple<K,T>> GetEnumerable(IEnumerable<ValueTuple<K,RingArray<T>>> enumerable,
            bool moveForward)
        {
            return moveForward
                ? enumerable.SelectMany(x => x.Item2, (x, y) => ValueTuple.Create(x.Item1, y))
                : enumerable.SelectMany(x => x.Item2.ToReversingList(), (x, y) => ValueTuple.Create(x.Item1, y));
        }

        /// <summary>
        /// returns an enumerable for this sparse array.
        /// </summary>
        public new IEnumerable<T> AsEnumerable(bool moveForward = true)
        {
            return AsPairEnumerable(moveForward).Select(pair => pair.Item2);
        }

        /// <summary>
        /// returns an enumerable for this sparse array.
        /// </summary>
        /// <param name="start">start of enumerable.</param>
        public new IEnumerable<T> AsEnumerable(K start, bool moveForward = true)
        {
            return AsPairEnumerable(start, moveForward).Select(pair => pair.Item2);
        }

        public new IEnumerator<T> GetEnumerator()
        {
            return AsEnumerable().GetEnumerator();
        }

        #endregion

        #region AsGrouping

        /// <summary>
        /// returns an enumerable for this sparse array.
        /// </summary>
        public IEnumerable<ValueTuple<K,IEnumerable<T>>> AsGrouping(bool moveForward = true)
        {
            return GetGrouping(base.AsPairEnumerable(moveForward), moveForward);
        }

        /// <summary>
        /// returns an enumerable for this sparse array.
        /// </summary>
        /// <typeparam name="TCast">target type to cast items while enumerating</typeparam>
        /// <param name="filter">if true is passed, filters the sequence otherwise casts the sequence values.</param>
        public IEnumerable<ValueTuple<K,IEnumerable<TCast>>> AsGrouping<TCast>(bool filter = true,
            bool moveForward = true)
        {
            var enumerable = base.AsPairEnumerable(moveForward);
            if (filter) return enumerable.Select(x => ValueTuple.Create(x.Item1, x.Item2.OfType<TCast>()));
            else return enumerable.Select(x => ValueTuple.Create(x.Item1, x.Item2.Cast<TCast>()));
        }

        /// <summary>
        /// returns an enumerable for this sparse array.
        /// </summary>
        /// <param name="start">start of enumerable.</param>
        public IEnumerable<ValueTuple<K,IEnumerable<T>>> AsGrouping(K start, bool moveForward = true)
        {
            return GetGrouping(base.AsPairEnumerable(start, moveForward), moveForward);
        }

        /// <summary>
        /// returns an enumerable for this sparse array.
        /// </summary>
        /// <typeparam name="TCast">target type to cast items while enumerating</typeparam>
        /// <param name="start">start of enumerable.</param>
        /// <param name="filter">if true is passed, filters the sequence otherwise casts the sequence values.</param>
        public IEnumerable<ValueTuple<K,IEnumerable<TCast>>> AsGrouping<TCast>(K start, bool filter = true,
            bool moveForward = true)
        {
            var enumerable = base.AsPairEnumerable(start, moveForward);
            if (filter) return enumerable.Select(x => ValueTuple.Create(x.Item1, x.Item2.OfType<TCast>()));
            else return enumerable.Select(x =>ValueTuple.Create(x.Item1, x.Item2.Cast<TCast>()));
        }

        private IEnumerable<ValueTuple<K,IEnumerable<T>>> GetGrouping(
            IEnumerable<ValueTuple<K,RingArray<T>>> enumerable, bool moveForward)
        {
            return moveForward
                ? enumerable.Select(x => ValueTuple.Create(x.Item1, x.Item2.ToReadOnlyList()))
                : enumerable.Select(x => ValueTuple.Create(x.Item1, x.Item2.ToReversingReadOnlyList()));
        }

        #endregion

        #region Builder

        internal new sealed class Builder // todo make this public, use builder interface.
        {
            private BPTree<K, RingArray<T>>.Builder builder;

            public Builder(SparseArray<K, T> tree)
            {
                builder = new BPTree<K, RingArray<T>>.Builder(tree);
            }

            public void Add(K key, T value) => builder.Add(key, value, _add, _update);

            public SparseArray<K, T> Build() => (SparseArray<K, T>) builder.Build();
        }

        #endregion
    }
}