﻿using SiliconStudio.Core;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Effects.Modules;
using SiliconStudio.Paradox.Graphics;
using System.IO;

namespace Clockwork.Atmosphere
{
    public class AtmosphereData : ComponentBase
    {
        public AtmosphereSettings Settings { get; private set; }

        public Texture2D Transmittance { get; private set; }

        public Texture2D Irradiance { get; private set; }

        public Texture3D Inscatter { get; private set; }

        public ParameterCollection Parameters { get; private set; }

        public AtmosphereData(AtmosphereSettings settings, Texture2D transmittance, Texture2D irradiance, Texture3D inscatter)
        {
            Settings = settings;
            Transmittance = transmittance;
            Irradiance = irradiance;
            Inscatter = inscatter;

            Initialize();
        }

        public AtmosphereData(GraphicsDevice device, AtmosphereSettings settings)
        {
            Settings = settings;

            Transmittance = Texture2D.New(device, settings.TransmittanceSize.Width, settings.TransmittanceSize.Height,
                PixelFormat.R11G11B10_Float, TextureFlags.ShaderResource | TextureFlags.RenderTarget).DisposeBy(this);

            Inscatter = Texture3D.New(device, settings.SunZenithResolution * settings.ViewSunResolution,
                settings.ViewZenithResolution, settings.AltitudeResolution, PixelFormat.R32G32B32A32_Float, //R16G16B16A16_Float,
                TextureFlags.ShaderResource | TextureFlags.RenderTarget).DisposeBy(this);

            Irradiance = Texture2D.New(device, settings.SkySize.Width, settings.SkySize.Height,
                PixelFormat.R11G11B10_Float, TextureFlags.ShaderResource | TextureFlags.RenderTarget).DisposeBy(this);

            Initialize();
        }

        public static AtmosphereData Load(GraphicsDevice device, Stream stream)
        {
            var reader = new BinarySerializationReader(stream);
            var settings = reader.Read<AtmosphereSettings>();

            long transmittanceSize = reader.ReadInt64();
            long irradianceSize = reader.ReadInt64();
            long inscatterSize = reader.ReadInt64();

            Texture2D transmittance, irradiance;
            Texture3D inscatter;

            var buffer = reader.ReadBytes((int)transmittanceSize);
            using (var data = new MemoryStream(buffer))
                transmittance = Texture2D.Load(device, data);

            buffer = reader.ReadBytes((int)irradianceSize);
            using (var data = new MemoryStream(buffer))
                irradiance = Texture2D.Load(device, data);

            buffer = reader.ReadBytes((int)inscatterSize);
            using (var data = new MemoryStream(buffer))
                inscatter = Texture3D.Load(device, data);

            return new AtmosphereData(settings, transmittance, irradiance, inscatter);
        }

        public void Save(Stream stream)
        {
            long transmittanceSize, irradianceSize, inscatterSize;
            using (var data = new MemoryStream())
            {
                Transmittance.Save(data, ImageFileType.Paradox);
                transmittanceSize = data.Position;

                Irradiance.Save(data, ImageFileType.Paradox);
                irradianceSize = data.Position - transmittanceSize;

                Inscatter.Save(data, ImageFileType.Paradox);
                inscatterSize = data.Position - transmittanceSize - irradianceSize;

                var writer = new BinarySerializationWriter(stream);
                writer.Write(Settings);
                writer.Write(transmittanceSize);
                writer.Write(irradianceSize);
                writer.Write(inscatterSize);
                data.WriteTo(stream);
            }
        }

        private void Initialize()
        {
            Parameters = new ParameterCollection();

            Parameters.Set(AtmosphereKeys.Transmittance, Transmittance);
            Parameters.Set(AtmosphereKeys.Inscatter, Inscatter);
            Parameters.Set(AtmosphereKeys.Irradiance, Irradiance);

            Parameters.Set(AtmosphereKeys.GroundHeight, Settings.GroundHeight);
            Parameters.Set(AtmosphereKeys.TopHeight, Settings.TopHeight);
            Parameters.Set(AtmosphereKeys.HeightLimit, Settings.HeightLimit);

            Parameters.Set(AtmosphereKeys.SunZenithResolution, Settings.SunZenithResolution);
            Parameters.Set(AtmosphereKeys.ViewZenithResolution, Settings.ViewZenithResolution);
            Parameters.Set(AtmosphereKeys.AltitudeResolution, Settings.AltitudeResolution);
            Parameters.Set(AtmosphereKeys.ViewSunResolution, Settings.ViewSunResolution);
        }
    }
}
