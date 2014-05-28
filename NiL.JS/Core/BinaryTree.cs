﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NiL.JS
{
    /// <summary>
    /// Предоставляет реализацию бинарного дерева поиска со строковым аргументом.
    /// </summary>
    [Serializable]
    internal class BinaryTree<TKey, TValue> : IDictionary<TKey, TValue> where TKey : IComparable
    {
        private sealed class _Values : ICollection<TValue>
        {
            private BinaryTree<TKey, TValue> owner;

            public _Values(BinaryTree<TKey, TValue> owner)
            {
                this.owner = owner;
            }

            public int Count { get { return owner.Count; } }
            public bool IsReadOnly { get { return true; } }

            public void Add(TValue item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(TValue item)
            {
                foreach (var i in owner)
                {
                    if (i.Value.Equals(item))
                        return true;
                }
                return false;
            }

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                if (array.Length - arrayIndex < owner.Count)
                    throw new ArgumentOutOfRangeException();
                foreach (var i in owner)
                    array[arrayIndex++] = i.Value;
            }

            public bool Remove(TValue item)
            {
                throw new NotSupportedException();
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                foreach (var kvp in owner)
                {
                    yield return kvp.Value;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return (this as IEnumerable<TValue>).GetEnumerator();
            }
        }

        private sealed class _Keys : ICollection<TKey>
        {
            private BinaryTree<TKey, TValue> owner;

            public _Keys(BinaryTree<TKey, TValue> owner)
            {
                this.owner = owner;
            }

            public int Count { get { return owner.Count; } }
            public bool IsReadOnly { get { return true; } }

            public void Add(TKey item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(TKey item)
            {
                return owner.ContainsKey(item);
            }

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                if (array.Length - arrayIndex < owner.Count)
                    throw new ArgumentOutOfRangeException();
                foreach (var i in owner)
                    array[arrayIndex++] = i.Key;
            }

            public bool Remove(TKey item)
            {
                throw new NotSupportedException();
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                foreach (var kvp in owner)
                {
                    yield return kvp.Key;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return (this as IEnumerable<TKey>).GetEnumerator();
            }
        }

        [Serializable]
        protected sealed class Node
        {
            public TKey key;
            public TValue value = default(TValue);
            public Node less = null;
            public Node greater = null;
            public int height;

            private void rotateLtM(ref Node _this)
            {
                var temp = less.greater;
                less.greater = _this;
                _this = less;
                less = temp;
            }

            private void rotateMtL(ref Node _this)
            {
                var temp = greater.less;
                greater.less = _this;
                _this = greater;
                greater = temp;
            }

            private void validateHeight()
            {
                height = Math.Max(less != null ? less.height : 0, greater != null ? greater.height : 0) + 1;
            }

            public void Balance(ref Node _this)
            {
                int lessH = 0;
                int greaterH = 0;
                if (less != null)
                {
                    lessH = less.height;
                    if (lessH == 0)
                    {
                        less.Balance(ref less);
                        lessH = less.height;
                    }
                }
                if (greater != null)
                {
                    greaterH = greater.height;
                    if (greaterH == 0)
                    {
                        greater.Balance(ref greater);
                        greaterH = greater.height;
                    }
                }
                int delta = lessH - greaterH;
                if (delta > 1)
                {
                    int llessH = less.less != null ? less.less.height : 0;
                    int lgreaterH = less.greater != null ? less.greater.height : 0;
                    if (llessH < lgreaterH)
                    {
                        less.rotateMtL(ref less);
                        less.less.validateHeight();
                        less.validateHeight();
                    }
                    _this.rotateLtM(ref _this);
                    validateHeight();
                    _this.validateHeight();
                }
                else if (delta < -1)
                {
                    int mlessH = greater.less != null ? greater.less.height : 0;
                    int ggreaterH = greater.greater != null ? greater.greater.height : 0;
                    if (mlessH > ggreaterH)
                    {
                        greater.rotateLtM(ref greater);
                        greater.greater.validateHeight();
                        greater.validateHeight();
                    }
                    _this.rotateMtL(ref _this);
                    validateHeight();
                    _this.validateHeight();
                }
                else
                    height = Math.Max(less != null ? less.height : 0, greater != null ? greater.height : 0) + 1;
            }

            public override string ToString()
            {
                return key + ": " + value;
            }

            public Node()
            {
                height = 1;
            }
        }

        [NonSerialized]
        private long state = 0;
        [NonSerialized]
        private Stack<Node> stack = new Stack<Node>();
        public int Height { get { return root == null ? 0 : root.height; } }
        public int Count { get; private set; }
        public bool IsReadOnly { get { return false; } }
        [NonSerialized]
        private ICollection<TKey> keys;
        public ICollection<TKey> Keys { get { return keys ?? (keys = new _Keys(this)); } }
        [NonSerialized]
        private ICollection<TValue> values;
        public ICollection<TValue> Values { get { return values ?? (values = new _Values(this)); } }
        private Node root;
        protected Node Root { get { return root; } }

        public BinaryTree()
        {
            root = null;
            Count = 0;
            state = DateTime.UtcNow.Ticks;
        }

        public TValue this[TKey key]
        {
            get
            {
                if (key == null)
                    throw new ArgumentNullException();
                TValue res;
                if (!TryGetValue(key, out res))
                    throw new ArgumentException("Key not found.");
                return res;
            }
            set
            {
                if (key == null)
                    throw new ArgumentNullException();
                if (root == null)
                {
                    root = new Node() { value = value, key = key };
                    Count++;
                    state = state ^ state << 1;
                }
                else
                {
                    var c = root;
                    do
                    {
                        var cmp = key.CompareTo(c.key);
                        if (cmp == 0)
                        {
                            c.value = value;
                            return;
                        }
                        else if (cmp > 0)
                        {
                            if (c.greater == null)
                            {
                                c.greater = new Node() { key = key, value = value };
                                c.height = 0;
                                while (stack.Count != 0)
                                    stack.Pop().height = 0;
                                root.Balance(ref root);
                                Count++;
                                state = state ^ state << 1;
                                return;
                            }
                            stack.Push(c);
                            c = c.greater;
                        }
                        else if (cmp < 0)
                        {
                            if (c.less == null)
                            {
                                c.less = new Node() { key = key, value = value };
                                c.height = 0;
                                while (stack.Count != 0)
                                    stack.Pop().height = 0;
                                root.Balance(ref root);
                                Count++;
                                state = state ^ state << 1;
                                return;
                            }
                            stack.Push(c);
                            c = c.less;
                        }
                    }
                    while (true);
                }
            }
        }

        public void Clear()
        {
            Count = 0;
            state = state ^ state << 1;
            root = null;
        }

        public void Add(KeyValuePair<TKey, TValue> keyValuePair)
        {
            Add(keyValuePair.Key, keyValuePair.Value);
        }

        public void Add(TKey key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException();
            if (root == null)
            {
                root = new Node() { value = value, key = key };
                Count++;
                state = state ^ state << 1;
            }
            else
            {
                var c = root;
                var stack = new Stack<Node>();
                do
                {
                    var cmp = key.CompareTo(c.key);
                    if (cmp == 0)
                        throw new ArgumentException();
                    else if (cmp > 0)
                    {
                        if (c.greater == null)
                        {
                            c.greater = new Node() { key = key, value = value };
                            c.height = 0;
                            while (stack.Count != 0)
                                stack.Pop().height = 0;
                            root.Balance(ref root);
                            Count++;
                            state = state ^ state << 1;
                            return;
                        }
                        stack.Push(c);
                        c = c.greater;
                    }
                    else if (cmp < 0)
                    {
                        if (c.less == null)
                        {
                            c.less = new Node() { key = key, value = value };
                            c.height = 0;
                            while (stack.Count != 0)
                                stack.Pop().height = 0;
                            root.Balance(ref root);
                            Count++;
                            state = state ^ state << 1;
                            return;
                        }
                        stack.Push(c);
                        c = c.less;
                    }
                }
                while (true);
            }
        }
#if INLINE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (root == null)
            {
                value = default(TValue);
                return false;
            }
            else
            {
                var c = root;
                do
                {
                    var cmp = key.CompareTo(c.key);
                    if (cmp == 0)
                    {
                        value = c.value;
                        return true;
                    }
                    else if (cmp > 0)
                    {
                        if (c.greater == null)
                        {
                            value = default(TValue);
                            return false;
                        }
                        c = c.greater;
                    }
                    else
                    {
                        if (c.less == null)
                        {
                            value = default(TValue);
                            return false;
                        }
                        c = c.less;
                    }
                }
                while (true);
            }
        }

        public bool ContainsKey(TKey key)
        {
            TValue temp;
            return TryGetValue(key, out temp);
        }

        public bool Contains(KeyValuePair<TKey, TValue> keyValuePair)
        {
            TValue temp;
            return TryGetValue(keyValuePair.Key, out temp) && keyValuePair.Value.Equals(temp);
        }

        public bool Remove(TKey key)
        {
            Node prev = null;
            var c = root;
            var stack = new Stack<Node>();
            do
            {
                var cmp = key.CompareTo(c.key);
                if (cmp == 0)
                {
                    if (c.greater == null)
                    {
                        if (prev == null)
                            root = c.less;
                        else
                        {
                            if (prev.greater == c)
                                prev.greater = c.less;
                            else
                                prev.less = c.less;
                        }
                    }
                    else if (c.less == null)
                    {
                        if (prev == null)
                            root = c.greater;
                        else
                        {
                            if (prev.greater == c)
                                prev.greater = c.greater;
                            else
                                prev.less = c.greater;
                        }
                    }
                    else
                    {
                        var caret = c.less;
                        if (caret.greater != null)
                        {
                            caret.height = 0;
                            var pcaret = c;
                            while (caret.greater != null)
                            {
                                pcaret = caret;
                                caret = caret.greater;
                                caret.height = 0;
                            }
                            pcaret.greater = caret.less;
                            caret.greater = c.greater;
                            caret.less = c.less;
                            if (prev == null)
                                root = caret;
                            else if (prev.greater == c)
                                prev.greater = caret;
                            else
                                prev.less = caret;
                        }
                        else
                        {
                            caret.height = 0;
                            caret.greater = c.greater;
                            if (prev == null)
                                root = caret;
                            else if (prev.greater == c)
                                prev.greater = caret;
                            else
                                prev.less = caret;
                        }
                    }
                    while (stack.Count > 0)
                        stack.Pop().height = 0;
                    root.Balance(ref root);
                    return true;
                }
                else if (cmp > 0)
                {
                    if (c.greater == null)
                        return false;
                    prev = c;
                    stack.Push(c);
                    c = c.greater;
                }
                else
                {
                    if (c.less == null)
                        return false;
                    prev = c;
                    stack.Push(c);
                    c = c.less;
                }
            }
            while (true);
        }

        public bool Remove(KeyValuePair<TKey, TValue> keyValuePair)
        {
            var key = keyValuePair.Key;
            Node prev = null;
            var c = root;
            var stack = new Stack<Node>();
            do
            {
                var cmp = key.CompareTo(c.key);
                if (cmp == 0)
                {
                    if (!keyValuePair.Value.Equals(c.value))
                        return false;
                    if (c.greater == null)
                    {
                        if (prev == null)
                            root = c.less;
                        else
                        {
                            if (prev.greater == c)
                                prev.greater = c.less;
                            else
                                prev.less = c.less;
                        }
                    }
                    else if (c.less == null)
                    {
                        if (prev == null)
                            root = c.greater;
                        else
                        {
                            if (prev.greater == c)
                                prev.greater = c.greater;
                            else
                                prev.less = c.greater;
                        }
                    }
                    else
                    {
                        var caret = c.less;
                        if (caret.greater != null)
                        {
                            caret.height = 0;
                            var pcaret = c;
                            while (caret.greater != null)
                            {
                                pcaret = caret;
                                caret = caret.greater;
                                caret.height = 0;
                            }
                            pcaret.greater = caret.less;
                            caret.greater = c.greater;
                            caret.less = c.less;
                            if (prev == null)
                                root = caret;
                            else if (prev.greater == c)
                                prev.greater = caret;
                            else
                                prev.less = caret;
                        }
                        else
                        {
                            caret.height = 0;
                            caret.greater = c.greater;
                            if (prev == null)
                                root = caret;
                            else if (prev.greater == c)
                                prev.greater = caret;
                            else
                                prev.less = caret;
                        }
                    }
                    while (stack.Count > 0)
                        stack.Pop().height = 0;
                    root.Balance(ref root);
                    return true;
                }
                else if (cmp > 0)
                {
                    if (c.greater == null)
                        return false;
                    prev = c;
                    stack.Push(c);
                    c = c.greater;
                }
                else
                {
                    if (c.less == null)
                        return false;
                    prev = c;
                    stack.Push(c);
                    c = c.less;
                }
            }
            while (true);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException();
            if (index < 0)
                throw new ArgumentOutOfRangeException();
            if (array.Length - index < Count)
                throw new ArgumentException();
            foreach (var kvp in this)
                array[index++] = kvp;
        }

        protected IEnumerator<KeyValuePair<TKey, TValue>> enumerateReversed(Node node)
        {
            if (node != null)
            {
                var sstate = state;
                Node[] stack = new Node[node.height];
                int[] step = new int[node.height];
                int sindex = -1;
                if (node != null)
                {
                    stack[++sindex] = node;
                    while (sindex >= 0)
                    {
                        if (step[sindex] == 0 && stack[sindex].greater != null)
                        {
                            stack[sindex + 1] = stack[sindex].greater;
                            step[sindex] = 1;
                            sindex++;
                            step[sindex] = 0;
                            continue;
                        }
                        if (step[sindex] < 2)
                        {
                            step[sindex] = 2;
                            yield return new KeyValuePair<TKey, TValue>(stack[sindex].key, stack[sindex].value);
                            if (sstate != state)
                                throw new InvalidOperationException("Коллекция была изменена после создания перечислителя.");
                        }
                        if (step[sindex] < 3 && stack[sindex].less != null)
                        {
                            stack[sindex + 1] = stack[sindex].less;
                            step[sindex] = 3;
                            sindex++;
                            step[sindex] = 0;
                            continue;
                        }
                        sindex--;
                    }
                }
            }
        }

        protected IEnumerator<KeyValuePair<TKey, TValue>> enumerate(Node node)
        {
            if (node != null)
            {
                var sstate = state;
                Node[] stack = new Node[node.height];
                int[] step = new int[node.height];
                int sindex = -1;
                if (node != null)
                {
                    stack[++sindex] = node;
                    while (sindex >= 0)
                    {
                        if (step[sindex] == 0 && stack[sindex].less != null)
                        {
                            stack[sindex + 1] = stack[sindex].less;
                            step[sindex] = 1;
                            sindex++;
                            step[sindex] = 0;
                            continue;
                        }
                        if (step[sindex] < 2)
                        {
                            step[sindex] = 2;
                            yield return new KeyValuePair<TKey, TValue>(stack[sindex].key, stack[sindex].value);
                            if (sstate != state)
                                throw new InvalidOperationException("Коллекция была изменена после создания перечислителя.");
                        }
                        if (step[sindex] < 3 && stack[sindex].greater != null)
                        {
                            stack[sindex + 1] = stack[sindex].greater;
                            step[sindex] = 3;
                            sindex++;
                            step[sindex] = 0;
                            continue;
                        }
                        sindex--;
                    }
                }
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return enumerate(root);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (this as IEnumerable<KeyValuePair<TKey, TValue>>).GetEnumerator();
        }
    }

    internal sealed class BinaryTree<TValue> : BinaryTree<string, TValue>
    {
        public IEnumerable<KeyValuePair<string, TValue>> StartedWith(string prefix)
        {
            return StartedWith(prefix, false, 0, int.MaxValue);
        }

        public IEnumerable<KeyValuePair<string, TValue>> StartedWith(string prefix, bool reversed)
        {
            return StartedWith(prefix, reversed, 0, int.MaxValue);
        }

        public IEnumerable<KeyValuePair<string, TValue>> StartedWith(string prefix, bool reversed, long offset)
        {
            return StartedWith(prefix, reversed, offset, int.MaxValue);
        }

        public IEnumerable<KeyValuePair<string, TValue>> StartedWith(string prefix, bool reversed, long offset, long count)
        {
            var c = Root;
            if (c != null)
                do
                {
                    var cmp = c.key.StartsWith(prefix) ? 0 : prefix.CompareTo(c.key);
                    if (cmp == 0)
                    {
                        var enmrtr = reversed ? enumerateReversed(c) : enumerate(c);
                        while (count-- > 0 && enmrtr.MoveNext())
                        {
                            if (offset-- > 0)
                            {
                                count++;
                                continue;
                            }
                            var crnt = enmrtr.Current;
                            if (crnt.Key.StartsWith(prefix))
                            {
                                yield return crnt;
                            }
                        }
                        break;
                    }
                    else if (cmp > 0)
                    {
                        c = c.greater;
                    }
                    else
                    {
                        c = c.less;
                    }
                    if (c == null)
                        break;
                }
                while (true);
        }
    }
}