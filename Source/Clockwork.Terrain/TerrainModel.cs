using Clockwork.Serialization;
using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Effects.Modules;
using SiliconStudio.Paradox.Graphics;
using System;

namespace Clockwork.Terrain
{
    public class TerrainModel : ComponentBase
    {
        private readonly GraphicsDevice graphics;
        private readonly TerrainPatch patch;
        private readonly TerrainTileInfo[] bufferData;
        private readonly TerrainSelection selection;
        private readonly ParameterCollection parameters = new ParameterCollection();

        private QuadTreeContentObserver observer;
        private Camera camera;

        public TerrainContent Content { get; private set; }

        public TerrainModel(GraphicsDevice graphics, TerrainContent content, Camera camera)
        {
            this.camera = camera;
            this.graphics = graphics;

            var tileDesc = new ImageTileDescription(content.Description.VerticesPerPatch, content.Description.VertexOverlap);

            int leafNodeSize = tileDesc.EffectiveRasterSize;
            var visibleRanges = new DefaultVisibleRanges(content.Description.Tree.MaximumDepth + 1, leafNodeSize, content.Description.PatchSize / leafNodeSize);

            selection = new TerrainSelection(visibleRanges, 1000) { Camera = camera };
            observer = new QuadTreeContentObserver(visibleRanges);

            Content = content;
            content.Observers.Add(observer);

            float heightScale = content.Description.HeightScale;
            int tesselation = tileDesc.EffectiveRasterSize;
            tesselation += tesselation % 2;

            bufferData = new TerrainTileInfo[selection.MaxSelectedNodeCount];
            patch = new TerrainPatch(graphics, tesselation, selection.MaxSelectedNodeCount).DisposeBy(this);

            UpdateMorphConstants(visibleRanges);
            parameters.Set(TerrainBaseKeys.HalfTesselation, tesselation * 0.5f);
            parameters.Set(TerrainBaseKeys.TwoOverTesselation, 2.0f / tesselation);
            parameters.Set(TerrainBaseKeys.TexelSize, 1.0f / tileDesc.Size);
            parameters.Set(TerrainBaseKeys.HeightScale, heightScale);
            parameters.Set(TerrainBaseKeys.TwoTexelSizeOverHeightScale, 2.0f / tileDesc.EffectiveRasterSize / heightScale);
            parameters.Set(TerrainBaseKeys.RelativeMarginWidth, tileDesc.RelativeRasterOffset);
            parameters.Set(TerrainBaseKeys.RelativeEffectivePatchSize, tileDesc.RelativeEffectiveRasterSize);

            parameters.Set(TerrainBaseKeys.HeightMap, content.HeightMap.Texture);
            content.HeightMap.TextureChanged += (texture) => parameters.Set(TerrainBaseKeys.HeightMap, texture);

            parameters.Set(TerrainBaseKeys.ColorMap, content.ColorMap.Texture);            
            content.ColorMap.TextureChanged += (texture) => parameters.Set(TerrainBaseKeys.ColorMap, texture);

            parameters.Set(SplattingKeys.BlendMap, content.BlendMap.Texture);
            content.BlendMap.TextureChanged += (texture) => parameters.Set(SplattingKeys.BlendMap, texture);
        }

        const float MorphStartRatio = 0.66f;
        const float ErrorFudge = 0.01f;

        // TODO: Take terrain height into account when calculating morph starting distance. Possibly per-vertex in shader?
        // TODO: First morph start can be 0.0f. Need to investigate into morph precision issues
        private void UpdateMorphConstants(IVisibleRanges visibleRanges)
        {
            var morphConstants = new Vector2[visibleRanges.Count];
            float previousStart = 0;
            for (int i = 0; i < visibleRanges.Count; i++)
            {
                var end = visibleRanges[i];
                var morphAmount = i == 0 ? 0.5f : (float)(1.5 * Math.Pow(2, i) / ((Math.Pow(2.5, i + 1) - Math.Pow(2.5, i)) * 1.5));
                var start = MathUtil.Lerp(previousStart, end, morphAmount);
                previousStart = end;
                //var start = MathUtil.Lerp(previousStart, end, MorphStartRatio);
                end = MathUtil.Lerp(end, start, ErrorFudge);
                //previousStart = start;

                morphConstants[i] = new Vector2(end / (end - start), 1 / (end - start));
            }

            parameters.SetArray(TerrainBaseKeys.MorphConsts, morphConstants);
        }

        public void Update(TimeSpan elapsedTime)
        {
            observer.Position = camera.Position;
            Content.Observe();
        }

        public void Draw(RenderContext renderContext, Effect effect)
        {
            var view = renderContext.CurrentPass.Parameters.Get(TransformationKeys.View);
            var projection = renderContext.CurrentPass.Parameters.Get(TransformationKeys.Projection);

            Content.Select(selection);

            int instanceCount = 0;

            var tileDesc = new ImageTileDescription(Content.Description.VerticesPerPatch, Content.Description.VertexOverlap);
            float relativeBorderWidth = (Content.Description.VertexOverlap.X + 0.5f) / Content.Description.VerticesPerPatch;

            for (int i = 0; i < selection.SelectedNodeCount; i++)
            {
                var node = selection.GetSelectedNode(i);

                if (node.Depth == 0)
                {
                    // TODO: Make parent data equal to instance data?
                    continue;
                }

                // TODO: Use parent data if child has not finished loading and vice versa?

                if (node.Value.State != TileState.Mapped || node.Parent.Value.State != TileState.Mapped)
                    continue;

                var relativePosition = new Vector2(node.Position.X % 2, node.Position.Y % 2);
                var parentOffset = new Vector2(tileDesc.RelativeRasterOffset) + relativePosition * tileDesc.RelativeEffectiveRasterSize * 0.5f;

                bufferData[instanceCount++] = new TerrainTileInfo
                {
                    Offset = node.BoundingRectangle.Location,
                    Scale = node.BoundingRectangle.Width,
                    Level = Content.Description.Tree.MaximumDepth - node.Depth,
                    TextureIndex = node.Value.PhysicalOffset,
                    ParentOffset = parentOffset,
                    ParentTextureIndex = node.Parent.Value.PhysicalOffset
                };
            }

            effect.Parameters.Set(TransformationKeys.World, Matrix.Identity);
            for (int i = 0; i < instanceCount; i++)
            {
                var data = bufferData[i];
                effect.Parameters.Set(TerrainRenderer.TileInfo, data);
                effect.Apply(renderContext.CurrentPass.Parameters, parameters, true);
                patch.Draw(renderContext.GraphicsDevice, true);
            }
        }
    }
}
