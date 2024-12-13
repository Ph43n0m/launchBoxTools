using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace syncFavorite
{
    internal class PlatformFileHandler
    {
        internal List<GameEntry> GameEntries { get; private set; }

        internal PlatformFileHandler(String[] platformFiles, string appPath)
        {
            GameEntries = new List<GameEntry>();

            if (platformFiles != null)
            {
                foreach (String pFile in platformFiles)
                {
                    if (File.Exists(pFile))
                    {
                        // Load the XML document
                        XDocument doc = XDocument.Load(pFile);

                        // Iterate through each GameEntry element in the XML
                        foreach (var entryElement in doc.Descendants("Game"))
                        {
                            bool isFavourite = (bool)entryElement.Element("Favorite");

                            if (isFavourite)
                            {
                                // Create an GameEntry object and populate it with data from the XML
                                GameEntry gameEntry = new GameEntry
                                {
                                    ID = (string)entryElement.Element("DatabaseID"),
                                    Name = (string)entryElement.Element("Title"),
                                    Path = Path.Combine(appPath, (string)entryElement.Element("ApplicationPath")),
                                    Platform = Path.GetFileNameWithoutExtension(pFile)
                                };

                                if (File.Exists(gameEntry.Path))
                                {
                                    // Add the GameEntry object to the list
                                    GameEntries.Add(gameEntry);
                                }
                            }
                        }
                    }
                }
            }

        }

        internal List<string> GetDistinctPlatforms()
        {
            return GameEntries.Select(o => o.Platform)
                .Where(o => !string.IsNullOrEmpty(o.Trim()))
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        internal List<GameEntry> GetGamesByPlatform(String platform)
        {
            return GameEntries.FindAll(o => o.Platform.ToLower().Equals(platform.ToLower()));
        }
    }
}
