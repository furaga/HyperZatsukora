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

        public SockswitchSVM()
        {

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
                double val = lower + (upper - lower) * (x[i].value - min) / (max - min);
                sx[i] = new svm_node()
                {
                    index = x[i].index,
                    value = val,
                };
            }
            return sx;
        }

        int[] baseline_cnts;
        Queue<double>[] baseline_totals;
        double[] baseline_prevs;
        public List<double[]> baselines = new List<double[]>();

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

        Queue<double>[] window_totals;
        public List<double> window = new List<double>();
        public List<CharacterRange> windowRanges = new List<CharacterRange>();

        public double Window(List<double>[] raw_waves, List<double[]> baselines, int frameIndex, int smooth, double powK, double threshold = 0.3)
        {
            if (window_totals == null)
            {
                window_totals = new Queue<double>[raw_waves.Length];
                for (int i = 0; i < window_totals.Length; i++) window_totals[i] = new Queue<double>();
            }
            for (int i = 0; i < raw_waves.Length; i++)
            {
                window_totals[i].Enqueue(raw_waves[i][frameIndex] - baselines[frameIndex][i]);
                while (window_totals[i].Count > smooth) window_totals[i].Dequeue();
            }
            double val = 0;
            for (int i = 0; i < window_totals.Length; i++)
            {
                double average = Math.Abs(window_totals[i].Average());
                val += Math.Exp(powK * average) - 1;
            }
            val /= window_totals.Length;
            window.Add(val);

            if (window.Count >= 2)
            {
                int idx = window.Count - 1;
                if (window[idx - 1] < threshold && threshold <= window[idx])
                {
                    windowRanges.Add(new CharacterRange( idx, 1));
                }
                if (window[idx - 1] >= threshold && threshold > window[idx] && windowRanges.Count >= 1)
                {
                    var range = windowRanges.Last();
                    range.Length = idx - range.First;
                    windowRanges[windowRanges.Count - 1] = range;
                }
            }
            return val;
        }

        CharacterRange prevRange;
        public bool CanRecognize(int margin, out CharacterRange range)
        {
            range = new CharacterRange(0, 0);
            if (windowRanges.Count <= 1)
            {
                return false;
            }
            int start = (windowRanges.Last().First + windowRanges.Last().Length);
            int end = window.Count - 1;
            if (windowRanges.Last() != prevRange && end - start >= margin)
            {
                start = windowRanges.Last().First - margin;
                for (int i = windowRanges.Count - 2; i >= 0; i--)
                {
                    int s= windowRanges[i].First - margin;
                    int e = windowRanges[i].First + windowRanges[i].Length + margin;
                    if (start <= e) start = s;
                }
                range.First = start;
                range.Length = end - start;
                prevRange = windowRanges.Last();
                return true;
            }
            return false;
        }

        // TODO
        public static svm_node[] GetFeatures(List<double>[] raw_waves)
        {
            if (raw_waves.All(w => w.Count > 20))
            {
                int fIdx = 1;
                List<svm_node> vx = new List<svm_node>();
                double time = raw_waves.First().Count;
                fIdx = AddFeature(vx, fIdx, time);
                for (int i = 0; i < raw_waves.Length; i++)
                {
                    if (true || 
                        i % 8 == 2 ||
                        i % 8 == 0 ||
                        i % 8 == 5 ||
                        i % 8 == 6)
                    {
                        ILArray<double> wave = ILNumerics.ILMath.array<double>(raw_waves[i].ToArray());

                        // 波形の長さ・最大値・最小値・平均（・標準偏差）
                        double max = wave.Max();
                        double min = wave.Min();
                        double mean = wave.Average();
                        //                    double std = ILNumerics
                        fIdx = AddFeature(vx, fIdx, max);
                        fIdx = AddFeature(vx, fIdx, min);
                        fIdx = AddFeature(vx, fIdx, mean);

                        //

                        //

                        //

                        // FFTの波形
                        ILArray<complex> spec = ILMath.fft(wave);
                        for (int j = 0; j < 20 / 2; j++)
                        {
                            var z = spec.ElementAt(j);
                            fIdx = AddFeature(vx, fIdx, z.Abs());
                        }
                    }
                }
                return vx.ToArray();
            }
            return null;
        }

        static int AddFeature(List<svm_node> vx, int idx, double value)
        {
            vx.Add(new svm_node() {
                index = idx,
                value = value,
            });
            return idx + 1;
        }
    }
}
