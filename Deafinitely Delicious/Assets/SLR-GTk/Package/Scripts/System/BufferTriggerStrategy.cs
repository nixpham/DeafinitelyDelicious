using System.Collections.Generic;

namespace System {
    public interface IBufferTriggerStrategy<T> {
        bool Check(List<T> buffer) {
            return false;
        }
    }

    class CapacityFullTrigger<T> : IBufferTriggerStrategy<T> {
        private int capacity;

        public CapacityFullTrigger(int capacity) {
            this.capacity = capacity;
        }

        public bool Check(List<T> buffer) {
            return buffer.Count == capacity;
        }
    }

    class NoTrigger<T> : IBufferTriggerStrategy<T> {
        public bool Check(List<T> buffer) {
            return false;
        }
    }
}
