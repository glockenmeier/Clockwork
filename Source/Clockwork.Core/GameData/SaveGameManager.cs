using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SiliconStudio.Core.Collections;
using SiliconStudio.Core.IO;

namespace Clockwork.Data
{
    public class SaveGame
    {

    }

    public class SaveGameManager
    {
        private string SaveGamePath = "/roaming/savegames/";

        public TrackingDictionary<string, SaveGame> SaveGames { get; private set; }

        public SaveGameManager()
        {
            SaveGames = new TrackingDictionary<string, SaveGame>();
            var directoryWatcher = new DirectoryWatcher("*.save");

            UpdateSaveGames();
            directoryWatcher.Track(SaveGamePath);
            directoryWatcher.Modified += directoryWatcher_Modified;
            //VirtualFileSystem.ApplicationRoaming.
        }

        public Stream Create(string name)
        {
            VirtualFileSystem.CreateDirectory(SaveGamePath);
            return VirtualFileSystem.OpenStream(SaveGamePath + name + ".save", VirtualFileMode.Create, VirtualFileAccess.Write);
        }

        public Stream Open(string name)
        {
            return VirtualFileSystem.OpenStream(SaveGamePath + name + ".save", VirtualFileMode.Open, VirtualFileAccess.Read);
        }

        private void directoryWatcher_Modified(object sender, FileEvent e)
        {
            UpdateSaveGames();
        }

        private async void UpdateSaveGames()
        {
            SaveGames.Clear();

            var files = await VirtualFileSystem.ListFiles(SaveGamePath, "*.save", VirtualSearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                SaveGame saveGame;
                if (!SaveGames.TryGetValue(file, out saveGame))
                {
                    saveGame = new SaveGame();
                    SaveGames.Add(file, saveGame);
                }
            }
        }
    }
}
