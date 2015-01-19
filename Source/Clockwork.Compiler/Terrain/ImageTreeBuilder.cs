using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.TextureConverter;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Clockwork.Terrain.Compiler
{
    public class ImageTreeBuilder : ComponentBase
    {
        private struct QuadrantData
        {
            public Matrix Transform;
            public Rectangle ScissorRectangle;

            public RenderTarget LeafIntermediate;
            public RenderTarget LeafTarget;
        }

        public delegate bool SourceTextureProvider(Int2 position, RenderTarget renderTarget);

        private RenderTarget[] intermediates;
        private RenderTarget target;
        private PixelFormat targetFormat;
        private QuadrantData[] quadrants;
        private Matrix windowingTransform;
        private Texture2D stagingTexture;

        public ImageTreeBuilderContext Context { get; private set; }

        public TerrainMetrics Metrics { get; private set; }

        public SourceTextureProvider GenerateImage;

        public Action<Image, int, int, int> SaveImage;

        public ImageTreeBuilder(ImageTreeBuilderContext resamplerContext, PixelFormat intermediateFormat, PixelFormat targetFormat, TerrainMetrics metrics)
        {
            Context = resamplerContext;
            Metrics = metrics;
            this.targetFormat = targetFormat;

            target = CreateRenderTarget(metrics.VerticesPerPatch, intermediateFormat);

            stagingTexture = Texture2D.New(Context.GraphicsDevice, target.Description.ToStagingDescription()).DisposeBy(this);

            intermediates = new RenderTarget[metrics.LevelCount];
            for (int i = 0; i < metrics.LevelCount; i++)
                intermediates[i] = CreateRenderTarget(metrics.SourceSize, intermediateFormat);

            quadrants = new QuadrantData[4];
            for (int quadrant = 0; quadrant < 4; quadrant++)
            {
                bool positiveX = (quadrant & 1) != 0;
                bool positiveY = (quadrant & 2) != 0;

                Vector2 relativePosition = new Vector2(positiveX ? 1 : 0, positiveY ? 1 : 0);

                Vector2 corner = new Vector2(relativePosition.X * 2 - 1, -relativePosition.Y * 2 + 1);

                Vector2 padding = new Vector2(!positiveX ? Metrics.Padding.X : Metrics.Padding.Y, !positiveY ? Metrics.Padding.X : Metrics.Padding.Y);
                Vector2 margin = new Vector2(!positiveX ? Metrics.PatchMargin.X : Metrics.PatchMargin.Y, !positiveY ? Metrics.PatchMargin.X : Metrics.PatchMargin.Y);
                Vector2 vertsPerPatch = new Vector2(Metrics.VerticesPerPatch);
                Vector2 a1 = Divide(padding + vertsPerPatch - margin, vertsPerPatch + padding * 2);

                var a2 = Divide(new Vector2(Metrics.SourceSize), padding + vertsPerPatch - margin);
                var cutOppositeBoder = Scaling(a2, corner);
                var scaleToMatchOuterVertex = Scaling(a1, -corner);

                var scale = (padding + vertsPerPatch * 0.5f) / Metrics.SourceSize;
                var moveToQuadrant = Scaling(scale, corner);

                quadrants[quadrant] = new QuadrantData
                {
                    Transform = cutOppositeBoder * scaleToMatchOuterVertex * moveToQuadrant,

                    ScissorRectangle = new Rectangle
                    {
                        X = !positiveX ? 0 : Metrics.SourceSize - (int)(padding.X + Metrics.VerticesPerPatch * 0.5f),
                        Y = !positiveY ? 0 : Metrics.SourceSize - (int)(padding.Y + Metrics.VerticesPerPatch * 0.5f),
                        Width = (int)(padding.X + Metrics.VerticesPerPatch * 0.5f),
                        Height = (int)(padding.Y + Metrics.VerticesPerPatch * 0.5f)
                    },

                    LeafIntermediate = CreateRenderTarget(metrics.SourceSize, intermediateFormat),
                    LeafTarget = CreateRenderTarget(metrics.VerticesPerPatch, intermediateFormat)
                };
            }

            float d = 2 * (Metrics.Padding.Y - Metrics.Padding.X) * 0.5f / Metrics.SourceSize;
            windowingTransform =
                Matrix.Translation(d, -d, 0) *
                Matrix.Scaling((float)Metrics.SourceSize / Metrics.VerticesPerPatch);
        }

        private RenderTarget CreateRenderTarget(int size, PixelFormat format)
        {
            var texture = Texture2D.New(Context.GraphicsDevice, size, size, 1, format, TextureFlags.ShaderResource | TextureFlags.RenderTarget).DisposeBy(this);
            return texture.ToRenderTarget().DisposeBy(this);
        }

        public void Build()
        {
            BuildRecursive(0, Int2.Zero).Wait();
        }

        protected virtual void ProcessImage(Image image, int depth, Point position) { }

        protected virtual async Task<bool> BuildRecursive(int depth, Int2 position)
        {
            Context.GraphicsDevice.Clear(intermediates[depth], Color.Black);

            if (depth < Metrics.MaximumLevel)
            {
                if (false)//depth > 6)
                {
                    var childTasks = new Task<bool>[4]; 
                    for (int i = 0; i < 4; i++)
                    {
                        var childPosition = new Int2(position.X * 2 + (i % 2), position.Y * 2 + (i / 2));
                        childTasks[i] = BuildRecursive(depth + 1, childPosition);
                    }

                    var results = await Task.WhenAll(childTasks);

                    if (!results.Any())
                        return false;
                }
                else
                {
                    bool anyChildren = false;
                    for (int i = 0; i < 4; i++)
                    {
                        var childPosition = new Int2(position.X * 2 + (i % 2), position.Y * 2 + (i / 2));
                        anyChildren |= await BuildRecursive(depth + 1, childPosition);
                    }

                    if (!anyChildren)
                        return false;
                }
            }
            else
            {
                if (GenerateImage == null || !GenerateImage(position, intermediates[depth]))
                    return false;
            }

            if (depth > 0)
            {
                var quadrant = quadrants[(position.X % 2) + ((position.Y % 2) << 1)];
                Draw(intermediates[depth].Texture, quadrant.Transform, intermediates[depth - 1], quadrant.ScissorRectangle);
            }

            Draw(intermediates[depth].Texture, windowingTransform, target);

            await Save(target.Texture, depth, (int)position.X, (int)position.Y);
            
            return true;
        }

        public Vector2 Divide(Vector2 left, Vector2 right)
        {
            return new Vector2(left.X / right.X, left.Y / right.Y);
        }

        public Matrix Scaling(float scale, Vector2 scaleCenter)
        {
            return Matrix.Translation(-(Vector3)scaleCenter) * Matrix.Scaling(scale) * Matrix.Translation((Vector3)scaleCenter);
        }

        public Matrix Scaling(Vector2 scale, Vector2 scaleCenter)
        {
            return Matrix.Translation(-(Vector3)scaleCenter) * Matrix.Scaling(scale.X, scale.Y, 1) * Matrix.Translation((Vector3)scaleCenter);
        }

        private void Draw(Texture source, Matrix transform, RenderTarget target, Rectangle? scissorRectangle = null)
        {
            Context.GraphicsDevice.SetRenderTargets(target);

            if (scissorRectangle.HasValue)
            {
                Context.GraphicsDevice.SetRasterizerState(Context.ScissorTestEnabled);
                Context.GraphicsDevice.SetScissorRectangles(scissorRectangle.Value);
            }
            else
            {
                Context.GraphicsDevice.SetRasterizerState(Context.GraphicsDevice.RasterizerStates.CullNone);
            }

            Context.Effect.Transform = transform;
            Context.Quad.Draw(source);
        }

        private Image image;

        private async Task Save(Texture texture, int depth, int x, int y)
        {
            int level = Metrics.MaximumLevel - depth;

            if (image == null)
                image = Image.New(texture.Description).DisposeBy(this);

            texture.GetData(stagingTexture, new DataPointer(image.DataPointer, image.TotalSizeInBytes));

            ProcessImage(image, depth, new Point(x, y));

            if (SaveImage == null)
                return;

            if (targetFormat.IsCompressed())
            {
                /*
                // Reinterpret pixels if necessary
                var description = image.Description;
                if (description.Format == PixelFormat.R16_UNorm)
                {
                    switch (targetFormat)
                    {
                        case PixelFormat.BC1_UNorm:
                            description.Format = PixelFormat.B5G6R5_UNorm;
                            break;
                        case PixelFormat.BC5_UNorm:
                            description.Format = PixelFormat.R8G8_UNorm;
                            break;
                    }
                }
                image.Description = description;
                */

                using (var texTool = new TextureTool())
                using (var tex = texTool.Load(image))
                {
                    texTool.Compress(tex, targetFormat, SiliconStudio.TextureConverter.Requests.TextureQuality.Best);
                    using (var compressedImage = texTool.ConvertToParadoxImage(tex))
                    {
                        SaveImage(compressedImage, level, x, y);
                    }
                }
            }
            else
            {
                SaveImage(image, level, x, y);
            }
        }

        private static Matrix TransformRectangle(Vector2 sourceLocation, Vector2 sourceSize, Vector2 destLocation, Vector2 destSize)
        {
            return TransformRectangle(
                new RectangleF(sourceLocation.X, sourceLocation.Y, sourceSize.X, sourceSize.Y),
                new RectangleF(destLocation.X, destLocation.Y, destSize.X, destSize.Y));
        }

        private static Matrix TransformRectangle(RectangleF source, RectangleF destination)
        {
            Matrix undoSource = Matrix.Translation(1f - source.Center.X * 2f, source.Center.Y * 2f - 1f, 0);
            Matrix scale = Matrix.Scaling(destination.Width / source.Width, destination.Height / source.Height, 1);
            Matrix applyDest = Matrix.Translation(destination.Center.X * 2f - 1f, 1f - destination.Center.Y * 2f , 0);
            return undoSource * scale * applyDest;
        }
    }
}
