extern alias FreeImageNET;
using Clockwork.Serialization;
using FreeImageNET::FreeImageAPI;
using SiliconStudio.Assets.Compiler;
using SiliconStudio.BuildEngine;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.LZ4;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Graphics;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Clockwork.Terrain.Compiler
{
    public class TerrainAssetCompiler : AssetCompilerBase<TerrainAsset>
    {
        protected override void Compile(AssetCompilerContext context, string urlInStorage, UFile assetAbsolutePath, TerrainAsset asset, AssetCompilerResult result)
        {
            UDirectory parent = assetAbsolutePath.GetParent();
            UFile sourcePathFromDisk = UPath.Combine(parent, asset.Source);

            result.BuildSteps = new ListBuildStep
            {
                new TerrainAssetCommand(urlInStorage, sourcePathFromDisk, asset)
            };
        }
    }

    /// <summary>
    /// Command used by the build engine to convert the asset
    /// </summary>
    public class TerrainAssetCommand : AssetCommand<TerrainAsset>
    {
        private UFile sourcePathFromDisk;
        private ImageTreeBuilderContext resamplerContext;
        private TerrainMetrics metrics;

        protected TagSymbol DisableCompressionSymbol;

        public TerrainAssetCommand(string url, UFile sourcePathFromDisk, TerrainAsset asset)
            : base(url, asset)
        {
            this.sourcePathFromDisk = sourcePathFromDisk;

            DisableCompressionSymbol = RegisterTag(Builder.DoNotCompressTag, () => Builder.DoNotCompressTag);
        }

        public override IEnumerable<ObjectUrl> GetInputFiles()
        {
            yield return new ObjectUrl(UrlType.File, sourcePathFromDisk);
        }

        protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            var objectURL = new ObjectUrl(UrlType.Internal, Url + "__TILES");
            commandContext.AddTag(objectURL, DisableCompressionSymbol);

            long startTime = Stopwatch.GetTimestamp();

            using (resamplerContext = new ImageTreeBuilderContext())
            {
                Compile(commandContext);
            }

            long endTime = Stopwatch.GetTimestamp();
            double duration = (endTime - startTime) / (double)Stopwatch.Frequency;
            commandContext.Logger.Info(string.Format("Terrain compiled in {0} seconds", duration));

            return Task.FromResult(ResultStatus.Successful);
        }

        private void Compile(ICommandContext commandContext)
        {
            var assetManager = new AssetManager();

            int width, height;
            int tileCount;
            int channelCount = 4;

            DataRange[,] tempOffsets;
            ImageTreeDescription treeDescription;

            var dib = FreeImage.LoadEx(sourcePathFromDisk, FREE_IMAGE_LOAD_FLAGS.FIF_LOAD_NOPIXELS);
            try
            {
                width = (int)FreeImage.GetWidth(dib);
                height = (int)FreeImage.GetHeight(dib);
            }
            finally
            {
                FreeImage.Unload(dib);
            }

            // Adjust for block compression
            int tileSize = Asset.VerticesPerPatch + (4 - Asset.VerticesPerPatch % 4);
            var overlap = new Int2(1, 2);
            treeDescription = ImageTreeDescription.FromSize(width, height, new ImageTileDescription(tileSize, overlap));

            tileCount = ((1 << (2 * treeDescription.LevelCount)) - 1) / 3;
            tempOffsets = new DataRange[tileCount, channelCount];

            using (var tempStream = new MemoryStream())
            {
                var tempWriter = new BinarySerializationWriter(tempStream);
  
                using (var heightMap = new ImageSource(resamplerContext.GraphicsDevice, sourcePathFromDisk))
                {
                    metrics = new TerrainMetrics
                    {
                        HeightScale = Asset.HeightScale,
                        VertexSpacing = Asset.VertexSpacing,
                        VerticesPerPatch = tileSize,
                        VertexOverlap = overlap, // Asymmetric, so we can block compress
                        PatchCount = treeDescription.TileCount
                    };

                    heightMap.Metrics = metrics;

                    TerrainDescription description = new TerrainDescription
                    {
                        HeightScale = metrics.HeightScale,
                        VerticesPerPatch = metrics.VerticesPerPatch,
                        VertexOverlap = metrics.VertexOverlap,
                        PatchSize = metrics.PatchScale,
                        HeightMapFormat = Asset.HeightMapFormat,
                        HeightMapName = Url + "__HEIGHT",
                        Tree = new QuadTree<TerrainTileData>(metrics.Bounds, metrics.MaximumLevel),
                        Materials = asset.Layers.Select(layer => new ContentReference<Material>(layer.Material.Id, layer.Material.Location)).ToList()
                    };

                    using (var heightMapBuilder = new HeightMapTreeBuilder(resamplerContext, PixelFormat.R16_UNorm, Asset.HeightMapFormat, metrics, description))
                    using (var collisionStream = AssetManager.FileProvider.OpenStream(Url + "__COLLISION_ALL", VirtualFileMode.Create, VirtualFileAccess.Write))
                    {
                        heightMapBuilder.GenerateImage = (position, target) => heightMap.TryGenerate(resamplerContext, position, target);

                        heightMapBuilder.SaveImage = (image, level, x, y) =>
                        {
                            var pixelBuffer = image.PixelBuffer[0];
                            int index = QuadTreeHelper.GetNodeIndex(metrics.MaximumLevel - level, new Int2(x, y));

                            var start = (int)tempStream.Position;
                            tempWriter.Serialize(pixelBuffer.DataPointer, pixelBuffer.BufferStride);
                            tempOffsets[index, 0] = new DataRange(start, (int)tempStream.Position - start);
                        };

                        var collisionMap = new int[1 << (metrics.MaximumLevel * 2)];
                        for (int i = 0; i < collisionMap.Length; i++)
                            collisionMap[i] = -1;
                        collisionStream.Position = collisionMap.Length * 4;

                        heightMapBuilder.SaveHeightMap = (data, x, y) =>
                        {
                            int index = (1 << metrics.MaximumLevel) * y + x;
                            collisionMap[index] = (int)collisionStream.Position;
                            var stream = new BinarySerializationWriter(collisionStream);
                            stream.Write(data);
                        };
                        heightMapBuilder.Build();

                        collisionStream.Position = 0;
                        var stream2 = new BinarySerializationWriter(collisionStream);
                        for (int i = 0; i < collisionMap.Length; i++)
                            stream2.Write(collisionMap[i]);

                    }

                    assetManager.Save(Url, description);
                }

                assetManager.Save(Url + "__COLLISION", new RegularGridContentData
                {
                    Bounds = new Rectangle(0, 0, metrics.PatchCount, metrics.PatchCount),
                    Origin = new Vector2(-metrics.PatchCount * 0.5f * metrics.PatchScale),
                    CellSize = new Vector2(metrics.PatchScale),
                    NamePattern = Url + "__COLLISION_ALL", // /{0}_{1}",
                });

                UFile path = "Tint.png";
                using (var diffuseMap = new ImageSource(resamplerContext.GraphicsDevice, UPath.Combine(sourcePathFromDisk.GetDirectory(), path)))
                {
                    diffuseMap.Metrics = metrics;

                    using (var builder = new ImageTreeBuilder(resamplerContext, PixelFormat.R8G8B8A8_UNorm, PixelFormat.R8G8B8A8_UNorm, metrics))
                    {
                        builder.GenerateImage = (position, target) => diffuseMap.TryGenerate(resamplerContext, position, target);

                        builder.SaveImage = (image, level, x, y) =>
                        {
                            var pixelBuffer = image.PixelBuffer[0];
                            int index = QuadTreeHelper.GetNodeIndex(metrics.MaximumLevel - level, new Int2(x, y));

                            var start = (int)tempStream.Position;
                            tempWriter.Serialize(pixelBuffer.DataPointer, pixelBuffer.BufferStride);
                            tempOffsets[index, 1] = new DataRange(start, (int)tempStream.Position - start);
                        };

                        builder.Build();
                    }
                }

                QuadTree<MaterialTreeData> tree = new QuadTree<MaterialTreeData>(new RectangleF(), metrics.MaximumLevel);

                var dir = Path.Combine(Path.GetDirectoryName(sourcePathFromDisk), "Layers");
                for (int layerIndex = 0; layerIndex < asset.Layers.Count; layerIndex++)
                {
                    var fileName = Path.Combine(dir, asset.Layers[layerIndex].Opacity);

                    using (var map = new ImageSource(resamplerContext.GraphicsDevice, fileName))
                    using (var builder = new MaterialTreeBuilder(resamplerContext, PixelFormat.R8_UNorm, PixelFormat.R8_UNorm, metrics, tree))
                    {
                        map.Metrics = metrics;
                        //builder.OpacityThreshold = 0.5f;
                        builder.LayerName = fileName;
                        builder.GenerateImage = (position, target) => map.TryGenerate(resamplerContext, position, target);

                        builder.SaveImage = (image, level, x, y) =>
                        {
                            var pixelBuffer = image.PixelBuffer[0];
                            int index = QuadTreeHelper.GetNodeIndex(metrics.MaximumLevel - level, new Int2(x, y));

                            var start = (int)tempStream.Position;
                            tempWriter.Serialize(pixelBuffer.DataPointer, pixelBuffer.BufferStride);
                            tempOffsets[index, 2 + layerIndex] = new DataRange(start, (int)tempStream.Position - start);
                        };

                        builder.Build(); 
                    }
                }

                using (var tileStream = AssetManager.FileProvider.OpenStream(Url + "__TILES", VirtualFileMode.Create, VirtualFileAccess.Write))
                {
                    var tileOffsets = new DataRange[tileCount, channelCount];

                    var writer = new BinarySerializationWriter(tileStream);
                    writer.Write(tileCount);
                    writer.Write(channelCount);

                    tileStream.Seek(8 + 8 * tileCount * channelCount, SeekOrigin.Begin);

                    for (int i = 0; i < tileCount; i++)
                    {
                        for (int j = 0; j < channelCount; j++)
                        {
                            var tempOffset = tempOffsets[i, j];
                            var uncompressedSize = tempOffset.Length;

                            var start = (int)tileStream.Position;
                            if (uncompressedSize > 0)
                            {
                                using (var compressedTileStream = new LZ4Stream(tileStream, CompressionMode.Compress, false, uncompressedSize))
                                {
                                    tempStream.Position = tempOffset.Start;
                                    compressedTileStream.Write(tempStream.GetBuffer(), tempOffset.Start, uncompressedSize);
                                }
                            }
                            tileOffsets[i, j] = new DataRange(start, uncompressedSize);
                        }
                    }

                    tileStream.Seek(8, SeekOrigin.Begin);

                    for (int i = 0; i < tileCount; i++)
                    {
                        for (int j = 0; j < channelCount; j++)
                        {
                            writer.Write(tileOffsets[i, j].Start);
                            writer.Write(tileOffsets[i, j].Length);
                        }
                    }
                }
            }
        }
    }
}
