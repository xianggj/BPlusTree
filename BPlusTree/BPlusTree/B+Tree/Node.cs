using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BPlusTree
{
    public partial class BPTree<TKey, TValue>
    {
        #region Node

        /// <summary>
        /// base class for internal nodes and leaf nodes.
        /// </summary>
        private abstract partial class Node
        {
            /// <summary>
            /// inserts an item to a node. if node splits, right split is returned other wise null is returned.
            /// </summary>
            public abstract KeyNodeItem? Insert<TArg>(ref InsertArguments<TArg> args, ref NodeRelatives relatives);

            /// <summary>
            /// removes an item from a node. if node merges, returns ture. other wise returns false. 
            /// all nodes merge to left sibling. if left most child underflows, its right sibling is merged to left most child.
            /// </summary>
            public abstract bool Remove(ref RemoveArguments args, ref NodeRelatives relatives);

            /// <summary>
            /// finds index of child or value with specified key. if key is not found, complement index of nearest item is returned.
            /// </summary>
            public abstract int Find(ref TKey item, NodeComparer comparer);

            /// <summary>
            /// get child of node at specified index. if used on leaf node it returns null.
            /// </summary>
            public abstract Node GetChild(int index);

            /// <summary>
            /// find nearest child with the key.
            /// </summary>
            public abstract Node GetNearestChild(TKey key, NodeComparer comparer);
            
            /// <summary>
            /// find nearest child with the key and sum key count between first and left sibling child.

            /// <param name="count">sum key count between first leaf and the return node's left sibling leaf</param>
            /// </summary>
            public abstract Node GetNearestChild(TKey key, NodeComparer comparer, out int count);

            public abstract TKey FirstKey { get; }

            /// <summary>
            /// true if node is leaf.
            /// </summary>
            public abstract bool IsLeaf { get; }

            /// <summary>
            /// true if node is full.
            /// </summary>
            public abstract bool IsFull { get; }

            /// <summary>
            /// true if node contains items at least half of its capacity. (Length >= Capacity/2)
            /// </summary>
            public abstract bool IsHalfFull { get; }

            /// <summary>
            /// number of items in this node.
            /// </summary>
            public abstract int Length { get; }

            /// <summary>
            /// returns an children node enumerable for this node.
            /// </summary>
            public abstract IEnumerable<Node> GetChildren();

            /// <summary>
            /// returns an key enumerable for this node.
            /// </summary>
            public abstract IEnumerable<TKey> KeyEnumerable();
            
            public abstract int AddAndGetSubtreeValueCount(int delta);
        }

        #endregion

        #region Leaf Items

        /// <summary>
        /// represents a value associated with a key.
        /// used for searching and storing items in leaf.
        /// </summary>
        private partial struct KeyValueItem
        {
            public readonly TKey Key;
            public readonly TValue Value;

            public KeyValueItem(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }

            public static void ChangeValue(ref KeyValueItem item, TValue newValue)
            {
                item = new KeyValueItem(item.Key, newValue);
            }
        }

        #endregion

        #region Internal Node Items

        /// <summary>
        /// represents a key and a pointer to right child.
        /// used for searching and storing items in internal nodes.
        /// </summary>
        private partial struct KeyNodeItem
        {
            public readonly TKey Key;
            public readonly Node Right;

            public KeyNodeItem(TKey key, Node right)
            {
                Key = key;
                Right = right;
            }

            public static void ChangeKey(ref KeyNodeItem item, TKey newKey)
            {
                item = new KeyNodeItem(newKey, item.Right);
            }

            public static void SwapKeys(ref KeyNodeItem x, ref KeyNodeItem y)
            {
                var xKey = x.Key;
                ChangeKey(ref x, y.Key);
                ChangeKey(ref y, xKey);
            }

            public static void ChangeRight(ref KeyNodeItem item, Node newRight)
            {
                item = new KeyNodeItem(item.Key, newRight);
            }

            public static void SwapRightWith(ref KeyNodeItem item, ref Node pointer)
            {
                var temp = pointer;
                pointer = item.Right;
                item = new KeyNodeItem(item.Key, temp);
            }
        }

        #endregion

        #region Key Comparer

        /// <summary>
        /// contains the key comparer required to find the path to leaf nodes and items.
        /// </summary>
        private sealed class NodeComparer : IComparer<KeyNodeItem>, IComparer<KeyValueItem>
        {
            public readonly IComparer<TKey> KeyComparer;

            public NodeComparer(IComparer<TKey> keyComparer)
            {
                KeyComparer = keyComparer ?? Comparer<TKey>.Default;
            }

            /// <inheritdoc />
            public int Compare(KeyNodeItem x, KeyNodeItem y)
            {
                return KeyComparer.Compare(x.Key, y.Key);
            }

            /// <inheritdoc />
            public int Compare(KeyValueItem x, KeyValueItem y)
            {
                return KeyComparer.Compare(x.Key, y.Key);
            }
        }

        #endregion

        #region Insert Arguments

        /// <summary>
        /// contains reaonly arguments for insert operation.
        /// </summary>
        private struct InsertArguments<TArg>
        {
            public readonly TKey Key;
            private readonly TArg Arg; // optional argument, can be TValue or any helper value.
            private readonly Func<ValueTuple<TKey, TArg>, TValue> AddFunction;
            private readonly Func<ValueTuple<TKey, TArg, TValue>, TValue> UpdateFunction;
            public readonly NodeComparer Comparer;

            /// <summary>
            /// true if item was added and not updated.
            /// </summary>
            public bool Added { get; private set; }

            public TValue GetValue() // get value
            {
                Added = true;
                return AddFunction(ValueTuple.Create(Key, Arg));
            }

            public TValue GetUpdateValue(TValue oldVal) // get update value
            {
                return UpdateFunction(ValueTuple.Create(Key, Arg, oldVal));
            }

            public InsertArguments(ref TKey key, ref TArg arg,
                ref Func<ValueTuple<TKey, TArg>, TValue> addFunction,
                ref Func<ValueTuple<TKey, TArg, TValue>, TValue> updateValue, NodeComparer comparer) : this()
            {
                Key = key;
                Arg = arg;
                AddFunction = addFunction;
                UpdateFunction = updateValue;
                Comparer = comparer;

                Added = false;
            }
        }

        #endregion

        #region Remove Arguments

        /// <summary>
        /// contains arguments for remove operation.
        /// </summary>
        private struct RemoveArguments
        {
            public readonly TKey Key;
            public readonly NodeComparer Comparer;

            /// <summary>
            /// result is set once when the value is found at leaf node.
            /// </summary>
            public TValue Value { get; private set; }

            /// <summary>
            /// true if item is removed.
            /// </summary>
            public bool Removed { get; private set; }

            public RemoveArguments(ref TKey key, NodeComparer comparer) : this()
            {
                Key = key;
                Comparer = comparer;

                Value = default(TValue);
                Removed = false;
            }

            public void SetRemovedValue(TValue value)
            {
                Value = value;
                Removed = true;
            }
        }

        #endregion

        #region Node Kindship Relations

        /// <summary>
        /// contains information about relatives of each node, such as ancestors and siblings.
        /// this information is used for borrow and spill operations.
        /// </summary>
        private struct NodeRelatives
        {
            /*  Note: "/" is left pointer. "\" is right pointer.
             * 
             *               [LeftAncestor][RightAncestor]
             *              /              \              \
             *       [LeftSibling]       [Node]     [RightSibling]
             *      
             *      
             *                    [LeftAncestor][...]
             *                   /              \    ...
             *                [X]       [RightAncestor]  ...
             *                   \     /               \
             *         [LeftSibling][Node]       [RightSibling]
             *        
             *                      [RightAncestor][...]
             *                     /        \         ...
             *          [LeftAncestor]      [X][...]     ...
             *         /              \    /    
             *   [LeftSibling]     [Node][RightSibling]  
            */

            public readonly Node LeftSibling;
            public readonly Node RightSibling;

            /// <summary>
            /// nearest ancestor of node and its left sibling.
            /// </summary>
            public readonly InternalNode LeftAncestor;

            /// <summary>
            /// parent or ancestor used to get right sibling.
            /// </summary>
            public readonly InternalNode RightAncestor;

            /// <summary>
            /// index of item in ancestor that shares left sibling.
            /// </summary>
            public readonly int LeftAncestorIndex;

            /// <summary>
            /// index of item in ancestor that shares right sibling.
            /// </summary>
            public readonly int RightAncestorIndex;

            /// <summary>
            /// if left sibling is sibling and not cousin
            /// Two nodes are cousins of each other if they are at same level and have different parents.
            /// </summary>
            public readonly bool HasTrueLeftSibling;

            /// <summary>
            /// if right sibling is sibling and not cousin
            /// </summary>
            public readonly bool HasTrueRightSibling;


            public int Delta;
            public int LeftSiblingDelta;
            public int RightSiblingDelta;
           

            private NodeRelatives(InternalNode leftAncestor, int leftAncestorIndex, Node leftSibling,
                bool hasTrueLeftSibling,
                InternalNode rightAncestor, int rightAncestorIndex, Node rightSibling, bool hasTrueRightSibling)
            {
                LeftAncestor = leftAncestor;
                LeftAncestorIndex = leftAncestorIndex;
                LeftSibling = leftSibling;
                HasTrueLeftSibling = hasTrueLeftSibling;

                RightAncestor = rightAncestor;
                RightAncestorIndex = rightAncestorIndex;
                RightSibling = rightSibling;
                HasTrueRightSibling = hasTrueRightSibling;
                
                Delta = 0;
                LeftSiblingDelta = 0;
                RightSiblingDelta = 0;

            }

            /// <summary>
            /// creates new relatives for child node.
            /// <param name="child">child node</param>
            /// <param name="index"> child index in parent</param>
            /// <param name="parent"> child's parent node</param>
            /// </summary>
            public static NodeRelatives Create(Node child, int index, InternalNode parent,
                ref NodeRelatives parentRelatives)
            {
                Debug.Assert(index >= -1 && index < parent.Length);

                // assign nearest ancestors betweem child and siblings.
                InternalNode leftAncestor, rightAncestor;
                int leftAncestorIndex, rightAncestorIndex;
                Node leftSibling, rightSibling;
                bool hasTrueLeftSibling, hasTrueRightSibling;

                if (index == -1) // if child is left most, use left cousin as left sibling.
                {
                    leftAncestor = parentRelatives.LeftAncestor;
                    leftAncestorIndex = parentRelatives.LeftAncestorIndex;
                    leftSibling = (InternalNode) parentRelatives.LeftSibling != null
                        ? ((InternalNode) parentRelatives.LeftSibling).GetLastChild()
                        : null;
                    hasTrueLeftSibling = false;

                    rightAncestor = parent;
                    rightAncestorIndex = index + 1;
                    rightSibling = parent.GetChild(rightAncestorIndex);
                    hasTrueRightSibling = true;
                }
                else if (index == parent.Length - 1) // if child is right most, use right cousin as right sibling.
                {
                    leftAncestor = parent;
                    leftAncestorIndex = index;
                    leftSibling = parent.GetChild(leftAncestorIndex - 1);
                    hasTrueLeftSibling = true;

                    rightAncestor = parentRelatives.RightAncestor;
                    rightAncestorIndex = parentRelatives.RightAncestorIndex;
                    rightSibling = (InternalNode) parentRelatives.RightSibling != null
                        ? ((InternalNode) parentRelatives.RightSibling).GetFirstChild()
                        : null;
                    hasTrueRightSibling = false;
                }
                else // child is not right most nor left most.
                {
                    leftAncestor = parent;
                    leftAncestorIndex = index;
                    leftSibling = parent.GetChild(leftAncestorIndex - 1);
                    hasTrueLeftSibling = true;

                    rightAncestor = parent;
                    rightAncestorIndex = index + 1;
                    rightSibling = parent.GetChild(rightAncestorIndex);
                    hasTrueRightSibling = true;
                }

                return new NodeRelatives(leftAncestor, leftAncestorIndex, leftSibling, hasTrueLeftSibling,
                    rightAncestor, rightAncestorIndex, rightSibling, hasTrueRightSibling);
            }
        }

        #endregion
    }
}