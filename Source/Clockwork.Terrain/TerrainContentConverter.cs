using SiliconStudio.Core;
using SiliconStudio.Core.Serialization.Converters;
using SiliconStudio.Paradox.Effects;
using System;

namespace Clockwork.Terrain
{
    public class TerrainContentConverter : DataConverter<TerrainDescription, TerrainContent>
    {
        public override void ConvertFromData(ConverterContext converterContext, TerrainDescription data, ref TerrainContent obj)
        {
            // TODO: Handle correctly
            foreach (var material in data.Materials)
            {
                Material source = null;
                converterContext.ConvertFromData(material, ref source);
                material.Value = source;
            }

            ServiceRegistry serviceRegistry = converterContext.Tags.Get<ServiceRegistry>(ServiceRegistry.ServiceRegistryKey);
            obj = new TerrainContent(serviceRegistry, data, 512);
        }

        public override void ConvertToData(ConverterContext converterContext, ref TerrainDescription data, TerrainContent obj)
        {
            throw new NotSupportedException();
        }

        [ModuleInitializer]
        internal static void Initialize()
        {
            ConverterContext.RegisterConverter<TerrainDescription, TerrainContent>(new TerrainContentConverter());
        }
    }
}
