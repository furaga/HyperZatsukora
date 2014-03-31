using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using libsvm;
using ILNumerics;
using System.Drawing;
namespace FLib
{
    public class SockswitchSVM
    {
        public C_SVC svm = null;
        List<double> scaleMax = new List<double>();
        List<double> scaleMin = new List<double>();
        static List<bool> scaleMask = new List<bool>();
        int[] baseline_cnts;
        Queue<double>[] baseline_totals;
        double[] baseline_prevs;
        public List<double[]> baselines = new List<double[]>();
        Queue<double>[] window_dataHistory;
        public List<double> window = new List<double>();
        public List<CharacterRange> windowHighRanges = new List<CharacterRange>();
        public List<CharacterRange> windowLowRanges = new List<CharacterRange>();
        CharacterRange prevRange;
        static Dictionary<string, bool> featuresFilter = new Dictionary<string, bool>();

       static bool use(string featureName) { return featuresFilter.ContainsKey(featureName) && featuresFilter[featureName]; }

        public SockswitchSVM()
        {
        }

        public void Reset()
        {
            baseline_cnts = null;
            baseline_totals = null;
            baseline_prevs = null;
            baselines = new List<double[]>();
            window_dataHistory = null;
            window = new List<double>();
            windowLowRanges = new List<CharacterRange>();
            windowHighRanges = new List<CharacterRange>();
            prevRange = new CharacterRange();
        }

        public void Load(string trainFile, double C, double gamma)
        {
            var raw_prob = ProblemHelper.ReadProblem(trainFile);
            var prob = ProblemHelper.ScaleProblem(raw_prob);
            svm = new C_SVC(prob, KernelHelper.RadialBasisFunctionKernel(gamma), C);

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
        }

        public svm_node[] Scaling(svm_node[] x)
        {
            System.Diagnostics.Debug.Assert(scaleMin.Count == scaleMax.Count);
            System.Diagnostics.Debug.Assert(scaleMax.Count == x.Length);

            const double lower = -1;
            const double upper = 1;

            var sx = new svm_node[x.Length];
            for (int i = 0; i < sx.Length; i++)
            {
                double max = scaleMax[x[i].index - 1];
                double min = scaleMin[x[i].index - 1];
                double val = scaleMask[x[i].index - 1] ? lower + (upper - lower) * (x[i].value - min) / (max - min) : x[i].value;
                sx[i] = new svm_node()
                {
                    index = x[i].index,
                    value = val,
                };
            }
            return sx;
        }

        public double[] Baselines(List<double>[] raw_waves, int frameIndex, int smooth, double threshold)
        {
            double[] baseline = new double[raw_waves.Length];
            if (baseline_cnts == null)
            {
                baseline_cnts = new int[raw_waves.Length];
                baseline_totals = new Queue<double>[raw_waves.Length];
                for (int i = 0; i < baseline_totals.Length; i++) baseline_totals[i] = new Queue<double>();
                baseline_prevs = new double[raw_waves.Length];
            }
            for (int sIdx = 0; sIdx < raw_waves.Length; sIdx++)
            {
                double val = raw_waves[sIdx][frameIndex];
                double diff = Math.Abs(baseline_prevs[sIdx] - val);
                if (diff >= threshold)
                {
                    // 圧力が変化したら
                    baseline_cnts[sIdx] = smooth;
                    baseline_totals[sIdx].Clear();
                }
                else
                {
                    baseline_cnts[sIdx]--;
                    baseline_totals[sIdx].Enqueue(val);
                    while (baseline_totals[sIdx].Count > smooth) baseline_totals[sIdx].Dequeue();
                }
                // 圧力が一定時間変化しなかったらベースラインを更新
                if (baseline_cnts[sIdx] <= 0)
                {
                    baseline[sIdx] = baseline_totals[sIdx].Average();
                }
                baseline_prevs[sIdx] = val;
            }

            baselines.Add(baseline);

            return baseline;
        }
        public double Window(List<double>[] raw_waves, List<double[]> baselines, int frameIndex, int smooth, double powK, double highThreshold = 0.5, double lowThreshold = 0.05f)
        {
            if (window_dataHistory == null)
            {
                window_dataHistory = new Queue<double>[raw_waves.Length];
                for (int i = 0; i < window_dataHistory.Length; i++) window_dataHistory[i] = new Queue<double>();
            }
            for (int i = 0; i < raw_waves.Length; i++)
            {
                window_dataHistory[i].Enqueue(raw_waves[i][frameIndex] - baselines[frameIndex][i]);
                while (window_dataHistory[i].Count > smooth) window_dataHistory[i].Dequeue();
            }
            double val = 0;
            //            for (int i = 0; i < window_dataHistory.Length; i++)
            for (int i = 8; i < window_dataHistory.Length; i++)
            {
                double average = Math.Abs(window_dataHistory[i].Average());
                val += Math.Exp(powK * average) - 1;
            }
            val /= window_dataHistory.Length;
            window.Add(val);

            if (window.Count >= 2)
            {
                int idx = window.Count - 1;
                if (window[idx - 1] < highThreshold && highThreshold <= window[idx])
                {
                    windowHighRanges.Add(new CharacterRange(idx, 0));
                }
                if (window[idx - 1] >= highThreshold && highThreshold > window[idx] && windowHighRanges.Count >= 1)
                {
                    var range = windowHighRanges.Last();
                    range.Length = idx - range.First;
                    windowHighRanges[windowHighRanges.Count - 1] = range;
                }
                if (window[idx - 1] < lowThreshold && lowThreshold <= window[idx])
                {
                    windowLowRanges.Add(new CharacterRange(idx, 0));
                }
                if (window[idx - 1] >= lowThreshold && lowThreshold > window[idx] && windowLowRanges.Count >= 1)
                {
                    var range = windowLowRanges.Last();
                    range.Length = idx - range.First;
                    windowLowRanges[windowLowRanges.Count - 1] = range;
                }
            }
            return val;
        }

        int lastGestureFinishFrame = 0;

        public bool CanRecognize(int margin, out CharacterRange range)
        {
            range = GetFrame(windowLowRanges.Count - 1);
            if (range.Length >= 1)
            {
                return true;
            }
            return false;
        }

        public CharacterRange GetFrame(int idx)
        {
            if (windowLowRanges.Count >= 1 && windowHighRanges.Count >= 1 && windowLowRanges[idx].Length >= 2)
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
                    return range;
                }
            }
            return new CharacterRange();
        }

        public List<CharacterRange> GetAllFrames()
        {
            List<CharacterRange> ranges = new List<CharacterRange>();
            lastGestureFinishFrame = 0;
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
        public static svm_node[] GetFeatures(List<double>[] raw_waves)
        {
            if (raw_waves != null && raw_waves.All(w => w.Count > 20))
            {
                int fIdx = 1;
                List<svm_node> vx = new List<svm_node>();
                scaleMask.Clear();
                double time = raw_waves.First().Count;
                for (int i = 0; i < raw_waves.Length; i++)
                {
                    if (use("pin " + i))
                    {
                        ILArray<double> wave = ILNumerics.ILMath.array<double>(raw_waves[i].ToArray());
                        // 波形の長さ・最大値・最小値・平均（・標準偏差）
                        double max = wave.Max();
                        double min = wave.Min();
                        double mean = wave.Average();
                        //                    double std = ILNumerics
/*                        if (use("max"))
                        {
                            fIdx = AddFeature(vx, fIdx, max - min, false);
                        }
                        if (use("mean"))
                        {
                            fIdx = AddFeature(vx, fIdx, mean - min, false);
                        }
                        //
                        if (use("ranking"))
                        {
                            double[] average = new double[4];
                            for (int j = 0; j < average.Length; j++)
                            {
                                int start = raw_waves[i].Count / 4 * j;
                                int end = Math.Min(raw_waves[i].Count, raw_waves[i].Count / 4 * (j + 1));
                                average[j] = 0;
                                for (int k = start; k < end; k++)
                                {
                                    average[j] += raw_waves[i][k];
                                }
                                average[j] /= (end - start);
                            }
                            for (int j = 0; j < average.Length; j++)
                            {
                                double rank = 0;
                                for (int k = 0; k < average.Length; k++)
                                {
                                    if (average[j] > average[k])
                                        rank++;
                                }
                                rank /= average.Length * 4;
                                fIdx = AddFeature(vx, fIdx, rank, false);
                            }
                        }
  */                      // FFTの波形
                        if (use("fft"))
                        {
                            ILArray<complex> spec = ILMath.fft(wave);
                            for (int j = 0; j < 20 / 2; j++)
                            {
                                var z = spec.ElementAt(j);
                                fIdx = AddFeature(vx, fIdx, z.Abs(), true);
                            }
                        }
                    }
                }
                return vx.ToArray();
            }
            return null;
        }

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
