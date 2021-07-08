using System;
using System.Runtime.CompilerServices;

namespace CombatExtended
{
    /*
     * Taken RocketMan by Karim
     * 
     * https://github.com/kbatbouta/RimWorld-RocketMan/blob/development-1.3-unstable/Source/Cosmodrome/Core/Collections/FlagsArray.cs
     * 
     * Reasoning:
     * 
     * This provide a very efficent way to store a lot of flags for defs.
     */
    public class FlagArray
    {
        private int[] map;

        private const int Bit = 1;

        /// <summary>
        /// The length of the data type used for storing the bits in a bitmap
        /// </summary>
        private const int ChunkSize = 32;

        /// <summary>
        /// Used to access the size of the internel flag array
        /// </summary>
        /// <returns>Current array length</returns>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => map.Length;
        }

        /// <summary>
        /// Create an instant of FlagArray (int32 bitmap). Make sure that the size is large enough.
        /// No need to worry about memory since this will be 1 bit per bool.
        /// </summary>
        /// <param name="size">The maximum size of this array (bool count)</param>
        public FlagArray(int size)
        {
            this.map = new int[(size / ChunkSize) + ChunkSize];
        }

        /// <summary>
        /// Used to communicate with the flag array as an array
        /// </summary>
        /// <param name="key">Flag index</param>
        /// <returns>Flag value</returns>
        public bool this[int key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Get(key);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Set(key, value);
        }

        /// <summary>
        /// Used to access flags values.
        /// </summary>
        /// <param name="key">Flag index</param>
        /// <returns>Return if the flag is set</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(int key)
        {
            return (map[key / ChunkSize] & GetOp(key)) != 0;
        }
        /// <summary>
        /// Used to set flags values.
        /// </summary>
        /// <param name="key">Flag index</param>
        /// <param name="value">Flag value</param>
        /// <returns>return self</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FlagArray Set(int key, bool value)
        {
            map[key / ChunkSize] = value ?
                map[key / ChunkSize] | GetOp(key) :
                map[key / ChunkSize] & ~GetOp(key);
            return this;
        }

        /// <summary>
        /// Used to expand the size of the flag array.
        /// </summary>
        /// <param name="targetLength">The new target length</param>
        public void Expand(int targetLength)
        {
            targetLength = (targetLength / ChunkSize) + ChunkSize;
            if (targetLength > Length * 0.75f)
            {
                int[] expanded = new int[targetLength];
                Array.Copy(map, 0, expanded, 0, map.Length);
                this.map = expanded;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetOp(int key)
        {
            return Bit << (key % ChunkSize);
        }
    }
}
