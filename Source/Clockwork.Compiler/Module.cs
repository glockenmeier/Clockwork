using SiliconStudio.Core;
using SiliconStudio.Core.Reflection;

namespace Clockwork.Compiler
{
    internal class Module
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
        }
    }
}
