using System.Data;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Collections.Generic;


namespace System {
//Translated from Kotlin to C# by Marie Andry
    public interface IBufferFillStrategy<T> {
        void Fill(List<T> buffer, T elem, bool triggered) { }
    }

    public class CapacityFill<T> : IBufferFillStrategy<T> {
        public void Fill(List<T> buffer, T elem, bool triggered) {
            buffer.Add(elem);
            if (triggered) {
                buffer.RemoveAt(0);
            }
        }
    }

    public class SlidingWindowFill<T> : IBufferFillStrategy<T> {
        private int windowSize;

        public SlidingWindowFill(int windowSize) {
            this.windowSize = windowSize;
        }

        public void Fill(List<T> buffer, T elem, bool triggered) {
            buffer.Add(elem);
            while (buffer.Count > windowSize) {
                buffer.RemoveAt(0);
            }
        }
    }
}