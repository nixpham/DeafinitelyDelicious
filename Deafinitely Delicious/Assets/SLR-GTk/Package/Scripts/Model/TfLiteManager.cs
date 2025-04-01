using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using TensorFlowLite;
using System.IO;
using System.Linq;
using Common;
using UnityEngine.Networking;

namespace Model {
	public abstract class TfLiteModelManager<T> {
		protected Interpreter interpreter;
		protected int numThreads = 1;

		protected int maxFrames;
		protected int inputSize;
		protected int outputSize;

		protected TextAsset model;

		protected List<T> mapping;


		protected Interpreter GetInterpreter() {
			if (interpreter == null) {
				interpreter = new Interpreter(model.bytes, new InterpreterOptions()
				{
					threads = numThreads,
				});

				maxFrames = interpreter.GetInputTensorInfo(0).shape[1];
				inputSize = interpreter.GetInputTensorInfo(0).shape[2];
				outputSize = interpreter.GetOutputTensorInfo(0).shape[1];
			}

			return interpreter;
		}
		// since data can be dynamic - have multiple tensors etc - we don't have a Run Model function
		// as that would require different signatures.
	}

	public class SLRTfLiteModel<T> : TfLiteModelManager<T> {
		// float[,,,] modelInputTensor;
		// NativeArray<float> modelOutputTensor;
		// NativeArray<float> modelOutputTensor;
		
		private Dictionary<string, Action<T>> callbacks = new();
		public List<PredictionFilter<T>> outputFilters = new();
		
		private readonly float[] outputs = new float[563];
		private float[] inputs = new float[42 * 60];

		public SLRTfLiteModel(TextAsset model, List<T> mapping) {
			this.model = model;
			GetInterpreter();
			interpreter.AllocateTensors();
			// modelInputTensor = new float[1, maxFrames, inputSize, 1];
			// modelOutputTensor = new NativeArray<float>(outputSize, Allocator.Persistent);
			this.mapping = mapping;
			outputFilters.Add(new PassThroughFilterSingle<T>());
		}

		public void RunModel(float[] data) {
			// Debug.Log("Data: " + data.Length + " -> " + string.Join(", ", data));
			Array.Copy(data, inputs, data.Length);
			interpreter.SetInputTensorData(0, inputs);
			// Debug.Log("invoke");
			interpreter.Invoke();

			// interpreter.GetOutputTensorData(0, modelOutputTensor.AsSpan());
			
			interpreter.GetOutputTensorData(0, outputs);
			float[] sendOutputs = new float[outputs.Length];
			Array.Copy(outputs, sendOutputs, outputs.Length);

			// Debug.Log("outputs: " + string.Join(", ", sendOutputs));

			// float[] outputs = modelOutputTensor.ToArray();

			FilterUnit<T> output = new FilterUnit<T>(mapping, sendOutputs);

			foreach (var filter in outputFilters)
			{
				output = filter.Filter(output);
			}

			foreach (var callback in callbacks) {
				if (output.mapping.Count == 1) {
					callbacks[callback.Key].Invoke(output.mapping[0]);
				}
				else if (output.mapping.Count > 1) {
					callbacks[callback.Key].Invoke(output.mapping[MathUtil.Argmax(output.probabilities.ToList())]);
				}
			}
			// modelOutputTensor.Dispose();
		}
		
		public void AddCallback(string name, Action<T> callback) {
			if (callbacks.ContainsKey(name)) callbacks.Remove(name);
			callbacks.Add(name, callback);
		}

		public void RemoveCallback(string name) {
			callbacks.Remove(name);
		}
	}
}