using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Shaders;

namespace Clockwork.Terrain
{
    public class SplattingParameters
    {
        public static ParameterKey<ShaderMixinParameters[]> Materials = ParameterKeys.New<ShaderMixinParameters[]>();

        public static ParameterKey<int> MaterialCount = ParameterKeys.New<int>();
    }
}
