using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Common {

    public class FilterUnit<T> {
        public List<T> mapping;
        public float[] probabilities;

        public FilterUnit(List<T> mapping, float[] probabilities) {
            this.mapping = mapping;
            this.probabilities = probabilities;
        }
    }

    // public static class ArgMaxExtension {
    //     public static int ArgMax<T>(this List<T> source) where T : IComparable<T> {
    //         int maxIndex = 0;
    //         T maxValue = source[0];
    //         for (int i = 1; i < source.Count(); i++) {
    //             if (source[i] > maxValue) {
    //                 maxValue = source[i];
    //                 maxIndex = i;
    //             }
    //         }
    //
    //         return maxIndex;
    //     }
    // }

    public static class MathUtil {
        public static int Argmax(List<float> source) {
            int maxIndex = 0;
            float maxValue = source[0];
            for (int i = 1; i < source.Count; i++) {
                if (source[i] > maxValue) {
                    maxIndex = i;
                    maxValue = source[i];
                }
            }
            return maxIndex;;
        }
    }

    public interface PredictionFilter<T> {
        public FilterUnit<T> Filter(FilterUnit<T> input);
    }

    public class PassThroughFilter<T> : PredictionFilter<T> {
        public FilterUnit<T> Filter(FilterUnit<T> input) {
            if (input.mapping.Count != input.probabilities.Length)
                throw new ArgumentException("Prediction filter received mapping that was not the same size as the input");
            if (input.mapping.Count == 0) return input;
            return input;
        }
    }
    
    // TODO: rename to 
    public class PassThroughFilterSingle<T> : PredictionFilter<T> {
        public FilterUnit<T> Filter(FilterUnit<T> input) {
            if (input.mapping.Count != input.probabilities.Length)
                throw new ArgumentException("Prediction filter received mapping that was not the same size as the input");
            if (input.mapping.Count == 0) return input;
            return new FilterUnit<T>(
                new List<T>(new []{input.mapping[MathUtil.Argmax(input.probabilities.ToList())]}),
                new float[] {input.probabilities.Max()}
            );
        }
    }

    public class Thresholder<T> : PredictionFilter<T> {
        private float threshold;
        
        public Thresholder(float threshold) {this.threshold = threshold;}
        
        public FilterUnit<T> Filter(FilterUnit<T> input) {
            if (input.mapping.Count != input.probabilities.Length)
                throw new ArgumentException("Prediction filter received mapping that was not the same size as the input");
            if (input.mapping.Count == 0) return input;
            var filteredMapping = input.mapping
                .Where((value, idx) => input.probabilities[idx] > threshold)
                .ToList();

            var filteredProbabilities = input.probabilities
                .Where(value => value > threshold)
                .ToArray();

            return new FilterUnit<T>(filteredMapping, filteredProbabilities);
        }
    }

    public class FocusSublistFilter<T> : PredictionFilter<T> {
        private List<T> focusSublist;

        public FocusSublistFilter(List<T> focusSublist) {
            this.focusSublist = focusSublist;
        }
        
        public FilterUnit<T> Filter(FilterUnit<T> input) {
            if (input.mapping.Count != input.probabilities.Length)
                throw new ArgumentException("Prediction filter received mapping that was not the same size as the input");
            if (input.mapping.Count == 0) return input;
            
            var indices = input.mapping
                .Select((value, idx) => focusSublist.Contains(value) ? idx : -1)
                .Where(idx => idx != -1)
                .ToList();

            var filteredMapping = indices.Select(idx => input.mapping[idx]).ToList();
            var filteredProbabilities = indices.Select(idx => input.probabilities[idx]).ToArray();
            
            //Debug.Log("Focus Sublist: " + filteredMapping.Count + ", " + filteredProbabilities.Length);

            return new FilterUnit<T>(filteredMapping, filteredProbabilities);
        }
    }
}