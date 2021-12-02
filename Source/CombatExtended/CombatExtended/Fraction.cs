using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CombatExtended
{
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public readonly struct Fraction : IComparable<float>, IComparable<Fraction>, IComparable<int>
    {
        private const int LIMIT = int.MaxValue / 2;

        [FieldOffset(0)]private readonly int top;
        [FieldOffset(4)]private readonly int bottom;

        public bool Negative
        {
            get => ((top ^ bottom) < 0);
        }

        private int TopAbs
        {
            get
            {
                int t = (top >> 31);
                return (top ^ t) - t;
            }
        }
        private int BottomAbs
        {
            get
            {
                int t = (top >> 31);
                return (top ^ t) - t;
            }
        }

        public Fraction(int top, int bottom)
        {
            if (bottom == 0)
            {
                throw new ArgumentException("Denominator cannot be zero.", nameof(bottom));
            }
            this.top = top;
            this.bottom = bottom;            
        }

        public int Floor()
        {
            if (top == 0)
            {
                return 0;
            }
            if (Negative)
            {
                return top / bottom - (TopAbs % BottomAbs == 0 ? 0 : 1);
            }
            return top / bottom;
        }

        public int Ceil()
        {
            if (top == 0)
            {
                return 0;
            }
            if (Negative)
            {
                return top / bottom;
            }
            return top / bottom + (TopAbs % BottomAbs == 0 ? 0 : 1);
        }       

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(float other)
            => this.top.CompareTo(other * this.bottom);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(Fraction other)
            => (this.top * other.bottom).CompareTo(other.top * this.bottom);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(int other)
            => this.top.CompareTo(other * this.bottom);

        public override string ToString()
           => $"{top} / {bottom}";

        public override int GetHashCode()
            => top.GetHashCode() ^ bottom.GetHashCode();

        public override bool Equals(object obj)
            => obj is Fraction f && f == this;


        public static Fraction operator +(Fraction a, int b) => new Fraction(a.top + b, a.bottom);
        public static Fraction operator -(Fraction a, int b) => new Fraction(a.top - b, a.bottom);
        public static Fraction operator *(Fraction a, int b) => new Fraction(a.top * b, a.bottom);
        public static Fraction operator /(Fraction a, int b) => new Fraction(a.top, a.bottom * b);        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fraction operator +(Fraction a) => a;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fraction operator -(Fraction a) => new Fraction(-a.top, a.bottom);
        public static Fraction operator +(Fraction a, Fraction b)
            => new Fraction(a.top * b.bottom + b.top * a.bottom, a.bottom * b.bottom);
        public static Fraction operator -(Fraction a, Fraction b) => a + (-b);
        public static Fraction operator *(Fraction a, Fraction b)
            => new Fraction(a.top * b.top, a.bottom * b.bottom);
        public static Fraction operator /(Fraction a, Fraction b)
        {
            if (b.top == 0)
            {
                throw new DivideByZeroException();
            }
            return new Fraction(a.top * b.bottom, a.bottom * b.top);
        }
        public static bool operator ==(Fraction a, Fraction b) => a.top == b.top && a.bottom == b.bottom;
        public static bool operator !=(Fraction a, Fraction b) => a.top != b.top || a.bottom != b.bottom;        
    }
}

