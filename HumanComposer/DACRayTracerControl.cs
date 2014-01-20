using System;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using System.Threading.Tasks;

namespace DACRayTracer2
{
    using XnaColor = Microsoft.Xna.Framework.Color;

    class DACRayTracerControl : GraphicsDeviceControl
    {
        const int screenWidth = 512;
        const int screenHeight = 512;
        const int sampling = 1;
        const string modelPath = "../resource/teapot.obj";
        readonly Rectangle rect = new Rectangle(0, 0, screenWidth, screenHeight);
        
        DACRayTracer tracer;
        Color[] colorMap;
        Texture2D texture;

        // 視点
        Vector3 eye = new Vector3(0, 5, 7);
        Vector3 center = new Vector3(0, 1, 0);
        Vector3 up = new Vector3(0, 1, 0);
        float fov = 60.0f;

        // ライト
        Vector3 lightPos = new Vector3(10, 10, 10);
        Vector3 lightColor = new Vector3(255 * 10, 255 * 10, 255 * 10);
          
        Task task;
        Action drawAction;


        SpriteBatch spriteBatch;

        double drawTime = 0;
        int drawCnt = 0;

        public bool? UseNaiveRT { set; private get; }
        public Label EllapsedTimeLabel { private get; set; }
        public Label FPSLabel { private get; set; }
        public int? OcculusionSampling { set; private get; }

        protected override void Initialize()
        {
            // テクスチャ
            colorMap = new Color[screenWidth * screenHeight];
            texture = new Texture2D(GraphicsDevice, screenWidth, screenHeight);

            OcculusionSampling = 0;

            // レンダラの初期化
            tracer = new DACRayTracer(
                modelPath,
                screenWidth, screenHeight, sampling,
                new Camera(eye, center, up, fov, screenWidth, screenHeight, sampling),
                new Light(lightPos, lightColor));

            // DACRTを行うスレッドを初期化
            drawAction = () =>
                {
                    try
                    {
                        drawTime += tracer.Draw(colorMap, screenWidth, screenHeight, OcculusionSampling.Value, UseNaiveRT.Value);
                        drawCnt++;
                    }
                    catch (InvalidOperationException e)
                    {
                        MessageBox.Show(e.ToString());
                    }
                };
            task = new Task(drawAction);

            // その他
            spriteBatch = new SpriteBatch(GraphicsDevice);
            Application.Idle += (sender, e) => Invalidate();

            for (int i = 0; i < colorMap.Length; i++)
            {
                colorMap[i].A = 255;
            }
        }

        public bool MultiThread {get; set;}

        protected override void Draw()
        {
            if (MultiThread == false && task.Status != TaskStatus.Running)
            {
                drawAction();
            }
            else
            {
                // 描画スレッドの処理
                switch (task.Status)
                {
                    case TaskStatus.Created:            // 生成されたら実行
                        task.Start();
                        break;
                    case TaskStatus.RanToCompletion:    // 終了したら作りなおす
                        task = new Task(drawAction);
                        break;
                    default:
                        break;
                }
            }
            
            GraphicsDevice.Textures[0] = null;  // これをしないでtexture.SetDataを呼ぶと例外が発生する
            texture.SetData(colorMap);

            spriteBatch.Begin();
            spriteBatch.Draw(texture, rect, Color.White);
            spriteBatch.End();

            double averageTime = drawCnt > 0 ? drawTime / drawCnt : 0;
            double fps = 1000 / averageTime;
            EllapsedTimeLabel.Text = drawCnt > 0 ? string.Format("Average Elapsed Time : {0 : .000} ms", averageTime) : "";
            FPSLabel.Text = drawCnt > 0 ? string.Format("fps: {0 : .00}", fps) : "";
        }
    }
}
