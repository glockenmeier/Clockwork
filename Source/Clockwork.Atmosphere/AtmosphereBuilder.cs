using SiliconStudio.Core;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Effects.Modules;
using SiliconStudio.Paradox.Graphics;

namespace Clockwork.Atmosphere
{
    
    public class AtmosphereBuilder : ComponentBase
    {
        private BlendState blendState;
        private Effect computeTransmittance, computeSingleIrradiance, computeMultipleIrradiance, copySingleIrradiance;
        private Effect computeSingleInscatter, computeMultipleInscatter, computeOutscatter, copySingleInscatter, copyMultipleInscatter;
        private Effect copySlice;
        private Texture2D deltaE;
        private Texture3D deltaSM, deltaSR, deltaJ;
        private RenderTarget deltaETarget, deltaSMTarget, deltaSRTarget, deltaJTarget, transmittanceTarget, irradianceTarget, inscatterTarget;
        private ParameterCollection parameters = new ParameterCollection();

        public AtmosphereData Data { get; set; }

        public AtmosphereBuilder(GraphicsDevice device, EffectSystem effectSystem)
        {
            blendState = BlendState.New(device, new BlendStateDescription(Blend.One, Blend.One)).DisposeBy(this);

            Data = new AtmosphereData(device, new AtmosphereSettings());

            // TODO: Use max precision temporary textures
/*
            var intermediateTransmittanceDesc = Data.Transmittance.Description;
            intermediateTransmittanceDesc.Format = PixelFormat.R32G32B32A32_Float;
            var intermediateIrradianceDesc = Data.Irradiance.Description;
            intermediateIrradianceDesc.Format = PixelFormat.R32G32B32A32_Float;
            var intermediateInscatterDesc = Data.Inscatter.Description;
            intermediateInscatterDesc.Format = PixelFormat.R32G32B32A32_Float;

            var intermediateTransmittance = Texture2D.New(device, intermediateTransmittanceDesc).DisposeBy(this);
            var intermediateIrradiance = Texture3D.New(device, intermediateIrradianceDesc).DisposeBy(this);
            var intermediateInscatter = Texture3D.New(device, intermediateInscatterDesc).DisposeBy(this);
 */

            transmittanceTarget = Data.Transmittance.ToRenderTarget().DisposeBy(this);
            irradianceTarget = Data.Irradiance.ToRenderTarget().DisposeBy(this);
            inscatterTarget = Data.Inscatter.ToRenderTarget(ViewType.Full, 0, 0).DisposeBy(this);

            deltaE = Texture2D.New(device, Data.Irradiance.Description).DisposeBy(this);
            deltaSM = Texture3D.New(device, Data.Inscatter.Description).DisposeBy(this);
            deltaSR = Texture3D.New(device, Data.Inscatter.Description).DisposeBy(this);
            deltaJ = Texture3D.New(device, Data.Inscatter.Description).DisposeBy(this);
            deltaETarget = deltaE.ToRenderTarget().DisposeBy(this);
            deltaSMTarget = deltaSM.ToRenderTarget(ViewType.Full, 0, 0).DisposeBy(this);
            deltaSRTarget = deltaSR.ToRenderTarget(ViewType.Full, 0, 0).DisposeBy(this);
            deltaJTarget = deltaJ.ToRenderTarget(ViewType.Full, 0, 0).DisposeBy(this);

            computeTransmittance = effectSystem.LoadEffect("ComputeTransmittance");
            computeSingleIrradiance = effectSystem.LoadEffect("SingleIrradiance");
            copySingleInscatter = effectSystem.LoadEffect("CopySingleInscatter");
            computeSingleInscatter = effectSystem.LoadEffect("SingleInscatter");
            computeOutscatter = effectSystem.LoadEffect("Outscatter");
            computeMultipleIrradiance = effectSystem.LoadEffect("MultipleIrradiance");
            computeMultipleInscatter = effectSystem.LoadEffect("MultipleInscatter");
            copyMultipleInscatter = effectSystem.LoadEffect("CopyMultipleInscatter");
            copySlice = effectSystem.LoadEffect("CopySlice");

            parameters.Set(AtmospherePrecomputationKeys.DeltaSR, deltaSR);
            parameters.Set(AtmospherePrecomputationKeys.DeltaSM, deltaSM);
            parameters.Set(AtmospherePrecomputationKeys.DeltaE, deltaE);
            parameters.Set(AtmospherePrecomputationKeys.DeltaJ, deltaJ);
        }

        public static AtmosphereData Generate(GraphicsDevice device, EffectSystem effectSystem)
        {
            if (VirtualFileSystem.ApplicationCache.FileExists("atmosphere"))
            {
                using (var stream = VirtualFileSystem.ApplicationCache.OpenStream("atmosphere", VirtualFileMode.Open, VirtualFileAccess.Read))
                {
                    return AtmosphereData.Load(device, stream);
                }
            }
            else
            {
                using (var builder = new AtmosphereBuilder(device, effectSystem))
                {
                    builder.Generate(device);

                    using (var stream = VirtualFileSystem.ApplicationCache.OpenStream("atmosphere", VirtualFileMode.Create, VirtualFileAccess.Write))
                    {
                        builder.Data.Save(stream);
                    }

                    return builder.Data;
                }
            }
        }

        public void Generate(GraphicsDevice device)
        {
            Draw(device, computeTransmittance, transmittanceTarget);

            Draw(device, computeSingleIrradiance, deltaETarget);

            DrawSlices(device, computeSingleInscatter, deltaSRTarget, deltaSMTarget);

            device.Clear(irradianceTarget, Color.Black);
            
            DrawSlices(device, copySingleInscatter, inscatterTarget);

            for (int order = 2; order <= 4; order++)
            {
                parameters.Set(AtmospherePrecomputationKeys.IsFirst, order == 2);

                DrawSlices(device, computeOutscatter, deltaJTarget);
                Draw(device, computeMultipleIrradiance, deltaETarget);
                DrawSlices(device, computeMultipleInscatter, deltaSRTarget);

                device.SetBlendState(blendState);
                {
                    device.SetRenderTarget(irradianceTarget);
                    device.DrawTexture(deltaE, device.SamplerStates.PointClamp); 
                    DrawSlices(device, copyMultipleInscatter, inscatterTarget); 
                }
                device.SetBlendState(device.BlendStates.Default);
            }
        }

        private void Draw(GraphicsDevice device, Effect effect, RenderTarget renderTarget)
        {
            device.SetRenderTarget(renderTarget);
            effect.Apply(Data.Parameters, parameters);
            device.Draw(PrimitiveType.TriangleList, 3);
        }

        private void DrawSlices(GraphicsDevice device, Effect effect, params RenderTarget[] renderTargets)
        {
            device.SetRenderTargets(renderTargets);
            parameters.Set(VolumeShaderBaseKeys.SliceCount, (uint)Data.Settings.AltitudeResolution);
            effect.Apply(Data.Parameters, parameters);
            device.DrawInstanced(PrimitiveType.TriangleList, 3, Data.Settings.AltitudeResolution);
        }

        private void Save(GraphicsDevice device, Texture2D texture, string url)
        {
            var desc = texture.Description;
            desc.Format = PixelFormat.R8G8B8A8_UNorm;
            desc.Flags |= TextureFlags.RenderTarget;

            using (var tex = Texture2D.New(device, desc))
            using (var texTarget = tex.ToRenderTarget())
            {
                //device.Clear(texTarget, Color.Black);
                device.SetRenderTarget(texTarget);
                device.DrawTexture(texture, device.SamplerStates.PointClamp);
                Save(tex, url);
            }
        }

        private void Save(Texture2D texture, string url)
        {
            VirtualFileSystem.ApplicationBinary.CreateDirectory("Precomputed");
            using (var stream = VirtualFileSystem.ApplicationBinary.OpenStream("Precomputed/" + url, VirtualFileMode.Create, VirtualFileAccess.Write))
                texture.Save(stream, ImageFileType.Png);
        }

        private void SaveInscatter(GraphicsDevice device, Texture3D inscatter, string name)
        {
            using (var compactInscatter = Texture2D.New(device, Data.Settings.SunZenithResolution * Data.Settings.ViewSunResolution, Data.Settings.ViewZenithResolution * Data.Settings.AltitudeResolution, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget | TextureFlags.ShaderResource))
            using (var compactInscatterTarget = compactInscatter.ToRenderTarget())
            {
                device.SetRenderTarget(compactInscatterTarget);
                device.Clear(compactInscatterTarget, Color.Black);

                for (int i = 0; i < Data.Settings.AltitudeResolution; i++)
                {
                    device.SetViewport(new Viewport(0, i * Data.Settings.ViewZenithResolution, Data.Settings.SunZenithResolution * Data.Settings.ViewSunResolution, Data.Settings.ViewZenithResolution));
                    copySlice.Parameters.Set(CopySliceKeys.Source, inscatter);
                    copySlice.Parameters.Set(CopySliceKeys.Slice, (i + 0.5f) / Data.Settings.AltitudeResolution);

                    copySlice.Apply();
                    device.Draw(PrimitiveType.TriangleList, 3);
                }

                Save(compactInscatter, "Compact" + name + ".png");
            }
        }

    }
}
