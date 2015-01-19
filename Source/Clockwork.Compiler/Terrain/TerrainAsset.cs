using SiliconStudio.Assets;
using SiliconStudio.Assets.Compiler;
using SiliconStudio.Core;
using SiliconStudio.Core.IO;
using SiliconStudio.Paradox.Graphics;
using System.Collections.Generic;
using System.ComponentModel;

namespace Clockwork.Terrain.Compiler
{
    [DataContract("Terrain")]
    [AssetFileExtension(FileExtension)]
    [AssetCompiler(typeof(TerrainAssetCompiler))]
    [AssetFactory(typeof(TerrainAsset.TerrainFactory))]
    [AssetDescription("Terrain ", "A terrain definition")]
    public class TerrainAsset : Asset
    {
        private class TerrainFactory : IAssetFactory
        {
            public Asset New()
            {
                return new TerrainAsset();
            }
        }

        public const string FileExtension = ".terrain";

        [DataMember(0), DefaultValue(null)]
        public UFile Source { get; set; }

        // 64.0f / 29; // 4096.0f units / 29 vertices  * (2 meters / 128 units)
        [DataMember(10), DefaultValue(2.0f)]
        public float VertexSpacing { get; set; }

        // (0xFFFF) / 8 = (0xFFFF) * 8 units * (2 meters / 128 units)
        [DataMember(20), DefaultValue(8192.0f)]
        public float HeightScale { get; set; }

        [DataMember(30), DefaultValue(43)]
        public int VerticesPerPatch { get; set; }

        [DataMember(40), DefaultValue(1)]
        public int VertexOverlap { get; set; }

        [DataMember(50), DefaultValue(PixelFormat.R16_UNorm)]
        public PixelFormat HeightMapFormat { get; set; }

        [DataMember(60)]
        public List<TerrainLayer> Layers { get; set; }

        public TerrainAsset()
        {
            SetDefaults();
        }

        public override void SetDefaults()
        {
            VertexSpacing = 2.0f;
            HeightScale = 8192.0f;
            VerticesPerPatch = 43;
            VertexOverlap = 1;
            HeightMapFormat = PixelFormat.R16_UNorm;
            Layers = new List<TerrainLayer>();
        }
    }
}
