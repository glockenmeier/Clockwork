using SiliconStudio.Assets;
using SiliconStudio.Core;
using SiliconStudio.Core.IO;
using SiliconStudio.Paradox.Assets.Materials;
using System;
using System.ComponentModel;

namespace Clockwork.Terrain.Compiler
{
    [DataContract]
    public class TerrainLayer
    {
        public TerrainLayer()
        {
            Material = new AssetReference<MaterialAsset>(Guid.Empty, new UFile(""));
        }

        [DataMember(0)]
        [DefaultValue(null)]
        public UFile Opacity { get; set; }

        [DataMember(10)] 
        [DefaultValue(null)]
        public AssetReference<MaterialAsset> Material { get; set; }
    }
}
