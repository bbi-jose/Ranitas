﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ranitas.Core;
using Ranitas.Core.Render;

namespace Ranitas.Insects
{
    public sealed class FlyRenderer
    {
        private PrimitiveRenderer mRenderer;

        private FlySim mFlySim;

        public FlyRenderer(FlySim flySim)
        {
            mRenderer = new PrimitiveRenderer();
            mFlySim = flySim;
        }

        public void Setup(GraphicsDevice device)
        {
            mRenderer.Setup(device);    //TODO: Share same rect renderer!
        }

        public void Render(GraphicsDevice device)
        {
            foreach (var fly in mFlySim.ActiveFlies)
            {
                Rect flyRect = new Rect(fly.Position, mFlySim.FlyData.Width, mFlySim.FlyData.Height);
                mRenderer.PushRect(flyRect, Color.Coral);
            }
            mRenderer.Render(device);
        }
    }
}
