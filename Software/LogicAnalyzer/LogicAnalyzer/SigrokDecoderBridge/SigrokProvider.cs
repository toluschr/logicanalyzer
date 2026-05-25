using LogicAnalyzer.Classes;
using LogicAnalyzer.Controls;
using LogicAnalyzer.SigrokDecoderBridge;
using SharedDriver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SigrokDecoderBridge
{
    public class SigrokProvider : IDisposable
    {
        private Dictionary<string, IEnumerable<SigrokOutputValue>>? currentInputs; //= new Dictionary<string, IEnumerable<SigrokOutputValue>>();
        private Dictionary<string, IEnumerable<SigrokOutputValue>>? currentOutputs; //= new Dictionary<string, IEnumerable<SigrokOutputValue>>();

        private SigrokDecoderBase[]? decoders;

        public SigrokDecoderBase[] Decoders
        {
            get 
            {
                if (decoders == null)
                    decoders = GetDecoders();

                return decoders.ToArray();
            }
        }

        private SigrokDecoderBase[] GetDecoders()
        {
            var dirs = Directory.GetDirectories(SigrokPythonEngine.DecoderPath);
            var decoders = new List<SigrokDecoderBase>();

            foreach (var dir in dirs)
            {
                if ((Directory.GetFiles(dir, "pd.py")?.Length ?? 0) == 0)
                    continue;

                var decoderName = Path.GetFileNameWithoutExtension(dir);
                // decoderName = Regex.Replace(decoderName, "[^a-zA-Z0-9_]+", "")

                try
                {
                    decoders.Add(new SigrokDecoderBase(decoderName, this));
                }
                catch
                {
                }
            }

            return decoders.ToArray();
        }

        public SigrokProvider()
        {
            if(!SigrokPythonEngine.EnsureInitialized())
                throw new Exception("Python engine could not be initialized");
        }

        public SigrokDecoderBase? GetDecoder(string DecoderName)
        {
            return Decoders.FirstOrDefault(t => t.DecoderName == DecoderName);
        }

        internal IEnumerable<SigrokOutputValue>? GetInput(string InputName)
        { 

            if(currentInputs == null || !currentInputs.ContainsKey(InputName))
                return null;

            return currentInputs[InputName];

            /*
            if(!sessionOutputs.ContainsKey(OutputName))
                return null;

            return sessionOutputs[OutputName];*/
        }

        internal void AddOutput(string OutputName, IEnumerable<SigrokOutputValue> Output)
        {
            if (currentOutputs == null)
                currentOutputs = new Dictionary<string, IEnumerable<SigrokOutputValue>>();
            
            currentOutputs[OutputName] = Output;
        }

        public Dictionary<string, SigrokAnnotation[]>? Execute(int SampleRate, AnalyzerChannel[]? Channels, SigrokDecodingTree Tree)
        {

            if(Channels == null || Tree.Branches.Count == 0)
                return null;

            Dictionary<string, IEnumerable<SigrokOutputValue>> voidInputs = new Dictionary<string, IEnumerable<SigrokOutputValue>>();
            Dictionary<string, SigrokAnnotation[]> result = new Dictionary<string, SigrokAnnotation[]>();

            foreach (var branch in Tree.Branches)
            {
                ExecuteDecodingBranch(branch, SampleRate, Channels, voidInputs, result);
            }

            return result;
        }

        private void ExecuteDecodingBranch(SigrokDecodingBranch branch, int sampleRate, AnalyzerChannel[]? channels, Dictionary<string, IEnumerable<SigrokOutputValue>> Inputs, Dictionary<string, SigrokAnnotation[]> Results)
        {
            if(!branch.Decoder.ValidateOptions(branch.Options, branch.Channels, channels))
                return;

            currentInputs = Inputs;
            currentOutputs = null;

            var result = branch.Decoder.ExecuteAnalysis(sampleRate, branch.Options, branch.Channels, channels);

            if(result != null)
                Results[branch.Name] = result;

            var newOutputs = Inputs;

            if(currentOutputs != null)
                newOutputs = MergeOutputs(newOutputs, currentOutputs);

            foreach(var child in branch.Children)
            {
                ExecuteDecodingBranch(child, sampleRate, channels, newOutputs, Results);
            }
        }

        private Dictionary<string, IEnumerable<SigrokOutputValue>> MergeOutputs(Dictionary<string, IEnumerable<SigrokOutputValue>> OldInputs, Dictionary<string, IEnumerable<SigrokOutputValue>> NewInputs)
        {
            if (NewInputs == null)
                return OldInputs;

            var dict = new Dictionary<string, IEnumerable<SigrokOutputValue>>();

            foreach(var item in NewInputs)
                dict.Add(item.Key, item.Value);

            foreach (var item in OldInputs)
            {
                if (!dict.ContainsKey(item.Key))
                    dict[item.Key] = item.Value;
            }

            return dict;
        }
        
        public void Dispose()
        {
            AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);
            SigrokPythonEngine.EnsureDestroyed();
            AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", false);
        }
    }
}
