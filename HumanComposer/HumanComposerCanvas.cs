using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FLib;

namespace HumanComposer
{
    public partial class HumanComposerCanvas : XNAControl
    {
        SpriteBatch spriteBatch;
        Texture2D Texture = null;
        Color[] texData = null;

        Skeleton skeleton;

        protected override void Initialize()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            XNATexture.Load(GraphicsDevice, "dummy.png", out Texture, out texData);

            // スケルトンを作成
            

            skeleton = new Skeleton(null);
        }

        protected override void Draw()
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin();
            skeleton.Draw(GraphicsDevice);
//            spriteBatch.Draw(Texture, new Rectangle(0, 0, Width, Height), Color.White);
            spriteBatch.End();
        }
    }
}
