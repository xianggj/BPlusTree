using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BPlusTree
{
    public partial class BPTree<TKey, TValue>
    {
        private sealed partial class LeafNode : Node
        {
            public readonly RingArray<KeyValueItem> Items;

            // leaf node siblings are linked to gether to make doubly linked list.
            public LeafNode Previous;
            public LeafNode Next;
            private int _subtreeValueCount;

            #region Constructors

            public LeafNode(RingArray<KeyValueItem> items)
            {
                Items = items;
                _subtreeValueCount = items.Count;
            }

            public LeafNode(int capacity)
            {
                Items = RingArray<KeyValueItem>.NewFixedCapacityArray(capacity);
            }

            #endregion

            #region Properties

            public override bool IsLeaf
            {
                get { return true; }
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
                return Enumerable.Empty<Node>();
            }

            public override IEnumerable<TKey> KeyEnumerable()
            {
                return Items.Select(o => o.Key);
                // for (int i = 0; i < Items.Count; i++)
                // {
                //     yield return Items[i].Key;
                // }
            }

            public override int AddAndGetSubtreeValueCount(int delta)
            {
                _subtreeValueCount += delta;
                return _subtreeValueCount;
            }

            public override TKey FirstKey
            {
                get { return Items.First.Key; }
            }

            #endregion

            #region Find/Traverse

            public override int Find(ref TKey key, NodeComparer comparer)
            {
                // find value in this bucket
                return Items.BinarySearch(new KeyValueItem(key, default(TValue)), comparer);
            }

            public override Node GetChild(int index)
            {
                return null;
            }

            public override Node GetNearestChild(TKey key, NodeComparer comparer)
            {
                return null;
            }
            
            public override Node GetNearestChild(TKey key, NodeComparer comparer, out int count)
            {
                count = 0;
                return null;
            }

            #endregion

            #region Insert

            public override KeyNodeItem? Insert<TArg>(ref InsertArguments<TArg> args, ref NodeRelatives relatives)
            {
                KeyNodeItem? rightLeaf = null;

                var key = args.Key;
                var index = Find(ref key, args.Comparer);

                if (index < 0)
                {
                    index = ~index;

                    Debug.Assert(index >= 0 && index <= Items.Count);

                    var item = new KeyValueItem(args.Key, args.GetValue()); // item to add

                    if (!IsFull) // if there is space, add and return.
                    {
                        Items.Insert(index, item); // insert value and return.
                        AddAndGetSubtreeValueCount(1);
                        relatives.Delta += 1;
                    }
                    else // cant add, spill or split
                    {
                        if (CanSpillTo(Previous))
                        {
                            var first = Items.InsertPopFirst(index, item);
                            Previous.Items.PushLast(first); // move smallest item to left sibling.
                            Previous.AddAndGetSubtreeValueCount(1);
                            relatives.LeftSiblingDelta += 1;

                            // update ancestors key.
                            var pl = relatives.LeftAncestor.Items[relatives.LeftAncestorIndex];
                            KeyNodeItem.ChangeKey(ref pl, Items.First.Key);
                            relatives.LeftAncestor.Items[relatives.LeftAncestorIndex] = pl;

                            Validate(this);
                            Validate(Previous);
                        }
                        else if (CanSpillTo(Next))
                        {
                            var last = Items.InsertPopLast(index, item);
                            Next.Items.PushFirst(last);
                            Next.AddAndGetSubtreeValueCount(1);
                            relatives.RightSiblingDelta += 1;

                            // update ancestors key.
                            var pr = relatives.RightAncestor.Items[relatives.RightAncestorIndex];
                            KeyNodeItem.ChangeKey(ref pr, last.Key);
                            relatives.RightAncestor.Items[relatives.RightAncestorIndex] = pr;

                            Validate(this);
                            Validate(Next);
                        }
                        else // split, then promote middle item
                        {
                            var rightNode = SplitRight();
                            
                            var delta = - rightNode._subtreeValueCount;
                            AddAndGetSubtreeValueCount(delta);
                            relatives.Delta += delta;

                            // new item always insert into right node.
                            // insert item and find middle value to promote
                            if (index <= Items.Count)
                            {
                                // when adding item to this node, pop last item and give it to right node.
                                // this way, this and right split always have equal length or maximum 1 difference. (also avoids overflow when capacity = 1)
                                rightNode.Items.PushFirst(Items.InsertPopLast(index, item));
                            }
                            else if (index > Items.Count)
                            {
                                rightNode.Items.Insert(index - Items.Count, item);
                            }
                            rightNode.AddAndGetSubtreeValueCount(1);

                            rightLeaf = new KeyNodeItem(rightNode.Items.First.Key, rightNode);

                            Validate(this);
                            Validate(rightNode);
                        }
                    }

                    // splits right side to new node and keeps left side for current node.
                }
                else
                {
                    var item = Items[index]; // old item
                    KeyValueItem.ChangeValue(ref item, args.GetUpdateValue(item.Value)); // update item value
                    Items[index] = item; // set new item
                }

                return rightLeaf;
            }

            private bool CanSpillTo(LeafNode leaf)
            {
                return leaf != null ? !leaf.IsFull : false;
            }

            private LeafNode SplitRight()
            {
                var right = new LeafNode(Items.SplitRight());
                if (Next != null)
                {
                    Next.Previous = right;
                    right.Next = Next; // to make linked list.
                }

                right.Previous = this;
                Next = right;
                return right;
            }

            #endregion

            #region Remove

            public override bool Remove(ref RemoveArguments args, ref NodeRelatives relatives)
            {
                var merge = false;
                var key = args.Key;
                var index = Find(ref key, args.Comparer);

                if (index >= 0)
                {
                    Debug.Assert(index >= 0 && index <= Items.Count);

                    args.SetRemovedValue(Items.RemoveAt(index).Value); // remove item
                    AddAndGetSubtreeValueCount(-1);
                    relatives.Delta += -1;

                    if (!IsHalfFull) // borrow or merge
                    {
                        if (CanBorrowFrom(Previous)) // left sibling
                        {
                            var last = Previous.Items.PopLast();
                            Items.PushFirst(last);

                            Previous.AddAndGetSubtreeValueCount(-1);
                            relatives.LeftSiblingDelta += -1;
                            AddAndGetSubtreeValueCount(1);
                            relatives.Delta += 1;

                            var p = relatives.LeftAncestor.Items[relatives.LeftAncestorIndex];
                            KeyNodeItem.ChangeKey(ref p, last.Key);
                            relatives.LeftAncestor.Items[relatives.LeftAncestorIndex] = p;

                            Validate(this);
                            Validate(Previous);
                        }
                        else if (CanBorrowFrom(Next)) // right sibling
                        {
                            var first = Next.Items.PopFirst();
                            Items.PushLast(first);
                            
                            Next.AddAndGetSubtreeValueCount(-1);
                            relatives.RightSiblingDelta += -1;
                            AddAndGetSubtreeValueCount(1);
                            relatives.Delta += 1;

                            var p = relatives.RightAncestor.Items[relatives.RightAncestorIndex];
                            KeyNodeItem.ChangeKey(ref p, Next.Items.First.Key);
                            relatives.RightAncestor.Items[relatives.RightAncestorIndex] = p;

                            Validate(this);
                            Validate(Next);
                        }
                        else // merge with either sibling.
                        {
                            merge = true; // set merge falg
                            if (relatives.HasTrueLeftSibling) // current node will be removed from parent.
                            {
                                var delta = _subtreeValueCount;
                                Previous.Items.MergeLeft(Items); // merge from left to keep items in order.
                                AddAndGetSubtreeValueCount(-delta);
                                relatives.Delta += -delta;
                                Previous.AddAndGetSubtreeValueCount(delta);
                                relatives.LeftSiblingDelta += delta;
                                
                                Previous.Next = Next; // fix linked list
                                if (Next != null) Next.Previous = Previous;

                                Validate(Previous);
                                Validate(Next);
                            }
                            else if (relatives.HasTrueRightSibling) // right sibling will be removed from parent
                            {
                                var delta = Next._subtreeValueCount;
                                Items.MergeLeft(Next.Items); // merge from right to keep items in order. 
                                AddAndGetSubtreeValueCount(delta);
                                relatives.Delta += delta;
                                Next.AddAndGetSubtreeValueCount(-delta);
                                relatives.RightSiblingDelta += -delta;
                                
                                Next = Next.Next; // fix linked list
                                if (Next != null) Next.Previous = this;

                                Validate(this);
                                Validate(Next);
                            }
                            else Debug.Fail("leaf must either have true left or true right sibling.");
                        }
                    }
                }

                return merge; // true if merge happened.
            }

            private bool CanBorrowFrom(LeafNode leaf)
            {
                if (leaf == null) return false;
                return leaf.Items.Count > leaf.Items.Capacity / 2;
            }

            #endregion

            #region Debug

            [Conditional("DEBUG")]
            private static void Validate(LeafNode node)
            {
                if (node == null) return;
                Debug.Assert(node.IsHalfFull);
                Debug.Assert(node.Previous == null || node.Previous.Next == node);
                Debug.Assert(node.Next == null || node.Next.Previous == node);
            }

            #endregion
        }
    }
}