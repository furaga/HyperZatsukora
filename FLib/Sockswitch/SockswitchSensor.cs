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
        List<float> pressureData = new List<float>();
        List<long> timeStamps = new List<long>(); // 各フレームの取得時間
        List<int> rPinTovPin = new List<int>(); // 現実のピンと仮想ピンの対応関係
        int[] tmpLowData;
        int[] tmpSensorIdx = new int[2] { 0, 0 };

        bool dummySerialMode = true;
        bool SerialIsOpen { get { if (dummySerialMode) return true; else return serialPort.IsOpen; } }
        int SerialBytesToRead { get { if (dummySerialMode) return (3 * sensorNum + 1); else return serialPort.BytesToRead; } }
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
                            rawData = line.Split(' ').Select(s => byte.Parse(s)).ToList();
                            break;
                        case "pressureData":
                            line = sr.ReadLine().Trim();
                            pressureData = line.Split(' ').Select(s => float.Parse(s)).ToList();
                            break;
                        case "timeStamps":
                            line = sr.ReadLine().Trim();
                            timeStamps = line.Split(' ').Select(s => long.Parse(s)).ToList();
                            break;
                        case "rPinTovPin":
                            line = sr.ReadLine().Trim();
                            rPinTovPin = line.Split(' ').Select(s => int.Parse(s)).ToList();
                            break;
                        case "end":
                            if (pressureData.Count >= sensorNum)
                            {
                                float[] max = new float[sensorNum];
                                for (int i = 0; i < max.Length; i++)
                                    max[i] = Math.Max(0.1f, pressureData.Where((_, idx) => idx % sensorNum == i).Max());
                                for (int i = 0; i < pressureData.Count; i++)
                                {
                                    pressureData[i] /= max[i % sensorNum];
                                }
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
        public SockswitchSensor(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits, int sensorNumPerFoot, Action OnUpdate)
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
                        Interval = 100,
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
                max = new float[] {
                    0.1f,
                    0.1485826f,
                    0.3059629f,
                    0.1788856f,
                    0.1f,
                    0.1661779f,
                    0.2199413f,
                    0.228739f,
                    0.1681329f,
                    0.1446725f,
                    0.18896608f,
                    0.1339198f,
                    0.1339198f,
                    0.3499511f,
                    0.1505376f,
                    0.3450635f,
                }.Take(sensorNum).ToArray();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ":" + ex.StackTrace);
            }
        }

        float[] max;
        #endregion

        #region 生のシリアルデータから圧力データを復元・各フレームにおける圧力データの取得
        void UpdatePressureData(byte[] data)
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
                                pressureData.Add(pressureData.Count >= sensorNum ? pressureData[pressureData.Count - sensorNum] : 0);
                            }
                            if (pressure >= 0)
                                pressureData.Add(pressure / max[targetIdx % sensorNum]);
                            else
                                pressureData.Add(pressureData.Count >= 1 ? pressureData.Last() : 0);
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
                        System.IO.StringReader sr = new System.IO.StringReader(System.IO.File.ReadAllText("../../../../../pressureData/201403032221_heeltap.txt"));
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
                MessageBox.Show(ex.Message + ":" + ex.StackTrace);
            }
        }
        #endregion
    }
}