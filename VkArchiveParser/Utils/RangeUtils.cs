using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VkArchiveParser.Utils
{
    public static class RangeUtils
    {
        public static Range GetPagination(this Range Pages, int Current, int Radius = 3)
        {
            if (Pages.End.Value - Pages.Start.Value < Radius * 2) return Pages;
            if (Current < Pages.Start.Value + Radius) return Pages.Start.Value..(Pages.Start.Value + Radius * 2);
            if (Current > Pages.End.Value - Radius) return (Pages.End.Value - Radius * 2)..Pages.End.Value;
            return (Current - Radius)..(Current + Radius);
        }
        public static RangeEnumerator GetEnumerator(this Range @this) => (@this.Start, @this.End) switch
        {
            ({ IsFromEnd: true, Value: 0 }, { IsFromEnd: true, Value: 0 }) => new RangeEnumerator(0, int.MaxValue, 1),
            ({ IsFromEnd: true, Value: 0 }, { IsFromEnd: false, Value: var to }) => new RangeEnumerator(0, to + 1, 1),
            ({ IsFromEnd: false, Value: var from }, { IsFromEnd: true, Value: 0 }) => new RangeEnumerator(from, int.MaxValue, 1),
            ({ IsFromEnd: false, Value: var from }, { IsFromEnd: false, Value: var to }) => new RangeEnumerator(from, to, from < to ? 1 : -1),
            _ => throw new InvalidOperationException("Invalid range")
        };
        //Slightly more compact version, however its allowing starting from the end which is not recommended...
        /*public static RangeEnumerator GetEnumerator(this Range @this)
        {
            var from = !@this.Start.IsFromEnd ? @this.Start.Value : int.MaxValue - @this.Start.Value;
            var to = !@this.End.IsFromEnd ? @this.End.Value : int.MaxValue - @this.End.Value;
            return new RangeEnumerator(from, to, from < to ? 1 : -1);
        }*/
        public struct RangeEnumerator
        {
            private readonly int to, step;
            private int curr;
            internal void Deconstruct(out int from, out int to, out int step) => (from, to, step) = (curr, this.to, this.step);
            internal RangeEnumerator(int from, int to, int step)
            {
                this.to = to + (this.step = step);
                curr = from - step;
            }
            public bool MoveNext() => (curr += step) != to;

            public int Current => curr;
        }
    }
}
