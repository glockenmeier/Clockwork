
namespace Terrain
{
    class TerrainApp
    {
        static void Main(string[] args)
        {
            // Profiler.EnableAll();
            using (var game = new TerrainGame())
            {
                game.Run();
            }
        }
    }
}
