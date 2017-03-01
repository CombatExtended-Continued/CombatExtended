using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended.AI
{
	public class BetterPQueue<T>
	{
		private readonly SortedDictionary<HeapKey, HeapKey> Heap;

		public BetterPQueue(){
			this.Heap = new
				SortedDictionary<HeapKey, HeapKey>();
		}

		public void Push(T item,float Score)
		{
			var NewKey =
				new HeapKey(Guid.NewGuid(),Score, item);

			this.Heap.Add(NewKey, NewKey);
		}

		public T getMin()
		{
			var minValue =
				this.Heap.Min().Value.ObjPointer;

			this.Heap.Remove(Heap.Min().Key);

			return minValue;
		}

		public Boolean isEmpty()
		{
			return this.Heap.Count == 0;
		}

		public void Clear() => this.Heap.Clear();

		private class HeapKey : IComparable<HeapKey>
		{
			public HeapKey(Guid id, float value,T objPointer)
			{
				Id = id;
				Value = value;
				ObjPointer = objPointer;
			}

			public Guid Id { get; private set; }
			public float Value { get; private set; }

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


}
