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
using System.IO;

namespace FLib
{
    public class SockswitchNaiveRecognizeSetting
    {

    }

    public enum SockswitchNaiveRecognizeCommandType
    {
        Tap, MultiTap, DoubleTap, DoubleMultiTap, Move, None
    }

    public class SockswitchNaiveGestureCommand
    {
        public SockswitchNaiveRecognizeCommandType type = SockswitchNaiveRecognizeCommandType.None;
        public string CommandName = "";
        public float DoubleTapDuration = 0;
        public float SingleTapDuration = 0;
        public List<int> sensorId = new List<int>();
        public List<float> sensorIncrease = new List<float>();
        public List<long> sensorDuration = new List<long>();

        public SockswitchNaiveGestureCommand()
        {

        }

        public SockswitchNaiveGestureCommand(string startLine, StringReader sr)
        {
            CommandName = StringToTokens(startLine, "CommandName")[1];

            // TODO
            while (true)
            {
                string line = sr.ReadLine().Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("//")) continue;
                if (line == "endcom") break;
                var tokens = StringToTokens(line);
                switch (tokens[0])
                {
                    case "Type":
                        type = (SockswitchNaiveRecognizeCommandType)Enum.Parse(typeof(SockswitchNaiveRecognizeCommandType), tokens[1]);
                        break;
                    case "DoubleTapDuration":
                        DoubleTapDuration = long.Parse(tokens[1]);
                        break;
                    case "SingleTapDuration":
                        SingleTapDuration = long.Parse(tokens[1]);
                        break;
                    case "SensorId":
                        sensorId.Add(int.Parse(tokens[1]));
                        break;
                    case "Increase":
                        sensorIncrease.Add(float.Parse(tokens[1]));
                        break;
                    case "Duration":
                        sensorDuration.Add(long.Parse(tokens[1]));
                        break;
                }
            }
        }

        public string[] StringToTokens(string str, string expectedTokenName = "")
        {
            var tokens = str.Split(':', '=').Select(s => s.Trim()).ToArray();
            Debug.Assert(tokens.Length >= 2);
            if (expectedTokenName != "") Debug.Assert(tokens[0] == expectedTokenName);
            return tokens;
        }
    }

    public class SockswitchNaiveRecognizer
    {
        public List<SockswitchNaiveGestureCommand> gestureCommands = new List<SockswitchNaiveGestureCommand>();
        public Dictionary<long, string> gestureHistory = new Dictionary<long, string>();

        public SockswitchNaiveRecognizer(string settingPath)
        {
            StringReader sr = new StringReader(File.ReadAllText(settingPath));

            while (true)
            {
                string line = sr.ReadLine().Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("//")) continue;
                if (line == "end") break;
                SockswitchNaiveGestureCommand com = new SockswitchNaiveGestureCommand(line, sr);
                gestureCommands.Add(com);
            }
        }

        public string Update(SockswitchSensor sensor)
        {
            if (sensor == null) return "sensor is null";

            // TODO
            long currentTime;
            float[] currentPressures = sensor.GetPressureData(sensor.FrameCount - 1, out currentTime);

            string GestureText = "";

            foreach (SockswitchNaiveGestureCommand com in gestureCommands)
            {
                int startFrame;
                bool isDown = IsDown(sensor, sensor.FrameCount - 1, com, out startFrame);
                if (isDown)
                {
                    if (com.DoubleTapDuration > 0)
                    {
                        int startFrame1, startFrame2;
                        if (IsUp(sensor, startFrame, com, out startFrame1) &&
                            IsDown(sensor, startFrame1, com, out startFrame2))
                        {
                            GestureText += "\n" + com.CommandName;
                        }
                    }
                    else
                    {
                        GestureText += "\n" + com.CommandName;
                    }
                }
            }

            if (gestureHistory.Count <= 0 || gestureHistory.Values.Last() != GestureText)
                gestureHistory.Add(currentTime, GestureText);

            return gestureHistory.Count >= 1 ? gestureHistory.Values.Last() : "None";
        }

        bool IsDown(SockswitchSensor sensor, int refFrame, SockswitchNaiveGestureCommand com, out int startFrame, bool detectDown = true)
        {
            float maxDuration =
                com.SingleTapDuration > 0 ? com.SingleTapDuration :
                com.sensorDuration.Max();

            long refTime;
            float[] refPressures = sensor.GetPressureData(refFrame, out refTime);
            
            long time = refTime;
            float[] mins = new float[sensor.sensorNum];
            bool[] isDown = new bool[sensor.sensorNum];
            int restButton = com.sensorId.Count;
            int frameCnt = sensor.FrameCount - 1;
            startFrame = refFrame;

            for (int j = 0; j < mins.Length; j++) mins[j] = float.MaxValue;
            
            while (refTime - time < maxDuration && restButton >= 1)
            {
                float[] pressures = sensor.GetPressureData(frameCnt, out time);
                if (pressures == null) break;
                Debug.Assert(pressures.Length == mins.Length);
                for (int j = 0; j < com.sensorId.Count; j++)
                {
                    int id = com.sensorId[j];
                    mins[id] = Math.Min(mins[id], pressures[id]);
                    if ((detectDown && refPressures[id] - mins[id] >= com.sensorIncrease[j]) ||
                        (!detectDown && mins[id] - refPressures[id] >= com.sensorIncrease[j]))
                    {
                        if (isDown[id] == false)
                        {
                            isDown[id] = true;
                            restButton--;
                            if (restButton <= 0)
                            {
                                startFrame = frameCnt;
                            }
                        }
                    }
                }
                frameCnt--;
            }

            return restButton <= 0;
        }

        bool IsUp(SockswitchSensor sensor, int refFrame, SockswitchNaiveGestureCommand com, out int startFrame)
        {
            return IsDown(sensor, refFrame, com, out startFrame, false);
        }
    }

}