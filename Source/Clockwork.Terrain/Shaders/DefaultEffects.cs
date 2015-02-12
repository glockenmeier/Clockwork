﻿// <auto-generated>
// Do not edit this file yourself!
//
// This code was generated by Paradox Shader Mixin Code Generator.
// To generate it yourself, please install SiliconStudio.Paradox.VisualStudio.Package .vsix
// and re-save the associated .pdxfx.
// </auto-generated>

using System;
using SiliconStudio.Core;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Shaders;
using SiliconStudio.Core.Mathematics;
using Buffer = SiliconStudio.Paradox.Graphics.Buffer;

using SiliconStudio.Paradox.Effects.Data;
namespace Clockwork.Terrain.Shaders
{
    [DataContract]public partial class TerrainParameters : ShaderMixinParameters
    {
        public static readonly ParameterKey<bool> UseInstancing = ParameterKeys.New<bool>();
        public static readonly ParameterKey<bool> UseQuadTriangulation = ParameterKeys.New<bool>(true);
    };
    internal static partial class ShaderMixins
    {
        internal partial class MorphDebugging  : IShaderMixinBuilder
        {
            public void Generate(ShaderMixinSourceTree mixin, ShaderMixinContext context)
            {
                context.CloneParentMixinToCurrent();
                context.Mixin(mixin, "MorphDebugShading");
            }

            [ModuleInitializer]
            internal static void __Initialize__()

            {
                ShaderMixinManager.Register("MorphDebugging", new MorphDebugging());
            }
        }
    }
    internal static partial class ShaderMixins
    {
        internal partial class DefaultDeferredTerrainEffect  : IShaderMixinBuilder
        {
            public void Generate(ShaderMixinSourceTree mixin, ShaderMixinContext context)
            {
                context.Mixin(mixin, "TerrainBase");
                context.Mixin(mixin, "PositionVSStream");
                context.Mixin(mixin, "TerrainNormalMap");
                context.Mixin(mixin, "SplattingEffect");
                if (context.GetParam(TerrainParameters.UseQuadTriangulation))
                    context.Mixin(mixin, "TerrainQuadTriangulation");
                if (context.GetParam(TerrainParameters.UseInstancing))
                    context.Mixin(mixin, "TerrainTileInstanced");
                else
                    context.Mixin(mixin, "TerrainTileParameter");
                context.Mixin(mixin, "ParadoxGBufferPlugin");
                context.Mixin(mixin, "LightDeferredShading");

                {
                    var __subMixin = new ShaderMixinSourceTree() { Name = "ShadowMapCaster" };
                    context.BeginChild(__subMixin);
                    context.Mixin(__subMixin, "ShadowMapCaster");
                    context.EndChild();
                }

                {
                    var __subMixin = new ShaderMixinSourceTree() { Name = "MorphDebugging" };
                    context.BeginChild(__subMixin);
                    context.Mixin(__subMixin, "MorphDebugging");
                    context.EndChild();
                }
            }

            [ModuleInitializer]
            internal static void __Initialize__()

            {
                ShaderMixinManager.Register("DefaultDeferredTerrainEffect", new DefaultDeferredTerrainEffect());
            }
        }
    }
    internal static partial class ShaderMixins
    {
        internal partial class SplattingEffect  : IShaderMixinBuilder
        {
            public void Generate(ShaderMixinSourceTree mixin, ShaderMixinContext context)
            {
                mixin.Mixin.AddMacro("SPLATTING_BLEND_COUNT", context.GetParam(SplattingParameters.MaterialCount));
                context.Mixin(mixin, "Splatting");
                context.Mixin(mixin, "AlbedoDiffuseBase");

                {
                    var __subMixin = new ShaderMixinSourceTree() { Parent = mixin };
                    context.PushComposition(mixin, "albedoDiffuse", __subMixin);
                    context.Mixin(__subMixin, "AlbedoDiffuseSplatted");
                    context.PopComposition();
                }
                context.Mixin(mixin, "AlbedoSpecularBase");

                {
                    var __subMixin = new ShaderMixinSourceTree() { Parent = mixin };
                    context.PushComposition(mixin, "albedoSpecular", __subMixin);
                    context.Mixin(__subMixin, "AlbedoSpecularSplatted");
                    context.PopComposition();
                }
                context.Mixin(mixin, "NormalMapTexture");

                {
                    var __subMixin = new ShaderMixinSourceTree() { Parent = mixin };
                    context.PushComposition(mixin, "normalMap", __subMixin);
                    context.Mixin(__subMixin, "NormalMapSplatted");
                    context.PopComposition();
                }
            }

            [ModuleInitializer]
            internal static void __Initialize__()

            {
                ShaderMixinManager.Register("SplattingEffect", new SplattingEffect());
            }
        }
    }
    internal static partial class ShaderMixins
    {
        internal partial class AlbedoDiffuseSplatted  : IShaderMixinBuilder
        {
            public void Generate(ShaderMixinSourceTree mixin, ShaderMixinContext context)
            {
                context.Mixin(mixin, "ComputeColorSplatted");
                foreach(var ____1 in context.GetParam(SplattingParameters.Materials))

                {
                    context.PushParameters(____1);
                    if (context.GetParam(MaterialParameters.AlbedoDiffuse) != null)

                        {
                            var __subMixin = new ShaderMixinSourceTree() { Parent = mixin };
                            context.PushCompositionArray(mixin, "Layers", __subMixin);
                            context.Mixin(__subMixin, context.GetParam(MaterialParameters.AlbedoDiffuse));
                            context.PopComposition();
                        }
                    else

                        {
                            var __subMixin = new ShaderMixinSourceTree() { Parent = mixin };
                            context.PushCompositionArray(mixin, "Layers", __subMixin);
                            context.Mixin(__subMixin, "ComputeColor");
                            context.PopComposition();
                        }
                    context.PopParameters();
                }
            }

            [ModuleInitializer]
            internal static void __Initialize__()

            {
                ShaderMixinManager.Register("AlbedoDiffuseSplatted", new AlbedoDiffuseSplatted());
            }
        }
    }
    internal static partial class ShaderMixins
    {
        internal partial class AlbedoSpecularSplatted  : IShaderMixinBuilder
        {
            public void Generate(ShaderMixinSourceTree mixin, ShaderMixinContext context)
            {
                context.Mixin(mixin, "ComputeColorSplatted");
                foreach(var ____1 in context.GetParam(SplattingParameters.Materials))

                {
                    context.PushParameters(____1);
                    if (context.GetParam(MaterialParameters.AlbedoSpecular) != null)

                        {
                            var __subMixin = new ShaderMixinSourceTree() { Parent = mixin };
                            context.PushCompositionArray(mixin, "Layers", __subMixin);
                            context.Mixin(__subMixin, context.GetParam(MaterialParameters.AlbedoSpecular));
                            context.PopComposition();
                        }
                    else

                        {
                            var __subMixin = new ShaderMixinSourceTree() { Parent = mixin };
                            context.PushCompositionArray(mixin, "Layers", __subMixin);
                            context.Mixin(__subMixin, "ComputeColor");
                            context.PopComposition();
                        }
                    context.PopParameters();
                }
            }

            [ModuleInitializer]
            internal static void __Initialize__()

            {
                ShaderMixinManager.Register("AlbedoSpecularSplatted", new AlbedoSpecularSplatted());
            }
        }
    }
    internal static partial class ShaderMixins
    {
        internal partial class NormalMapSplatted  : IShaderMixinBuilder
        {
            public void Generate(ShaderMixinSourceTree mixin, ShaderMixinContext context)
            {
                context.Mixin(mixin, "ComputeColorSplatted");
                foreach(var ____1 in context.GetParam(SplattingParameters.Materials))

                {
                    context.PushParameters(____1);
                    if (context.GetParam(MaterialParameters.NormalMap) != null)

                        {
                            var __subMixin = new ShaderMixinSourceTree() { Parent = mixin };
                            context.PushCompositionArray(mixin, "Layers", __subMixin);
                            context.Mixin(__subMixin, context.GetParam(MaterialParameters.NormalMap));
                            context.PopComposition();
                        }
                    else

                        {
                            var __subMixin = new ShaderMixinSourceTree() { Parent = mixin };
                            context.PushCompositionArray(mixin, "Layers", __subMixin);
                            context.Mixin(__subMixin, "ComputeColorNormalFlat");
                            context.PopComposition();
                        }
                    context.PopParameters();
                }
            }

            [ModuleInitializer]
            internal static void __Initialize__()

            {
                ShaderMixinManager.Register("NormalMapSplatted", new NormalMapSplatted());
            }
        }
    }
}
