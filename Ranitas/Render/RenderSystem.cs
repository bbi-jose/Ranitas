﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ranitas.Core;
using Ranitas.Core.ECS;
using Ranitas.Core.Render;
using Ranitas.Data;
using Ranitas.Pond;
using Ranitas.Sim;
using Ranitas.Sim.Events;

namespace Ranitas.Render
{
    public sealed class RenderSystem : ISystem
    {
        public RenderSystem(GraphicsDevice graphicsDevice, PondSimState pond, Texture2D frogSprite, SpriteFont uiFont, FrogAnimationData animationData)
        {
            mRenderer = new PrimitiveRenderer();
            mRenderer.Setup(graphicsDevice);

            mFrogRenderer = new FrogRenderer();
            mFrogRenderer.Setup(graphicsDevice, frogSprite, animationData);

            mUIFont = uiFont;

            mPond = pond;
            mDevice = graphicsDevice;
            SetupCamera(graphicsDevice, pond);

            mUISpriteBatch = new SpriteBatch(mDevice);
        }

        private PrimitiveRenderer mRenderer;
        private FrogRenderer mFrogRenderer;
        private SpriteFont mUIFont;
        private SpriteBatch mUISpriteBatch;
        private Matrix mCameraMatrix;
        private PondSimState mPond;    //TODO: Make lily pads entities so they can be rendered as the rest!
        private GraphicsDevice mDevice;

        private struct ColoredRectSlice
        {
            public SliceRequirementOutput<Rect> Rect;
            public SliceRequirementOutput<Color> Color;
        }
        private ColoredRectSlice mColoredRectSlice;

        private struct FrogRectSlice
        {
            public SliceRequirementOutput<Rect> Rect;
            public SliceRequirementOutput<AnimationState> Animation;
        }
        private FrogRectSlice mFrogRectSlice;

        private struct PlayerSlice
        {
            public SliceRequirementOutput<Player> Player;
            public SliceRequirementOutput<Score> Score;
        }
        private PlayerSlice mPlayerSlice;

        public void Initialize(EntityRegistry registry, EventSystem eventSystem)
        {
            registry.SetupSlice(ref mColoredRectSlice);
            registry.SetupSlice(ref mFrogRectSlice);
            registry.SetupSlice(ref mPlayerSlice);
        }

        public void Update(EntityRegistry registry, EventSystem eventSystem)
        {
            mDevice.Clear(Color.DimGray);

            RenderLilies();

            int frogCount = mFrogRectSlice.Rect.Count;
            for (int i = 0; i < frogCount; ++i)
            {
                mFrogRenderer.PushFrog(mFrogRectSlice.Rect[i], mFrogRectSlice.Animation[i]);
            }
            mFrogRenderer.Render(mCameraMatrix, mDevice);

            int rectCount = mColoredRectSlice.Rect.Count;
            for (int i = 0; i < rectCount; ++i)
            {
                mRenderer.PushRect(mColoredRectSlice.Rect[i], mColoredRectSlice.Color[i]);
            }

            RenderWater();

            mRenderer.Render(mCameraMatrix, mDevice);

            RenderUI();
        }

        private void RenderLilies()
        {
            foreach (var lily in mPond.Lilies)
            {
                mRenderer.PushRect(lily.Rect, Color.Green);
            }
        }

        private void RenderWater()
        {
            int wide = (int)mPond.Width;
            Rect waterRect = new Rect(new Vector2(-wide, 0f), new Vector2(wide, mPond.WaterLevel));
            Color waterColor = Color.DarkBlue;
            waterColor.A = 1;
            mRenderer.PushRect(waterRect, waterColor);
        }

        private void RenderUI()
        {
            float playerAreaWidth = (float)(mDevice.DisplayMode.Width) / 4f;
            int playerCount = mPlayerSlice.Player.Count;
            mUISpriteBatch.Begin(depthStencilState: DepthStencilState.DepthRead);
            for (int i = 0; i < playerCount; ++i)
            {
                float xPosition = mPlayerSlice.Player[i].Index * playerAreaWidth;
                Vector2 position = new Vector2(xPosition + playerAreaWidth * 0.25f, playerAreaWidth * 0.25f);
                //TODO: Can we avoid string allocations... does it even matter?
                string scoreString = string.Format("Score: {0}", mPlayerSlice.Score[i].Value);
                mUISpriteBatch.DrawString(mUIFont, scoreString, position, Color.BurlyWood);
            }
            mUISpriteBatch.End();
        }

        private void SetupCamera(GraphicsDevice device, PondSimState pond)
        {
            float pondWidth = pond.Width;
            float pondHeight = pond.Height;
            float aspectRatio = device.Adapter.CurrentDisplayMode.AspectRatio;
            Matrix translation = Matrix.CreateTranslation(-aspectRatio * pondHeight * 0.5f, -pondHeight * 0.5f, 0f);
            Matrix projectionMatrix = Matrix.CreateOrthographic(aspectRatio * pondHeight, pondHeight, -100, 100);
            mCameraMatrix = translation * projectionMatrix;
        }
    }
}
