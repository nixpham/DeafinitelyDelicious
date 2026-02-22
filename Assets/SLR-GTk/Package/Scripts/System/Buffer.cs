using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;


namespace System {
    public class Buffer<T> {
        List<T> internalBuffer = new();

        Dictionary<string, Action<List<T>>> callbacks = new();
        //commented out because we don't have SLREventHandler yet

        public IBufferTriggerStrategy<T> trigger = new CapacityFullTrigger<T>(60);
        public IBufferFillStrategy<T> filler = new CapacityFill<T>();

        public void AddElement(T elem) {

            // TODO - idk what tolist does here - in kotlin it copies but might not be doing that here
            List<T> temp = internalBuffer.ToList();
            bool triggered = trigger.Check(temp);
            filler.Fill(internalBuffer, elem, triggered);
            //can't use linked list internalBuffer for methods that accept List<T>
            //made temp so that I could still used these methods with internalBuffer

            if (triggered) {
                TriggerCallbacks();
            }

            // Debug.Log(internalBuffer.Count);

        }

        public void AddCallback(string name, Action<List<T>> callback) {
            if (callbacks.ContainsKey(name)) callbacks.Remove(name);
            callbacks.Add(name, callback);
        }

        public void RemoveCallback(string name) {
            callbacks.Remove(name);
        }

        public void TriggerCallbacks() {
            var copy = internalBuffer.ToList();
           //Debug.Log("On Trigger got: " + copy.Count);
            foreach (var callback in callbacks) {
                callback.Value(copy);
            }
        }

        public void Clear() {
            internalBuffer.Clear();
        }
    }
}