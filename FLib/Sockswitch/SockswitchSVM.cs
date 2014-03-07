using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using libsvm;
using ILNumerics;

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

        public static List<double[]> Baselines(List<double>[] raw_waves, int smooth, double threshold)
        {
            List<double[]> baselines = new List<double[]>();
            int[] cnts = new int[raw_waves.Length];
            double[] prevs = new double[raw_waves.Length];
            double[] total = new double[raw_waves.Length];
            for (int i = 0; i < cnts.Length; i++)
            {
                cnts[i] = smooth;
            }
            for (int fIdx = 0; fIdx < raw_waves.First().Count; fIdx++)
            {
                baselines.Add(new double[raw_waves.Length]);
                for (int sIdx = 0; sIdx < raw_waves.Length; sIdx++)
                {
                    double val = raw_waves[sIdx][fIdx];
                    double diff = Math.Abs(prevs[sIdx] - val);
                    // 圧力が変化したら
                    if (diff >= threshold)
                    {
                        cnts[sIdx] = smooth;
                        total[sIdx] = 0;
                    }
                    else
                    {
                        cnts[sIdx]--;
                        total[sIdx] += val;
                    }
                    // 圧力が一定時間変化しなかったらベースラインを更新
                    if (cnts[sIdx] <= 0)
                    {
                        double t = 0;
                        for (int i = 0; i < smooth; i++)
                        {
                            t += raw_waves[sIdx][fIdx + i - smooth];
                        }
                        baselines[fIdx][sIdx] = t / smooth;// total[sIdx] / smooth;
                    }
                    else if (fIdx >= 1)
                    {
                        baselines[fIdx][sIdx] = baselines[fIdx - 1][sIdx];
                    }
                    prevs[sIdx] = val;
                }
            }
            return baselines;
        }

        public static List<double> Window(List<double>[] normalized_waves, int smooth, double powK)
        {
            List<double> window_waves = new List<double>();
            int rem = normalized_waves.First().Count % smooth;
            int cnt = normalized_waves.First().Count / smooth;
            for (int offset = smooth; offset < normalized_waves.First().Count; offset++)
            {
                window_waves.Add(0);
                for (int sIdx = 0; sIdx < normalized_waves.Length; sIdx++)
                {
                    double total = 0;
                    for (int k = 0; k < smooth; k++)
                    {
                        total += normalized_waves[sIdx][offset - k];
                    }
                    double average = Math.Abs(total / smooth);
                    double pow = Math.Exp(powK * average) - 1;
                    window_waves[window_waves.Count - 1] += pow;
                }
                window_waves[window_waves.Count - 1] /= normalized_waves.Length;
            }
            return window_waves;
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
