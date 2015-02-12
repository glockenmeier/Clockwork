extern alias FreeImageNET;
using FreeImageNET::FreeImageAPI;
using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Graphics;
using System;

namespace Clockwork.Terrain.Compiler
{
    public interface IImageSource
    {
        bool TryGenerate(ImageTreeBuilderContext context, Int2 position, Texture renderTarget);
    }

    public class ImageSource : ComponentBase, IImageSource
    {
        public TerrainMetrics Metrics { get; set; }

        public Texture Texture { get; private set; }

        public ImageSource(GraphicsDevice device, string url)
        {
            var dib = FreeImage.LoadEx(url);
            try
            {
                FreeImage.FlipVertical(dib);

                var colorType = FreeImage.GetColorType(dib);
                var imageType = FreeImage.GetImageType(dib);
                var bitsPerPixel = FreeImage.GetBPP(dib);

                int width = (int)FreeImage.GetWidth(dib);
                int height = (int)FreeImage.GetHeight(dib);
                IntPtr dataPointer = FreeImage.GetBits(dib);
                int rowStride = (int)FreeImage.GetPitch(dib);

                PixelFormat format;
                if (colorType == FREE_IMAGE_COLOR_TYPE.FIC_RGBALPHA && bitsPerPixel == 32)
                {
                    format = PixelFormat.B8G8R8A8_UNorm;
                }
                else if (colorType == FREE_IMAGE_COLOR_TYPE.FIC_MINISBLACK && bitsPerPixel == 16)
                {
                    format = PixelFormat.R16_UNorm;
                }
                else if (colorType == FREE_IMAGE_COLOR_TYPE.FIC_MINISBLACK && bitsPerPixel == 8)
                {
                    format = PixelFormat.R8_UNorm;
                }
                else
                {
                    throw new NotSupportedException();
                }

                using (var image = Image.New2D(width, height, 1, format, 1, dataPointer, rowStride))
                {
                    Texture = Texture.New(device, image).DisposeBy(this);
                }
            }
            finally
            {
                FreeImage.Unload(dib);
            }

            /*using (var textImage = textureTool.Load(sourcePathFromDisk))
            using (var image = textureTool.ConvertToParadoxImage(textImage))
            {
                heightMap = Texture2D.New(resamplerContext.GraphicsDevice, image);//.DisposeBy(this);
            }*/
        }

        public bool TryGenerate(ImageTreeBuilderContext context, Int2 position, Texture renderTarget)
        {
            var windowPosition = new Point(position.X * Metrics.PatchVertexStride - Metrics.Padding.X, position.Y * Metrics.PatchVertexStride - Metrics.Padding.X);
            var patchRect = new RectangleF(position.X * Metrics.PatchVertexStride, position.Y * Metrics.PatchVertexStride, Metrics.EffectiveVerticesPerPatch, Metrics.EffectiveVerticesPerPatch);
            var terrainRect = new RectangleF((Metrics.TotalVertexCount - Texture.Width) / 2, (Metrics.TotalVertexCount - Texture.Height) / 2, Texture.Width, Texture.Height);

            if (!terrainRect.Intersects(patchRect))
                return false;

            var scaleToTotalArea = Matrix.Scaling((float)Texture.Width / Metrics.TotalVertexCount, (float)Texture.Height / Metrics.TotalVertexCount, 0);
            var moveToWindow = Matrix.Translation(-(float)windowPosition.X / Metrics.TotalVertexCount * 2, (float)windowPosition.Y / Metrics.TotalVertexCount * 2, 0);
            var scaleToWindow = Matrix.Translation(new Vector3(1, -1, 0)) * Matrix.Scaling((float)Metrics.TotalVertexCount / Metrics.SourceSize) * Matrix.Translation(new Vector3(-1, 1, 0));
            var transform = scaleToTotalArea * moveToWindow * scaleToWindow;

            context.GraphicsDevice.SetRasterizerState(context.GraphicsDevice.RasterizerStates.CullNone);
            context.GraphicsDevice.Clear(renderTarget, Color.Black);
            context.GraphicsDevice.SetRenderTarget(renderTarget);
            
            context.Quad.Parameters.Set(SpriteBaseKeys.MatrixTransform, transform);
            context.Quad.Draw(Texture, context.SamplerState, Color.White);
            
            return true;
        }
    }
}
