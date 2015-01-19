using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Graphics;

namespace Clockwork.Terrain.Compiler
{
    public class ImageTreeBuilderContext : ComponentBase
    {
        public GraphicsDevice GraphicsDevice { get; private set; }

        public PrimitiveQuad Quad { get; private set; }

        public SimpleEffect Effect { get; private set; }

        public SamplerState SamplerState { get; private set; }

        public RasterizerState ScissorTestEnabled { get; private set; }

        public ImageTreeBuilderContext()
        {
            GraphicsDevice = GraphicsDevice.New().DisposeBy(this);
            Effect = new SimpleEffect(GraphicsDevice).DisposeBy(this);
            Quad = new PrimitiveQuad(GraphicsDevice, Effect).DisposeBy(this);
            SamplerState = SamplerState.New(GraphicsDevice, new SamplerStateDescription(TextureFilter.Linear, TextureAddressMode.Border) { BorderColor = Color.Black }).DisposeBy(this);
            ScissorTestEnabled = RasterizerState.New(GraphicsDevice, new RasterizerStateDescription(CullMode.None) { ScissorTestEnable = true }).DisposeBy(this);
        }
    }
}
