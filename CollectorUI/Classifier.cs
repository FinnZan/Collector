using Dell.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace CollectorUI
{
    public class TestResult
    {
        public List<KeyValuePair<string, double>> Scores = new List<KeyValuePair<string, double>>();

        public override string ToString()
        {
            return $"[{Scores[0].Key}] [{Scores[0].Value}]";
        }
    }

    public class Classifier
    {
        private Process _process = null;

        public string PythonDir { get; private set; }

        public string ImageDir { get; private set; }

        public string BottleneckDir { get; private set; }

        public string OutputGraph { get; private set; }

        public string IntermediateOutputGraphsDir { get; private set; }

        public string OutputLabels { get; private set; }

        public string SummariesDir { get; private set; }

        public string[] Classes { get; set; }

        public Classifier(string name, string img_dir, string working_dir, string python_dir)
        {
            ImageDir = img_dir;
            PythonDir = python_dir;

            BottleneckDir = Path.Combine(working_dir, "bottleneck");
            IntermediateOutputGraphsDir = Path.Combine(working_dir, "intermediate_graph");
            OutputGraph = Path.Combine(working_dir, $"{name}_graph.pb");
            OutputLabels = Path.Combine(working_dir, $"{name}_lables.txt");
            SummariesDir = Path.Combine(working_dir, $"logs");

            var folders = Directory.GetDirectories(BottleneckDir);
            Classes = new string[folders.Length];
            for (int i = 0; i < folders.Length; i++)
            {
                Classes[i] = Path.GetFileName(folders[i]);
            }

            if (!Directory.Exists(BottleneckDir))
            {
                Directory.CreateDirectory(BottleneckDir);
                CommonTools.Log($"Creating directory [{BottleneckDir}]");
            }

            if (!Directory.Exists(IntermediateOutputGraphsDir))
            {
                Directory.CreateDirectory(IntermediateOutputGraphsDir);
                CommonTools.Log($"Creating directory [{IntermediateOutputGraphsDir}]");
            }

            if (!Directory.Exists(SummariesDir))
            {
                Directory.CreateDirectory(SummariesDir);

                CommonTools.Log($"Creating directory [{SummariesDir}]");
            }
        }

        public void Train()
        {
            try
            {
                var script = Path.Combine(PythonDir, "retrain.py");
                string cmd = $"{script} --image_dir {ImageDir} --bottleneck_dir {BottleneckDir} --output_graph {OutputGraph} --intermediate_output_graphs_dir {IntermediateOutputGraphsDir} --output_labels {OutputLabels} --summaries_dir {SummariesDir}";
                CommonTools.Log(cmd);

                _process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = cmd,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                    }
                };

                new Thread(() =>
                {
                    CommonTools.Log("Started.");
                    _process.OutputDataReceived += new DataReceivedEventHandler((object sender, DataReceivedEventArgs args) =>
                    {
                        CommonTools.Log(args.Data);
                    });

                    _process.ErrorDataReceived += new DataReceivedEventHandler((object sender, DataReceivedEventArgs args) =>
                    {
                        CommonTools.Log(args.Data);
                    });
                    _process.Start();
                    _process.BeginErrorReadLine();
                    _process.BeginOutputReadLine();
                    _process.WaitForExit();
                    CommonTools.Log("Done.");

                }).Start();
            }
            catch (Exception ex)
            {
                CommonTools.HandleException(ex);
            }
        }

        public TestResult Test(string img)
        {
            var script = Path.Combine(PythonDir, "label_image.py");
            var cmd = $"{script} --graph={OutputGraph} --labels={OutputLabels} --input_layer=Placeholder --output_layer=final_result --image={img}";

            CommonTools.Log(cmd);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = cmd,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            };

            process.Start();

            if (!process.HasExited)
            {
                process.WaitForExit();
            }

            TestResult ret = new TestResult(); ;

            if (!process.StandardOutput.EndOfStream)
            {
                var output = process.StandardOutput.ReadToEnd();
                CommonTools.Log(output.Trim());

                var pairs = output.Split('\n');
                foreach (var p in pairs)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(p))
                        {
                            var t = p.Split(' ');
                            ret.Scores.Add(new KeyValuePair<string, double>(t[0], double.Parse(t[1])));
                        }
                    }
                    catch (Exception ex)
                    {
                        CommonTools.HandleException(ex);
                    }                    
                }

                ret.Scores.Sort(new Comparison<KeyValuePair<string, double>>((KeyValuePair<string, double> a, KeyValuePair<string, double> b) =>
                {
                    return b.Value.CompareTo(a.Value);
                }));
            }

            if (!process.StandardError.EndOfStream)
            {
                var error = process.StandardError.ReadToEnd();
                CommonTools.Log($"Error [{error}]");
            }
            
            return ret;
        }

        public void Stop()
        {
            try
            {
                _process?.Kill();
            }
            catch (Exception ex)
            {
                CommonTools.HandleException(ex);
            }
        }
    }
}
