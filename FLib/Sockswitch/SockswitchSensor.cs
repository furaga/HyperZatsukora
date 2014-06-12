using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.Xml.Serialization;

namespace FLib
{
    public partial class SockswitchSensor : IDisposable
    {
        const float ratio = 1 / 1023f;

        public int sensorNum = 0;
        public int sensorNumPerFoot = 0;

        SerialPort serialPort;
        List<byte> rawData = new List<byte>();
        List<float> raw_pressureData = new List<float>();
        List<float> pressureData = new List<float>();
        public List<bool> isNewPressureData = new List<bool>();
        List<long> timeStamps = new List<long>(); // 各フレームの取得時間
        List<int> rPinTovPin = new List<int>(); // 現実のピンと仮想ピンの対応関係
        int[] tmpLowData;
        int[] tmpSensorIdx = new int[2] { 0, 0 };

        public List<Tuple<string, System.Drawing.CharacterRange>> anottationGestureNameList = new List<Tuple<string, System.Drawing.CharacterRange>>();

        bool dummySerialMode = true;
        int dummyCurrentPos = 0;
        Stopwatch dummyStopwatch = new Stopwatch();

        bool SerialIsOpen { get { if (dummySerialMode) return true; else return serialPort.IsOpen; } }
        int SerialBytesToRead { get {
            if (dummySerialMode)
            {
                if (t_timeStamps != null)
                {
                    for (int i = 0; i < t_timeStamps.Count; i++)
                    {
                        if (t_timeStamps[i] > dummyStopwatch.ElapsedMilliseconds)
                        {
                            return (i - FrameCount + 1) * sensorNum;
                        }
                    }
//                    (3 * sensorNum + 1);
                }
                return 0;
            }
            else return serialPort.BytesToRead;
        }
        }
        System.Windows.Forms.Timer serialTimer;
        Stopwatch serialStopwatch = Stopwatch.StartNew();

        public int FrameCount { get { return sensorNum == 0 ? 0 : pressureData.Count / sensorNum; } }
        public bool IsOpen { get { return serialPort == null ? false : serialPort.IsOpen; } }
        public bool IsUpdate { get; set; }
        public Action OnUpdate { set; get; }

        public void Dispose()
        {
            serialPort.Close();
        }

        public void Clear()
        {
            rawData.Clear();
            raw_pressureData.Clear();
            pressureData.Clear();
            isNewPressureData.Clear();
            timeStamps.Clear();
            anottationGestureNameList.Clear();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            serialPort_DataReceived(null, null);
        }

        #region  圧力データの保存・ロード
        public void Save(string filepath)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("sensorNumPerFoot:\n");
                sb.Append(sensorNumPerFoot + "\n");
                sb.Append("rPinTovPin:\n");
                sb.Append(string.Join(" ", rPinTovPin));
                sb.Append("\n");
                sb.Append("rawData:\n");
                sb.Append(string.Join(" ", rawData));
                sb.Append("\n");
                sb.Append("pressureData:\n");
                sb.Append(string.Join(" ", pressureData));
                sb.Append("\n");
                sb.Append("timeStamps:\n");
                sb.Append(string.Join(" ", timeStamps));
                sb.Append("\n");
                sb.Append("actualGestureName:\n");
                sb.Append(string.Join(" ", anottationGestureNameList.Select(tpl => tpl.Item1 + "," + tpl.Item2.First + "," + tpl.Item2.Length).ToArray()));
                sb.Append("\n");
                sb.Append("end:\n");
                System.IO.File.WriteAllText(filepath, sb.ToString());
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString() + ":" + e.StackTrace);
            }
        }

        public void Load(string filepath)
        {
            try
            {
                System.IO.StringReader sr = new System.IO.StringReader(System.IO.File.ReadAllText(filepath));
                Clear();
                while (true)
                {
                    string line = sr.ReadLine();
                    switch (line.Trim().Trim('\n', '\r', ':'))
                    {
                        case "sensorNumPerFoot":
                            line = sr.ReadLine().Trim();
                            sensorNumPerFoot = int.Parse(line);
                            sensorNum = sensorNumPerFoot * 2;
                            break;
                        case "rawData":
                            line = sr.ReadLine().Trim();
                            var rawData = line.Split(' ').Select(s => byte.Parse(s)).ToArray();
                            UpdatePressureData(rawData);
                            break;
                        case "pressureData":
                            line = sr.ReadLine().Trim();
                            //                            pressureData = line.Split(' ').Select(s => float.Parse(s)).ToList();
                            break;
                        case "timeStamps":
                            line = sr.ReadLine().Trim();
                            timeStamps = line.Split(' ').Select(s => long.Parse(s)).ToList();
                            break;
                        case "rPinTovPin":
                            line = sr.ReadLine().Trim();
                            rPinTovPin = line.Split(' ').Select(s => int.Parse(s)).ToList();
                            break;
                        case "actualGestureName":
                            line = sr.ReadLine().Trim();
                            if (line.Length <= 0) break;
                            anottationGestureNameList = line.Split(' ').Select(s =>
                                {
                                    var tokens = s.Split(',');
                                    string gestureName = tokens[0];
                                    int first = int.Parse(tokens[1]);
                                    int length = int.Parse(tokens[2]);
                                    return new Tuple<string, System.Drawing.CharacterRange>(gestureName, new System.Drawing.CharacterRange(first, length));
                                }).ToList();
                            break;
                        case "end":
                            if (pressureData.Count >= sensorNum)
                            {
                                float[] prevs = pressureData.Take(sensorNum).ToArray();
                                for (int i = sensorNum; i < pressureData.Count; i++)
                                {
                                    if (pressureData[i] < 0) pressureData[i] = prevs[i % sensorNum];
                                    else prevs[i % sensorNum] = pressureData[i];
                                }
                            }
                            OnUpdate();
                            return;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString() + ":" + e.StackTrace);
            }
        }
        #endregion

        #region  初期化
        public SockswitchSensor(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits, int sensorNumPerFoot, Action OnUpdate, float[] min, float[] max)
        {
            try
            {
                // ポート名がからじゃなければそのポートを開く
                // さもなくば、ダミーの圧力データをリアルタイムで生成する
                if (portName != "")
                {
                    serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
                    serialPort.Handshake = Handshake.None;
                    serialPort.Encoding = Encoding.Unicode;
                    serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);
                    //! シリアルポートをオープンする.
                    serialPort.Open();
                    serialPort.ReadExisting();
                    dummySerialMode = false;
                }
                else
                {
                    serialTimer = new System.Windows.Forms.Timer()
                    {
                        Enabled = true,
                        Interval = 30,
                    };
                    serialTimer.Tick += new EventHandler(timer_Tick);
                    dummySerialMode = true;
                }
                this.sensorNumPerFoot = sensorNumPerFoot;
                this.sensorNum = 2 * sensorNumPerFoot;
                tmpLowData = new int[sensorNum];
                for (int i = 0; i < tmpLowData.Length; i++) tmpLowData[i] = -1;
                for (int i = 0; i < sensorNum; i++) rPinTovPin.Add(i);
                this.OnUpdate = OnUpdate;

                // キャリブレーション
                this.max = max;
                this.min = min;
                UpdateCalibrationData(0, 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ":" + ex.StackTrace);
            }
        }

        float[] pressureWidthPerFoot = new[] { 0.1f, 0.1f };
        public float[] max;
        public float[] min;
        #endregion

        #region 生のシリアルデータから圧力データを復元・各フレームにおける圧力データの取得
        void UpdatePressureData(byte[] data, bool normalize = true)
        {
            rawData.AddRange(data);

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == 0xff)
                {
                    timeStamps.Add(serialStopwatch.ElapsedMilliseconds);
                }
                else
                {
                    int type = (data[i] >> 6) & 0x3;
                    int side = (data[i] >> 5) & 0x1;
                    int sensorIdx = 0;
                    int highBits, lowBits;
                    switch (type)
                    {
                        case 0:     // センサのID
                            sensorIdx = (data[i] & 0x7) + side * sensorNumPerFoot;
                            tmpSensorIdx[side] = sensorIdx;
                            break;
                        case 1:     // 下位ビット
                            sensorIdx = tmpSensorIdx[side];
                            lowBits = data[i] & 0x1f;
                            tmpLowData[sensorIdx] = lowBits;
                            break;
                        case 2:     // 上位ビット
                            sensorIdx = tmpSensorIdx[side];
                            highBits = data[i] & 0x1f;
                            lowBits = tmpLowData[sensorIdx];
                            float pressure = ((highBits << 5) | lowBits) * ratio;
                            int targetIdx = FrameCount * sensorNum + sensorIdx;
                            while (pressureData.Count > targetIdx) targetIdx += sensorNum;
                            while (pressureData.Count < targetIdx)
                            {
                                raw_pressureData.Add(raw_pressureData.Count >= sensorNum ? raw_pressureData[raw_pressureData.Count - sensorNum] : 0);
                                pressureData.Add(pressureData.Count >= sensorNum ? pressureData[pressureData.Count - sensorNum] : 0);
                                isNewPressureData.Add(false);
                                Debug.Assert(pressureData.Count == isNewPressureData.Count);
                            }
                            if (pressure >= 0)
                            {
                                raw_pressureData.Add(pressure);
                                if (normalize)
                                {
                                    int idx = targetIdx % sensorNum;
                                    float nv = (pressure - min[idx]) / pressureWidthPerFoot[side];
                                    pressureData.Add(nv);//FMath.Clamp(nv, 0, 1));
                                    isNewPressureData.Add(true);
                                    Debug.Assert(pressureData.Count == isNewPressureData.Count);
                                }
                                else
                                {
                                    pressureData.Add(pressure);
                                    isNewPressureData.Add(true);
                                    Debug.Assert(pressureData.Count == isNewPressureData.Count);
                                }
                            }
                            else
                            {
                                raw_pressureData.Add(raw_pressureData.Count >= sensorNum ? raw_pressureData[raw_pressureData.Count - sensorNum] : 0);
                                pressureData.Add(pressureData.Count >= sensorNum ? pressureData[pressureData.Count - sensorNum] : 0);
                                isNewPressureData.Add(false);
                                Debug.Assert(pressureData.Count == isNewPressureData.Count);
                            }
                            break;
                    }
                }
            }
        }
        public float[] GetPressureData(int frameIdx, out long timeStamp)
        {
            float[] data = new float[sensorNum];
            for (int i = 0; i < data.Length; i++)
            {
                int idx = sensorNum * frameIdx + i;
                if (0 <= idx && idx < pressureData.Count)
                {
                    data[i] = pressureData[idx];
                }
                else
                {
                    data = null;
                    break;
                }
            }
            timeStamp = (0 <= frameIdx && frameIdx < timeStamps.Count) ? timeStamps[frameIdx] : 0;
            return data;
        }
        public List<double>[] GetPressureDataRange(int start, int end)
        {
            if (start < 0 || end < 0 || start > end) return null;

            long timeStamp;
            List<double>[] raw_waves = new List<double>[sensorNum];
            for (int j = 0; j < raw_waves.Length; j++)
            {
                raw_waves[j] = new List<double>();
            }
            for (int i = start; i <= end; i++)
            {
                var data = GetPressureData(i, out timeStamp);
                for (int j = 0; j < raw_waves.Length; j++)
                {
                    raw_waves[j].Add(data[j]);
                }
            }
            return raw_waves;
        }

        #endregion

        #region シリアル通信
        int t_sensorNumPerFoot;
        int t_sensorNum;
        List<byte> t_rawData;
        List<long> t_timeStamps;
        List<float> t_pressureData;
        int t_sentCnt = 0;

        void SerialRead(byte[] data, int offset, int count)
        {
            // ダミーモード
            if (dummySerialMode)
            {
                try
                {
                    if (t_rawData == null)
                    {
                        //                        System.IO.StringReader sr = new System.IO.StringReader(System.IO.File.ReadAllText("../../../../../pressureData/201403032222_scrolldown.txt"));
//                        System.IO.StringReader sr = new System.IO.StringReader(System.IO.File.ReadAllText("../../../../../pressureData/furaga/data.txt"));
                        System.IO.StringReader sr = new System.IO.StringReader(System.IO.File.ReadAllText("../../../../../pressureData/20140416_walking_video.txt"));
                        dummyCurrentPos = 0;
                        dummyStopwatch = Stopwatch.StartNew();

                        while (true)
                        {
                            string line = sr.ReadLine();
                            switch (line.Trim().Trim('\n', '\r', ':'))
                            {
                                case "sensorNumPerFoot":
                                    line = sr.ReadLine().Trim();
                                    t_sensorNumPerFoot = int.Parse(line);
                                    t_sensorNum = sensorNumPerFoot * 2;
                                    break;
                                case "rawData":
                                    line = sr.ReadLine().Trim();
                                    t_rawData = line.Split(' ').Select(s => byte.Parse(s)).ToList();
                                    break;
                                case "timeStamps":
                                    line = sr.ReadLine().Trim();
                                    t_timeStamps = line.Split(' ').Select(s => long.Parse(s)).ToList();
                                    break;
                                case "rPinTovPin":
                                    line = sr.ReadLine().Trim();
                                    break;
                                case "pressureData":
                                    line = sr.ReadLine().Trim();
                                    t_pressureData = line.Split(' ').Select(s => float.Parse(s)).ToList();
                                    break;
                                case "end":
                                    return;
                            }
                        }
                    }
                    //                    for (int i = 0; i < max.Length; i++)
                    //                      max[i] = Math.Max(0.1f, t_pressureData.Where((_, idx) => idx % sensorNum == i).Max());
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString() + ":" + e.StackTrace);
                }


                if (t_rawData != null)
                {
                    for (int i = 0; i < count; i++)
                    {
                        data[i] = t_rawData[t_sentCnt];
                        t_sentCnt++;
                        if (t_sentCnt >= t_rawData.Count) t_sentCnt = 0;
                    }
                }
                else
                {
                    int sensorIdx = (int)((serialStopwatch.ElapsedMilliseconds / 500) % sensorNum);
                    int force = (int)serialStopwatch.ElapsedMilliseconds % 500;
                    int dataIdx = 0;
                    for (int i = 0; i < sensorNum; i++)
                    {
                        int side = (i < sensorNumPerFoot ? 0 : 1) << 5;
                        int val = (sensorIdx == i || sensorIdx - 1 == i) ? (int)(force * 0.002f * 1023) : 0;
                        int high = (val >> 5) & 0x1f;
                        int low = val & 0x1f;
                        byte info = (byte)((0 << 6) | side | (i % sensorNumPerFoot));
                        byte lowValue = (byte)((1 << 6) | side | low);
                        byte highValue = (byte)((2 << 6) | side | high);
                        data[dataIdx++] = info;
                        data[dataIdx++] = lowValue;
                        data[dataIdx++] = highValue;
                    }
                    data[dataIdx++] = (byte)0xff;
                }
            }
            else
            {
                serialPort.Read(data, 0, data.GetLength(0));
            }
        }

        // データを受信したらrawDataを更新
        void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (IsUpdate == false || SerialIsOpen == false) return;
            try
            {
                byte[] data = new byte[SerialBytesToRead];
                SerialRead(data, 0, SerialBytesToRead);
                UpdatePressureData(data);
                OnUpdate();
            }
            catch (Exception ex)
            {
//                MessageBox.Show(ex.Message + ":" + ex.StackTrace);
            }
        }
        #endregion

        public void UpdateCalibrationData(int start, int end)
        {
            if (max.All(x => x == 0.5f))
            {
                for (int i = 0; i < max.Length; i++)
                {
                    max[i] = 0;
                    min[i] = 1;
                }
            }

            start = (int)FMath.Clamp(start, 10, FrameCount - 10);
            end = (int)FMath.Clamp(end, 10, FrameCount - 10);
            if (start > end) FMath.Swap(ref start, ref end);

            // 更新
            for (int i = start * sensorNum; i < end * sensorNum; i++)
            {
                if (isNewPressureData[i])
                {
                    max[i % sensorNum] = Math.Max(max[i % sensorNum], raw_pressureData[i]);
                    min[i % sensorNum] = Math.Min(min[i % sensorNum], raw_pressureData[i]);
                }
            }

            for (int i = 0; i < max.Length; i++)
            {
                // 差が大きすぎたらバグってる
                if (max[i] - min[i] <= 0.6f)
                {
                    pressureWidthPerFoot[(i / sensorNumPerFoot) % 2] = Math.Max(pressureWidthPerFoot[(i / sensorNumPerFoot) % 2], max[i] - min[i]);
                }
            }
        }
    }
}