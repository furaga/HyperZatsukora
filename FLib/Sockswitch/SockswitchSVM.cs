using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using libsvm;
using ILNumerics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace FLib
{
    public class SockswitchSVM
    {
        [DllImport("LIBSVMBind.dll")]
        static extern int svm_init(char[] model_file);
        [DllImport("LIBSVMBind.dll")]
        static extern void svm_dispose();
        [DllImport("LIBSVMBind.dll")]
        static extern double svm_easy_predict(int n, double[] features, double[] probabilities);


        public static int intensitySmooth = 3;
        public static int intensityPowK = 8;
        public static float segmentHighThreshold = 0.4f;
        public static float segmentLowThreshold = 0.1f;

        int lastGestureFinishFrame = 0;

        public C_SVC svm = null;
        List<double> scaleMax = new List<double>();
        List<double> scaleMin = new List<double>();
        static List<bool> scaleMask = new List<bool>();
        int[] baseline_cnts;
        Queue<double>[] baseline_history;
        Queue<double>[] baseline_weights;
        double[] baseline_prevs;
        public List<double[]> baselines = new List<double[]>();
        Queue<double>[] window_dataHistory;
        public List<double> window = new List<double>();
        public List<CharacterRange> windowHighRanges = new List<CharacterRange>();
        public List<CharacterRange> windowLowRanges = new List<CharacterRange>();
        static Dictionary<string, bool> featuresFilter = new Dictionary<string, bool>();

       static bool use(string featureName) { return featuresFilter.ContainsKey(featureName) && featuresFilter[featureName]; }

        public SockswitchSVM()
        {

        }

        public void Reset()
        {
            baseline_cnts = null;
            baseline_history = null;
            baseline_weights = null;
            baseline_prevs = null;
            baselines = new List<double[]>();
            window_dataHistory = null;
            window = new List<double>();
            windowLowRanges = new List<CharacterRange>();
            windowHighRanges = new List<CharacterRange>();

            lastActivateFrame = 0;
            lastActivateLowWindow = -1;
        }

        int nClass = 0;
        Dictionary<int, double> scaleMinD = new Dictionary<int, double>();
        Dictionary<int, double> scaleMaxD = new Dictionary<int, double>();
        public void Load(string modelFile, string rangeFile)
        {
            nClass = svm_init(modelFile.ToCharArray());

            var ranges = System.IO.File.ReadAllLines(rangeFile)
                .Skip(2)
                .Select(s => s.Split(' ').Select(t => double.Parse(t)).ToArray())
                .ToArray();

            scaleMinD.Clear();
            scaleMaxD.Clear();
            for (int i = 0; i <  ranges.Length; i++)
            {
                scaleMinD[(int)ranges[i][0]] = ranges[i][1];
                scaleMaxD[(int)ranges[i][0]] = ranges[i][2];
            }
        }
        public void Dispose()
        {
            svm_dispose();
        }

        public Dictionary<int, double> Predict(double[] features)
        {
            Dictionary<int, double> res = new Dictionary<int,double>();
            double[] probabilities = new double[nClass];
            int label = (int)svm_easy_predict(features.Length, features, probabilities);
            res[label] = 1;
//            for (int i = 0; i < probabilities.Length; i++)
  //          {
    //            res[i] = probabilities[i];
      //      }
            return res;
        }

        public void Load(string trainFile, double C, double gamma)
        {
            var raw_prob = ProblemHelper.ReadProblem(trainFile);
            var prob = ProblemHelper.ScaleProblem(raw_prob);
            svm = new C_SVC(prob, KernelHelper.RadialBasisFunctionKernel(gamma), C);

            // new C_SVC("trainedModel.xml");//prob, KernelHelper.RadialBasisFunctionKernel(gamma), C);
            // svm.Export("model.xml");

            // スケーリング用に値域を記録
            scaleMax.Clear();
            scaleMin.Clear();
            for (int j = 0; j < raw_prob.x[0].Length; j++)
            {
                scaleMax.Add(double.MinValue);
                scaleMin.Add(double.MaxValue);
            }

            for (int i = 0; i < raw_prob.x.Length; i++)
            {
                for (int j = 0; j < raw_prob.x[i].Length; j++)
                {
                    scaleMax[j] = Math.Max(scaleMax[j], raw_prob.x[i][j].value);
                    scaleMin[j] = Math.Min(scaleMin[j], raw_prob.x[i][j].value);
                }
            }


            for (int i = 0; i < prob.x.Length; i++)
            {
                Console.WriteLine("predict" + svm.Predict(prob.x[i]));
            }
        }

        public svm_node[] Scaling(svm_node[] x)
        {
//            System.Diagnostics.Debug.Assert(scaleMin.Count == scaleMax.Count);
  //          System.Diagnostics.Debug.Assert(scaleMax.Count == x.Length);

            const double lower = -1;
            const double upper = 1;

            var sx = new svm_node[x.Length];
            for (int i = 0; i < sx.Length; i++)
            {
                if (scaleMinD.ContainsKey(x[i].index))
                {
                    double max = scaleMaxD[x[i].index];
                    double min = scaleMinD[x[i].index];
                    double val = max - min <= 0.00001f ? 0 : scaleMask[x[i].index - 1] ? lower + (upper - lower) * (x[i].value - min) / (max - min) : x[i].value;
                    sx[i] = new svm_node()
                    {
                        index = x[i].index,
                        value = val,
                    };
                }
                else
                {
                    sx[i] = new svm_node()
                    {
                        index = x[i].index,
                        value = 0,
                    };
                }
/*
                double max = scaleMax[x[i].index - 1];
                double min = scaleMin[x[i].index - 1];
                double val = max - min <= 0.00001f? 0 : scaleMask[x[i].index - 1] ? lower + (upper - lower) * (x[i].value - min) / (max - min) : x[i].value;
                sx[i] = new svm_node()
                {
                    index = x[i].index,
                    value = val,
                };
  */          }

            return sx;
        }


        public double[] Baselines(List<double>[] raw_waves, int frameIndex, int smooth)
        {
            double[] baseline = new double[raw_waves.Length];
            if (baseline_cnts == null)
            {
                baseline_cnts = new int[raw_waves.Length];
                baseline_history = new Queue<double>[raw_waves.Length];
                baseline_weights = new Queue<double>[raw_waves.Length];
                for (int i = 0; i < baseline_history.Length; i++)
                {
                    baseline_history[i] = new Queue<double>();
                    baseline_weights[i] = new Queue<double>();
                }
                baseline_prevs = new double[raw_waves.Length];
            }
            for (int sIdx = 0; sIdx < raw_waves.Length; sIdx++)
            {
                double val = raw_waves[sIdx][frameIndex];
                double diff = Math.Abs(baseline_history[sIdx].Count < 3 ? 0 : baseline_history[sIdx].Average() - val);

                baseline_history[sIdx].Enqueue(val);
                baseline_weights[sIdx].Enqueue(diff);
                while (baseline_history[sIdx].Count > smooth)
                {
                    baseline_history[sIdx].Dequeue();
                    baseline_weights[sIdx].Dequeue();
                }
               
                if (baseline_history[sIdx].Count >= smooth)
                {
                    double average = baseline_history[sIdx].Average();
                    double diffAverage = baseline_weights[sIdx].Average();
                    baseline[sIdx] = Math.Exp(-diffAverage * 10) * average + (1 - Math.Exp(-diffAverage * 10)) * baseline_prevs[sIdx];   //average;
                }

                baseline_prevs[sIdx] = baseline[sIdx];
            }

            baselines.Add(baseline);

            return baseline;
        }

        double Intensity(double[] baseline)
        {
            double intensity = 0;
            double powK = 8;
            if (baseline_history[0].Count >= 3)
            {
                double[] diffs = new double[baseline_history.Length];
                int cnt = 0;
                for (int i = 0; i < diffs.Length; i++)
                {
                    // センサがまともな値だったら
                    if (baseline_history[i].Max() <= 1)
                    {
                        diffs[i] = Math.Abs(baseline_history[i].Last() - baseline[i]);
                        intensity += Math.Exp(powK * diffs[i]) - 1;
                        cnt++;
                    }
                }
                if (cnt >= 1)
                {
                    intensity /= cnt;
                }
            }
            return intensity;
        }

        public double Segment(List<double>[] raw_waves, List<double[]> baselines, int frameIndex, int smooth, double powK, double answer)
        {
            double intensity = answer;// Intensity(baselines[frameIndex]);
//            window.Add(intensity);
            
            if (window.Count >= 2)
            {
                int idx = window.Count - 1;
                if (window[idx - 1] < segmentHighThreshold && segmentHighThreshold <= window[idx])
                {
                    windowHighRanges.Add(new CharacterRange(idx, 0));
                }
                if (window[idx - 1] >= segmentHighThreshold && segmentHighThreshold > window[idx] && windowHighRanges.Count >= 1)
                {
                    var range = windowHighRanges.Last();
                    range.Length = idx - range.First;
                    windowHighRanges[windowHighRanges.Count - 1] = range;
                }
                if (window[idx - 1] < segmentLowThreshold && segmentLowThreshold <= window[idx])
                {
                    windowLowRanges.Add(new CharacterRange(idx, 0));
                }
                if (window[idx - 1] >= segmentLowThreshold && segmentLowThreshold > window[idx] && windowLowRanges.Count >= 1)
                {
                    var range = windowLowRanges.Last();
                    range.Length = idx - range.First;
                    windowLowRanges[windowLowRanges.Count - 1] = range;
                }
            }

            return intensity;
        }

        int lastActivateFrame = 0;
        int lastActivateLowWindow = -1;
        CharacterRange lastActivateRange = new CharacterRange();

        public bool CanRecognize(out CharacterRange range, int frameIdx)
        {
            lastGestureFinishFrame = 0;
            var frames = GetAllFrames();

            if (frames.Count <= 0)
            {
                range = new CharacterRange();
                return false;
            }
            var _range = frames.Last();
            range = _range;// GetFrame(lastActivateLowWindow);
            if (range != lastActivateRange)
            {
                return (range.First <= 160 && 160 <= range.First + range.Length);// && 0.1f <= maxInt && maxInt <= 8)
            }
/*                &&
                windowLowRanges.Last().Length > 0 && // セグメント後、thresholdを超えていない
                frameIdx - (_range.First + _range.Length) >= 8)
            {
                if (range.Length > 0)
                {
                    int r_start = range.First;
                    int fIdx = frames.Count - 2;
                    for (int i = lastActivateLowWindow - 1; i >= 0; i--)
                    {
                        if (windowLowRanges[i].First + windowLowRanges[i].Length >= r_start) continue;
                        var t_range = 0 <= fIdx && fIdx < frames.Count ? frames[fIdx--] : new CharacterRange();
                        if (t_range.Length > 0)
                        {
                            var end = t_range.First + t_range.Length;
                            if (r_start - end <= 8)
                            {
                                r_start = t_range.First;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    int width = range.First + range.Length - r_start;
                    if (4 <= width && width <= 50)
                    {
                        int start = r_start;
                        int end = range.First + range.Length;
                        double maxInt = window.Where((_, i) => start <= i && i < end).Max();
                        if (range.First <= 160 && 160 <= range.First + range.Length)// && 0.1f <= maxInt && maxInt <= 8)
                        {
                            range = new CharacterRange(start, width);
                            lastActivateFrame = range.First + range.Length;
                            lastActivateRange = _range;
                            return true;
                        }
                    }
                }
            }*/
//            range = GetFrame(windowLowRanges.Count - 1);
  //          if (range.Length >= 1)
    //        {
      //          return true;
        //    }
            return false;
        }

        public CharacterRange GetFrame(int idx)
        {
            if (idx >= 0 && windowLowRanges.Count >= 1 && windowHighRanges.Count >= 1 && windowLowRanges[idx].Length >= 2)
            {
                int gestureFinishFrame = windowLowRanges[idx].Length + windowLowRanges[idx].First;
                int gestureMiddleFrame0 = -1;
                int gestureMiddleFrame1 = -1;
                for (int i = windowHighRanges.Count - 1; i >= 0; i--)
                {
                    if (windowHighRanges[i].First + windowHighRanges[i].Length <= gestureFinishFrame)
                    {
                        gestureMiddleFrame0 = windowHighRanges[i].First;
                        gestureMiddleFrame1 = windowHighRanges[i].First + windowHighRanges[i].Length;
                        break;
                    }
                }
                int gestureStartFrame = -1;
                System.Diagnostics.Debug.Assert(gestureMiddleFrame1 <= gestureFinishFrame);
                for (int i = windowLowRanges.Count - 1; i >= 0; i--)
                {
                    if (windowLowRanges[i].First <= gestureMiddleFrame0)
                    {
                        gestureStartFrame = windowLowRanges[i].First;
                        break;
                    }
                }
                if (gestureStartFrame >= lastGestureFinishFrame)
                {
                    var range = new CharacterRange(gestureStartFrame, gestureFinishFrame - gestureStartFrame);
                    lastGestureFinishFrame = gestureFinishFrame;

                    
                    
                    //
                    range.First -= intensitySmooth;

                    lastActivateLowWindow = idx;



                    return range;
                }
            }
            return new CharacterRange();
        }

        public List<CharacterRange> GetAllFrames()
        {
            List<CharacterRange> ranges = new List<CharacterRange>();
            lastGestureFinishFrame = 0;
            lastActivateLowWindow = -1;
            for (int i = 0; i < windowLowRanges.Count; i++)
            {
                var frame = GetFrame(i);
                if (frame.Length >= 1)
                {
                    ranges.Add(frame);
                }
            }
            return ranges;
        }

        // TODO
        public static svm_node[] GetFeatures(List<double>[] raw_waves,  List<int> sensorTopin = null)
        {
            if (raw_waves != null && raw_waves.Max(wv => wv.Count) >= 5)
            {
                double time = raw_waves.First().Count;
                List<double> fftList = new List<double>();

                List<List<double>> pushForceList = new List<List<double>>();
                List<List<CharacterRange>> pushRangesList = new List<List<CharacterRange>>();

                double norm_width = 0.05f;
                for (int i = 0; i < raw_waves.Length; i++)
                {
                    double _max = raw_waves[i].Max();
                    double _min = raw_waves[i].Min();
                    norm_width = Math.Max(_max - _min, norm_width);
                }

                for (int i = 0; i < raw_waves.Length; i++)
                {
                    if (use("pin " + i))
                    {
                        List<double> pushForce = new List<double>();
                        List<CharacterRange> pushRanges = new List<CharacterRange>();

                        float peekNum = 0;
                        int updateDir = 0;
                        double peek = raw_waves[i][0];
                        int prevFrame = 0;
                        double _max = raw_waves[i].Max();
                        double _min = raw_waves[i].Min();
                        double peekThreshold = Math.Max(0.05f, 0.5f * (_max - _min));
                        for (int j = 1; j < raw_waves[i].Count; j++)
                        {
                            if ((updateDir > 0 && peek < raw_waves[i][j]) || (updateDir < 0 && peek > raw_waves[i][j]))
                            {
                                peek = raw_waves[i][j];
                            }
                            if ((updateDir == 0 && Math.Abs(raw_waves[i][j] - peek) >= peekThreshold) ||
                                (updateDir < 0 && raw_waves[i][j] - peek >= peekThreshold) ||
                                (updateDir > 0 && peek - raw_waves[i][j] >= peekThreshold))
                            {
                                if (updateDir == 0 && peek > raw_waves[i][j])
                                {
                                    peekNum = 0.5f;
                                }
                                else
                                {
                                    peekNum++;
                                }

                                if (raw_waves[i][j] > peek)
                                {
                                    pushForce.Add(raw_waves[i][j] - peek);
                                }


                                updateDir = peek < raw_waves[i][j] ? 1 : -1;
                                peek = raw_waves[i][j];

                                pushRanges.Add(new CharacterRange(Math.Max(0, j - 3), 3));
                                prevFrame = j;
                            }
                        }

                        pushForceList.Add(pushForce);
                        pushRangesList.Add(pushRanges);
                    }
                }

                List<List<svm_node>> vxList = new List<List<svm_node>>();

                while (true)
                {
                    int fIdx = 1;
                    List<svm_node> vx = new List<svm_node>();

                    List<int>[] overlap = new List<int>[raw_waves[0].Count];
                    for (int j = 0; j < overlap.Length; j++)
                    {
                        overlap[j] = new List<int>();
                    }
                    for (int s = 0; s < sensorTopin.Count; s++)
                    {
                        var ranges = pushRangesList[s];
                        foreach (var range in ranges)
                        {
                            for (int j = range.First; j < range.First + range.Length; j++)
                            {
                                if (overlap[j] == null) overlap[j] = new List<int>();
                                overlap[j].Add(s);
                            }
                        }
                    }

                    int max = overlap.Max(ls => ls.Count);
                    if (max >= 1)
                    {
                        var item = overlap.First(ls => ls.Count == max);
                        int idx = Array.IndexOf(overlap, item);
                        for (int s = 0; s < raw_waves.Length; s++)
                        {
                            if (
                                true || 
                                (new int[] { 
                        1, //4,
                        2, //3,
                        6, //7,
                        9, //12,
                        10, //15,
                        13, // 14,
                        }).Contains(s))
                            {
                                //                            fIdx = AddFeature(vx, fIdx, item.Contains(s) ? pushForceList[s].Count * 0.1f : 0, true);
                                fIdx = AddFeature(vx, fIdx, item.Contains(s) ? pushForceList[s].Count <= 0 ? 0 : pushForceList[s].Max() / norm_width : 0, true);
                            }
                            pushForceList[s].Clear();
                            pushRangesList[s].Clear();
                        }
                    }

                    if (vx.Count <= 0) break;

                    double minLeft = 1;
                    double minRight = 1;
                    for (int j = 0; j < raw_waves[0].Count; j++)
                    {
                        minLeft = Math.Min(raw_waves.Take(8).Average(ls => ls[j]), minLeft);
                        minRight = Math.Min(raw_waves.Skip(8).Average(ls => ls[j]), minRight);
                    }
                    fIdx = AddFeature(vx, fIdx, minLeft, true);// <= 0.1 ? 1 : 0, true);
                    fIdx = AddFeature(vx, fIdx, minRight, true);// <= 0.1 ? 1 : 0, true);

                    vxList.Add(vx);
                }

                if (vxList.Count <= 0)
                {
                    var t_vx = new List<svm_node>();
                    int fIdx = 1;
                    for (int s = 0; s < raw_waves.Length; s++)
                    {
                        fIdx = AddFeature(t_vx, fIdx, 0, true);
                        fIdx = AddFeature(t_vx, fIdx, 0, true);
                    }
                    fIdx = AddFeature(t_vx, fIdx, 0, true);
                    fIdx = AddFeature(t_vx, fIdx, 0, true);
                    return t_vx.ToArray();
                }

                return vxList[(vx_idx++) % vxList.Count].ToArray();
            }
            return null;
        }

        static int vx_idx = 0;

        static int AddFeature(List<svm_node> vx, int idx, double value, bool canScaling)
        {
            vx.Add(new svm_node() {
                index = idx,
                value = value,
            });
            scaleMask.Add(canScaling);
            return idx + 1;
        }

        public void UpdateFeatureExtractor(Dictionary<string, bool> filter)
        {
            featuresFilter = filter;
        }
    }
}
