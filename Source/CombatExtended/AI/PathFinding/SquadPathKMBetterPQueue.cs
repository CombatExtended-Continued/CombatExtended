﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using System.Collections;

namespace CombatExtended.AI
{
    public class BetterPQueue<T>
    {
        //private readonly SortedDictionary<HeapKey, HeapKey> Heap;

        private MinHeap<HeapKey> heap;

        public BetterPQueue()
        {
            //this.Heap = new
            //  SortedDictionary<HeapKey, HeapKey>();
            heap = new MinHeap<HeapKey>();
        }

        public void Push(T item, float Score)
        {
            var NewKey =
                new HeapKey(Guid.NewGuid(), Score, item);

            this.heap.Add(NewKey);
        }

        public T getMin()
        {
            var minValue =
                this.heap.GetMin();

            this.heap.ExtractDominating();

            return minValue.ObjPointer;
        }

        public Boolean isEmpty()
        {
            return this.heap.Count == 0;
        }

        private class HeapKey : IComparable<HeapKey>
        {
            public HeapKey(Guid id, float value, T objPointer)
            {
                Id = id;
                Value = value;
                ObjPointer = objPointer;
            }

            public Guid Id
            {
                get;
                private set;
            }
            public float Value
            {
                get;
                private set;
            }

            //This is the Pointer to The Real Node
            public T ObjPointer;

            public int CompareTo(HeapKey other)
            {
                if (other == null)
                {
                    throw new ArgumentNullException("other");
                }

                var result = Value.CompareTo(other.Value);

                return result == 0 ? Id.CompareTo(other.Id) : result;
            }
        }
    }

    public abstract class Heap<T> : IEnumerable<T>
    {
        private const int InitialCapacity = 0;
        private const int GrowFactor = 2;
        private const int MinGrow = 1;

        private int _capacity = InitialCapacity;
        private T[] _heap = new T[InitialCapacity];
        private int _tail = 0;

        public int Count
        {
            get
            {
                return _tail;
            }
        }
        public int Capacity
        {
            get
            {
                return _capacity;
            }
        }

        protected Comparer<T> Comparer
        {
            get;
            private set;
        }
        protected abstract bool Dominates(T x, T y);

        protected Heap() : this(Comparer<T>.Default)
        {
        }

        protected Heap(Comparer<T> comparer) : this(Enumerable.Empty<T>(), comparer)
        {

        }

        protected Heap(IEnumerable<T> collection)
            : this(collection, Comparer<T>.Default)
        {
        }

        protected Heap(IEnumerable<T> collection, Comparer<T> comparer)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }
            if (comparer == null)
            {
                throw new ArgumentNullException("comparer");
            }

            Comparer = comparer;

            foreach (var item in collection)
            {
                if (Count == Capacity)
                {
                    Grow();
                }

                _heap[_tail++] = item;
            }

            for (int i = Parent(_tail - 1); i >= 0; i--)
            {
                BubbleDown(i);
            }
        }

        public void Add(T item)
        {
            if (Count == Capacity)
            {
                Grow();
            }

            _heap[_tail++] = item;
            BubbleUp(_tail - 1);
        }

        private void BubbleUp(int i)
        {
            if (i == 0 || Dominates(_heap[Parent(i)], _heap[i]))
            {
                return;    //correct domination (or root)
            }

            Swap(i, Parent(i));
            BubbleUp(Parent(i));
        }

        public T GetMin()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("Heap is empty");
            }
            return _heap[0];
        }

        public T ExtractDominating()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("Heap is empty");
            }
            T ret = _heap[0];
            _tail--;
            Swap(_tail, 0);
            BubbleDown(0);
            return ret;
        }

        private void BubbleDown(int i)
        {
            int dominatingNode = Dominating(i);
            if (dominatingNode == i)
            {
                return;
            }
            Swap(i, dominatingNode);
            BubbleDown(dominatingNode);
        }

        private int Dominating(int i)
        {
            int dominatingNode = i;
            dominatingNode = GetDominating(YoungChild(i), dominatingNode);
            dominatingNode = GetDominating(OldChild(i), dominatingNode);

            return dominatingNode;
        }

        private int GetDominating(int newNode, int dominatingNode)
        {
            if (newNode < _tail && !Dominates(_heap[dominatingNode], _heap[newNode]))
            {
                return newNode;
            }
            else
            {
                return dominatingNode;
            }
        }

        private void Swap(int i, int j)
        {
            T tmp = _heap[i];
            _heap[i] = _heap[j];
            _heap[j] = tmp;
        }

        private static int Parent(int i)
        {
            return (i + 1) / 2 - 1;
        }

        private static int YoungChild(int i)
        {
            return (i + 1) * 2 - 1;
        }

        private static int OldChild(int i)
        {
            return YoungChild(i) + 1;
        }

        private void Grow()
        {
            int newCapacity = _capacity * GrowFactor + MinGrow;
            var newHeap = new T[newCapacity];
            Array.Copy(_heap, newHeap, _capacity);
            _heap = newHeap;
            _capacity = newCapacity;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _heap.Take(Count).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class MaxHeap<T> : Heap<T>
    {
        public MaxHeap()
            : this(Comparer<T>.Default)
        {
        }

        public MaxHeap(Comparer<T> comparer)
            : base(comparer)
        {
        }

        public MaxHeap(IEnumerable<T> collection, Comparer<T> comparer)
            : base(collection, comparer)
        {
        }

        public MaxHeap(IEnumerable<T> collection) : base(collection)
        {
        }

        protected override bool Dominates(T x, T y)
        {
            return Comparer.Compare(x, y) >= 0;
        }
    }

    public class MinHeap<T> : Heap<T>
    {
        public MinHeap()
            : this(Comparer<T>.Default)
        {
        }

        public MinHeap(Comparer<T> comparer)
            : base(comparer)
        {
        }

        public MinHeap(IEnumerable<T> collection) : base(collection)
        {
        }

        public MinHeap(IEnumerable<T> collection, Comparer<T> comparer)
            : base(collection, comparer)
        {
        }

        protected override bool Dominates(T x, T y)
        {
            return Comparer.Compare(x, y) <= 0;
        }
    }


}
