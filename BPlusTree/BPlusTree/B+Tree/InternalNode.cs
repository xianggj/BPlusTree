﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BPlusTree
{
    public partial class BPTree<TKey, TValue>
    {
        private sealed partial class InternalNode : Node
        {
            public readonly RingArray<KeyNodeItem> Items;

            public Node Left; // left most child.
            private Node _parent;

            #region Constructors

            public InternalNode(RingArray<KeyNodeItem> items)
            {
                Items = items;
                foreach (var item in Items)
                {
                    item.Right.Parent = this;
                }
            }

            public InternalNode(int capacity)
            {
                Items = RingArray<KeyNodeItem>.NewFixedCapacityArray(capacity);
            }

            #endregion

            #region Properties

            public override bool IsLeaf
            {
                get { return false; }
            }

            public override bool IsFull
            {
                get { return Items.IsFull; }
            }

            public override bool IsHalfFull
            {
                get { return Items.IsHalfFull; }
            }

            public override int Length
            {
                get { return Items.Count; }
            }

            public override IEnumerable<Node> GetChildren()
            {
                if (Left != null)
                {
                    yield return Left;
                }

                int index = 0;
                for (; index < Items.Count; index++)
                {
                    var item = Items[index];
                    yield return item.Right;
                }
            }

            public override IEnumerable<TKey> KeyEnumerable()
            {
                return Items.Select(o => o.Key);
            }

            public override Node Parent
            {
                get { return _parent; }
                set { _parent = value; }
            }

            public override TKey FirstKey
            {
                get { return Items.First.Key; }
            }

            #endregion

            #region Find/Traverse

            public override int Find(ref TKey key, NodeComparer comparer)
            {
                return Items.BinarySearch(new KeyNodeItem(key, null), comparer);
            }

            public override Node GetChild(int index)
            {
                return index < 0 ? Left : Items[index].Right;
            }

            public override Node GetNearestChild(TKey key, NodeComparer comparer)
            {
                var index = Find(ref key, comparer);
                if (index < 0) index = ~index - 1; // get next nearest item.
                return GetChild(index);
            }

            public Node GetLastChild()
            {
                return Items.Last.Right;
            }

            public Node GetFirstChild()
            {
                return Left;
            }

            #endregion

            #region Insert

            public override KeyNodeItem? Insert<TArg>(ref InsertArguments<TArg> args, ref NodeRelatives relatives)
            {
                var key = args.Key;
                var index = Find(ref key, args.Comparer);

                // -1 because items smaller than key have to go inside left child. 
                // since items at each index point to right child, index is decremented to get left child.
                if (index < 0) index = ~index - 1;

                Debug.Assert(index >= -1 && index < Items.Count);

                // get child to traverse through.
                var child = GetChild(index);
                var childRelatives = NodeRelatives.Create(child, index, this, ref relatives);
                var rightChild = child.Insert(ref args, ref childRelatives);

                if (rightChild is KeyNodeItem) // if splitted, add middle key to this node.
                {
                    var middle = (KeyNodeItem) rightChild;
                    middle.Right.Parent = this; // set new child node parent is the current 
                    // +1 because middle is always right side which is fresh node. 
                    // items at index already point to left node after split. so middle must go after index.
                    index++;

                    rightChild = null;
                    if (!IsFull)
                    {
                        Items.Insert(index, middle);
                    }
                    else
                    {
                        InternalNode leftSibling;
                        InternalNode rightSibling;
                        // if left sbiling has spacem, spill left child of this item to left sibling.
                        if (CanSpillTo(relatives.LeftSibling, out leftSibling))
                        {
                            #region Fix Pointers after share

                            // give first item to left sibling.
                            //
                            //        [x][x]       [F][x]
                            //       /   \  \     // \\\ \ 
                            //
                            //        [x][x][F]       [x]
                            //       /   \  \ \\     /// \    

                            #endregion

                            var first = Items.InsertPopFirst(index, middle);

                            KeyNodeItem.SwapRightWith(ref first, ref Left); // swap left and right pointers.

                            var pl = relatives.LeftAncestor.Items[relatives.LeftAncestorIndex];
                            KeyNodeItem.SwapKeys(ref pl, ref first); // swap ancestor key with item.
                            relatives.LeftAncestor.Items[relatives.LeftAncestorIndex] = pl;

                            leftSibling.Items.PushLast(first);
                            first.Right.Parent = leftSibling; // # 1

                            Validate(this);
                            Validate(leftSibling);
                        }
                        else if (CanSpillTo(relatives.RightSibling, out rightSibling)) // if right sibling has space
                        {
                            #region Fix Pointers after share

                            // give last item to right sibling.
                            //
                            //        [x][L]       [x][x]
                            //       /   \ \\     /// \  \
                            //
                            //        [x]        [L][x][x]
                            //       /   \      // \\\ \  \

                            #endregion

                            var last = Items.InsertPopLast(index, middle);

                            KeyNodeItem.SwapRightWith(ref last, ref rightSibling.Left); // swap left and right pointers.

                            var pr = relatives.RightAncestor.Items[relatives.RightAncestorIndex];
                            KeyNodeItem.SwapKeys(ref pr, ref last); // swap ancestor key with item.
                            relatives.RightAncestor.Items[relatives.RightAncestorIndex] = pr;

                            rightSibling.Items.PushFirst(last);
                            last.Right.Parent = rightSibling; // # 2

                            Validate(this);
                            Validate(rightSibling);
                        }
                        else // split, then promote middle item
                        {
                            #region Fix Pointers after split

                            // ==============================================================
                            //
                            // if [left] and [right] were leafs
                            //
                            //     [][]...[N]...[][]
                            //               \   <= if we were here,
                            //             [left][mid][right]
                            //
                            // for insertion, make new key-node item with [mid] as key and [right] as node.
                            // simply add this item next to [N].
                            //
                            //     [][]...[N][mid]..[][]
                            //               \    \
                            //            [left][right]
                            //
                            // ==============================================================
                            //
                            // if [left] and [right] were internal nodes.
                            //
                            //     [middle]        [rightNode]       
                            //            \\       *         \     <= left pointer of [rightNode] is null 
                            //
                            //  Becomes
                            //
                            //    [middle]
                            //           \
                            //         [rightNode]
                            //        //          \
                            //
                            // ==============================================================

                            #endregion

                            var rightNode = new InternalNode(Items.SplitRight());
                            rightNode.Parent = this.Parent;

                            // find middle key to promote
                            if (index < Items.Count)
                            {
                                middle = Items.InsertPopLast(index, middle);
                            }
                            else if (index > Items.Count)
                            {
                                middle.Right.Parent = rightNode;
                                middle = rightNode.Items.InsertPopFirst(index - Items.Count, middle);
                            }

                            rightNode.Left = middle.Right;
                            rightNode.Left.Parent = rightNode;// for current pop last Key
                            KeyNodeItem.ChangeRight(ref middle, rightNode);
                            rightChild = middle;

                            Validate(this);
                            Validate(rightNode);
                        }
                    }
                }

                return rightChild;
            }

            private bool CanSpillTo(Node node, out InternalNode inode)
            {
                inode = (InternalNode) node;
                return inode != null ? !inode.IsFull : false;
            }

            #endregion

            #region Remove

            public override bool Remove(ref RemoveArguments args, ref NodeRelatives relatives)
            {
                var merge = false;
                var key = args.Key;
                var index = Find(ref key, args.Comparer);
                if (index < 0) index = ~index - 1;

                Debug.Assert(index >= -1 && index < Items.Count);

                var child = GetChild(index);
                var childRelatives = NodeRelatives.Create(child, index, this, ref relatives);
                var childMerged = child.Remove(ref args, ref childRelatives);

                if (childMerged)
                {
                    // removes right sibling of child if left most child is merged, other wise merged child is removed.
                    Items.RemoveAt(Math.Max(0, index));

                    if (!Items.IsHalfFull) // borrow or merge
                    {
                        InternalNode leftSibling;
                        InternalNode rightSibling;
                        if (CanBorrowFrom(relatives.LeftSibling, out leftSibling))
                        {
                            var last = leftSibling.Items.PopLast();

                            KeyNodeItem.SwapRightWith(ref last, ref Left); // swap left and right pointers.

                            var pr = relatives.LeftAncestor.Items[relatives.LeftAncestorIndex];
                            KeyNodeItem.SwapKeys(ref pr, ref last); // swap ancestor key with item.
                            relatives.LeftAncestor.Items[relatives.LeftAncestorIndex] = pr;

                            Items.PushFirst(last);
                            last.Right.Parent = this;

                            Validate(this);
                            Validate(leftSibling);
                        }
                        else if (CanBorrowFrom(relatives.RightSibling, out rightSibling))
                        {
                            var first = rightSibling.Items.PopFirst();

                            // swap left and right pointers.
                            KeyNodeItem.SwapRightWith(ref first, ref rightSibling.Left);

                            var pl = relatives.RightAncestor.Items[relatives.RightAncestorIndex];
                            KeyNodeItem.SwapKeys(ref pl, ref first); // swap ancestor key with item.
                            relatives.RightAncestor.Items[relatives.RightAncestorIndex] = pl;

                            Items.PushLast(first);
                            first.Right.Parent = this;

                            Validate(this);
                            Validate(rightSibling);
                        }
                        else // merge
                        {
                            merge = true;
                            if (relatives.HasTrueLeftSibling) // current node will be removed from parent
                            {
                                var pkey = relatives.LeftAncestor.Items[relatives.LeftAncestorIndex].Key; // demote key
                                var mid = new KeyNodeItem(pkey, Left);
                                leftSibling.Items.PushLast(mid);
                                leftSibling.Items.MergeLeft(Items); // merge from left to keep items in order.

                                Validate(leftSibling);
                            }
                            else if (relatives.HasTrueRightSibling) // right sibling will be removed from parent
                            {
                                // demote key
                                var pkey = relatives.RightAncestor.Items[relatives.RightAncestorIndex].Key;
                                var mid = new KeyNodeItem(pkey, rightSibling.Left);
                                Items.PushLast(mid);
                                Items.MergeLeft(rightSibling.Items); // merge from right to keep items in order.

                                Validate(this);
                            }
                        }
                    }
                }

                return merge; // true if merge happened.
            }

            private bool CanBorrowFrom(Node node, out InternalNode inode)
            {
                inode = (InternalNode) node;
                if (inode == null) return false;
                return inode.Items.Count > inode.Items.Capacity / 2;
            }

            #endregion

            #region Debug

            [Conditional("DEBUG")]
            private static void Validate(InternalNode node)
            {
                if (node == null) return;
                Debug.Assert(node.IsHalfFull);
                Debug.Assert(node.Left != null);
            }

            #endregion
        }
    }
}