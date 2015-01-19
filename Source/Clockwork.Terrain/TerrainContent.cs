using Clockwork.Serialization;
using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization.Contents;
using SiliconStudio.Core.Serialization.Converters;
using SiliconStudio.Paradox.Graphics;

namespace Clockwork.Terrain
{
    [ContentSerializer(typeof(DataContentConverterSerializer<TerrainContent>))]
    public class TerrainContent : QuadTreeContent<TerrainTileData>
    {
        public TerrainDescription Description { get; set; }

        public TextureContentChannel HeightMap { get; private set; }

        public TextureContentChannel ColorMap { get; private set; }

        public TextureContentChannel BlendMap { get; private set; }

        public TerrainContent(IServiceRegistry serviceRegistry, TerrainDescription description, int maximumTileCount)
            : base(serviceRegistry, description.Tree, maximumTileCount)
        {
            var device = serviceRegistry.GetServiceAs<IGraphicsDeviceService>().GraphicsDevice;

            Description = description;

            var store = new InterleavedTileStore("terrain__TILES");

            HeightMap = new TextureContentChannel(store.GetChannel(0), device, new TextureContentChannelData
                {
                    Description = new ImageDescription
                    {
                        Dimension = TextureDimension.Texture2D,
                        Width = Description.VerticesPerPatch,
                        Height = Description.VerticesPerPatch,
                        Depth = 1,
                        ArraySize = 1,
                        MipLevels = 1,
                        Format = Description.HeightMapFormat,
                    }
                });
            Channels.Add(HeightMap);

            ColorMap = new TextureContentChannel(store.GetChannel(1), device, new TextureContentChannelData
            {
                Description = new ImageDescription
                {
                    Dimension = TextureDimension.Texture2D,
                    Width = Description.VerticesPerPatch,
                    Height = Description.VerticesPerPatch,
                    Depth = 1,
                    ArraySize = 1,
                    MipLevels = 1,
                    Format = PixelFormat.R8G8B8A8_UNorm,
                }
            });
            //Channels.Add(colorMapChannel);

            BlendMap = new TextureContentChannel(store.GetChannel(2), device, new TextureContentChannelData
            {
                Description = new ImageDescription
                {
                    Dimension = TextureDimension.Texture2D,
                    Width = Description.VerticesPerPatch,
                    Height = Description.VerticesPerPatch,
                    Depth = 1,
                    ArraySize = 1,
                    MipLevels = 1,
                    Format = PixelFormat.R8_UNorm,
                }
            });
            Channels.Add(BlendMap);
        }

        protected override BoundingBox GetBoundingBox(QuadTreeNode<TerrainTileData> node)
        {
            return new BoundingBox
            {
                Minimum = new Vector3(node.BoundingRectangle.Left, node.Value.MinimumHeight, node.BoundingRectangle.Top),
                Maximum = new Vector3(node.BoundingRectangle.Right, node.Value.MaximumHeight, node.BoundingRectangle.Bottom)
            };
        }
    }
}
