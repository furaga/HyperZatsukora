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

namespace FLib
{
    public partial class SockswitchSensor : IDisposable
    {
        const float ratio = 1 / 1023f;

        int sensorNum = 0;
        int sensorNumParFoot = 0;

        SerialPort serialPort;
        List<byte> rawData = new List<byte>();
        List<float> pressureData = new List<float>();
        List<long> timeStamps = new List<long>(); // 各フレームの取得時間
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

        #region  初期化
        public SockswitchSensor(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits, int sensorNumPerFoot)
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
                this.sensorNumParFoot = sensorNumPerFoot;
                this.sensorNum = 2 * sensorNumPerFoot;
                tmpLowData = new int[sensorNum];
                for (int i = 0; i < tmpLowData.Length; i++) tmpLowData[i] = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ":" + ex.StackTrace);
            }
        }
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
                    int sensorIdx;
                    int highBits, lowBits;
                    switch (type)
                    {
                        case 0:     // センサのID
                            sensorIdx = (data[i] & 0x7) + side * sensorNumParFoot;
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
                            while (pressureData.Count < targetIdx) pressureData.Add(-1);
                            pressureData.Add(pressure);
                            break;
                    }
                }
            }
        }
        public float[] GetPressureData(int frameIdx)
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
            return data;
        }
        #endregion

        #region シリアル通信
        void SerialRead(byte[] data, int offset, int count)
        {
            if (dummySerialMode)
            {
                int sensorIdx = (int)((serialStopwatch.ElapsedMilliseconds / 500) % sensorNum);
                int force = (int)serialStopwatch.ElapsedMilliseconds % 500;
                int dataIdx = 0;
                for (int i = 0; i < sensorNum; i++)
                {
                    int side = (i < sensorNumParFoot ? 0 : 1) << 5;
                    int val = sensorIdx == i ? (int)(force * 0.002f * 1023) : 0;
                    int high = (val >> 5) & 0x1f;
                    int low = val & 0x1f;
                    byte info = (byte)((0 << 6) | side | (i % sensorNumParFoot));
                    byte lowValue = (byte)((1 << 6) | side | low);
                    byte highValue = (byte)((2 << 6) | side | high);
                    data[dataIdx++] = info;
                    data[dataIdx++] = lowValue;
                    data[dataIdx++] = highValue;
                }
                data[dataIdx++] = (byte)0xff;
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