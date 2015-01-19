using SiliconStudio.Core;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Shaders;
using SiliconStudio.Paradox.Shaders.Compiler;

namespace Clockwork.Terrain
{
    public class TerrainRenderer : Renderer
    {
        public static readonly ParameterKey<TerrainTileInfo> TileInfo = ParameterKeys.New(default(TerrainTileInfo));

        private Effect effect;
        private TerrainModel terrain;

        public TerrainRenderer(IServiceRegistry services, string effectName, TerrainModel terrain)
            : base(services)
        {
            this.terrain = terrain;

            int maxBlendCount = terrain.Content.Description.Materials.Count;
            var materialParams = new ShaderMixinParameters[maxBlendCount];
            var effectParams = new ParameterCollection();

            for (int i = 0; i < maxBlendCount; i++)
            {
                var param = new ShaderMixinParameters();

                foreach (var parameter in terrain.Content.Description.Materials[i].Value.Parameters)
                {
                    if (parameter.Key.PropertyType == typeof(Texture) ||
                        parameter.Key.PropertyType == typeof(SamplerState))
                    {
                        effectParams.SetObject(parameter.Key, parameter.Value);
                    }
                    else
                    {
                        param.SetObject(parameter.Key, parameter.Value);
                    }
                }

                materialParams[i] = param;
            }

            var compilerParameters = new CompilerParameters();
            compilerParameters.Set(SplattingParameters.MaterialCount, maxBlendCount);
            compilerParameters.Set(SplattingParameters.Materials, materialParams);

            effect = EffectSystem.LoadEffect(effectName, compilerParameters);
            effectParams.CopyTo(effect.Parameters);
        }

        public override void Load()
        {
            Pass.StartPass += Render;
        }

        private void Render(RenderContext renderContext)
        {
            if (terrain == null)
                return;

            terrain.Draw(renderContext, effect);
        }

        public override void Unload()
        {
            Pass.StartPass -= Render;
        }
    }
}
