using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using CommandLine.Utility;
using System.Threading;

namespace LaunchBoxCleanUp
{
    internal class Program
    {
        static AuditParser auditParser;
        static Arguments realArgs;
        static bool verbose = false;
        static int keepMaximum = 1;
        static string appPath = Environment.CurrentDirectory;
        static List<string> excludeRomFilters = new List<string>();
        static List<string> favouriteRomFilter = new List<string>();

        static void Main(string[] args)
        {
            try
            {
                if (args.Length > 0)
                {
                    realArgs = new Arguments(args);
                    auditParser = new AuditParser();

                    Set_local_variables_from_args();

                    if (auditParser.ReadData())
                    {
                        if (null != realArgs["promptFilterOptions"])
                        {
                            Console.WriteLine("FilterOptions:\n");
                            foreach (string option in auditParser.GetFilterOptions())
                            {
                                Console.WriteLine(option);
                            }
                            return;
                        }

                        if (excludeRomFilters.Count > 0 || favouriteRomFilter.Count > 0)
                        {
                            Console.WriteLine(string.Format("Starting with {0} read games from the audit log file.\n", auditParser.GameEntries.Count));

                            foreach (string gameID in auditParser.GetDistinctIDs())
                            {
                                List<GameEntry> gamesById = auditParser.GetGamesById(gameID);

                                if (gamesById.Count > 0)
                                {
                                    Console.WriteLine(string.Format("\nProcessing {0} entries: {1}\n", gamesById.Count, gamesById[0].ToString()));

                                    Remove_roms_by_non_existing_file(gamesById);
                                    Remove_roms_by_exclude_filter(gamesById);

                                    if (favouriteRomFilter.Count > 0)
                                    {
                                        Remove_roms_by_not_in_favourite_filter(gamesById);
                                    }

                                    Console.WriteLine(string.Format("\nFinally keep {0} entries.\n", auditParser.GetGamesById(gameID).Count));

                                }
                            }

                            Console.WriteLine(string.Format("Finish with {0} games.", auditParser.GameEntries.Count));
                        }
                        else
                        {
                            Console.WriteLine("add some exclude or favorite rom filter");
                        }
                    }
                    else
                    {
                        Console.WriteLine(String.Join(Environment.NewLine, auditParser.ErrorMessages));
                    }

                }
            }
            catch (Exception mex)
            {
                Console.WriteLine("MAIN ERROR: " + mex.ToString());
            }
        }

        private static void Set_local_variables_from_args()
        {
            if (null != realArgs["version"])
            {
                Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Version.ToString());
                return;
            }

            verbose = (null != realArgs["v"] || null != realArgs["verbose"]);

            if (String.IsNullOrEmpty(realArgs["filepath"]) | !System.IO.File.Exists(realArgs["filepath"]))
            {
                auditParser.FileName = Path.Combine(Environment.CurrentDirectory, Const.DEFAULT_LAUNCHBOX_AUDIT_FILE_NAME);
            }
            else
            {
                auditParser.FileName = realArgs["filepath"];
                appPath = Path.GetDirectoryName(realArgs["filepath"]);
            }

            if (!String.IsNullOrEmpty(realArgs["delimeter"]))
            {
                auditParser.Delimeter = new String[] { realArgs["delimeter"].Replace("\\t", "\t") };
            }

            if (!String.IsNullOrEmpty(realArgs["keepMaximum"]))
            {
                Int32.TryParse(realArgs["keepMaximum"], out keepMaximum);
            }

            if (!String.IsNullOrEmpty(realArgs["excludeRomFilters"]))
            {
                excludeRomFilters = realArgs["excludeRomFilters"]
                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => Regex.Replace(x, @"[\(\)]", "").Trim().ToLower())
                    .Where(o => !string.IsNullOrEmpty(o.Trim()))
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();
            }

            if (!String.IsNullOrEmpty(realArgs["favouriteRomFilter"]))
            {
                favouriteRomFilter = realArgs["favouriteRomFilter"]
                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => Regex.Replace(x, @"[\(\)]", "").Trim().ToLower())
                    .Where(o => !string.IsNullOrEmpty(o.Trim()))
                    .Distinct()
                    .ToList();
            }
        }

        private static void Remove_roms_by_non_existing_file(List<GameEntry> gamesById)
        {
            if (null != gamesById && gamesById.Count > 0)
            {
                for (int i = gamesById.Count - (1); i >= 0; i--)
                {
                    string romFile = Path.Combine(appPath, gamesById[i].RomPath);
                    bool romExists = File.Exists(romFile);

                    string outPut = string.Empty;
                    if (!romExists)
                    {
                        outPut += string.Format("Rom File: {0}\n", romFile);
                        outPut += string.Format("File exists: {0}\n", romExists);

                        bool del = Remove_game_from_main_list(gamesById[i]);
                        if (del)
                        {
                            gamesById.Remove(gamesById[i]);
                            outPut += string.Format("removed from gamelist: {0}\n", del);
                        }
                        Console.WriteLine(outPut.Trim());
                    }
                }

            }
        }

        private static void Remove_roms_by_exclude_filter(List<GameEntry> gamesById)
        {
            if (null != gamesById && gamesById.Count > 0 && excludeRomFilters.Count > 0)
            {
                foreach (string filter in excludeRomFilters)
                {
                    GameEntry toCheck = GetGameEntry_with_matching_filter(gamesById, filter);

                    while (null != toCheck)
                    {
                        bool del = Remove_game_from_main_list_and_delete_rom(toCheck, filter);
                        if (del)
                            gamesById.Remove(toCheck);

                        toCheck = GetGameEntry_with_matching_filter(gamesById, filter);
                    }

                }

            }
        }

        private static void Remove_roms_by_not_in_favourite_filter(List<GameEntry> gamesById)
        {
            if (null != gamesById && gamesById.Count > 0 && favouriteRomFilter.Count > 0)
            {
                List<GameEntry> keepGames = new List<GameEntry>();

                string outPut = string.Empty;

                if (gamesById.Count <= keepMaximum)
                {
                    keepGames.AddRange(gamesById);
                    gamesById.Clear();

                    outPut += string.Format("Keep maximum ({0}) covering {1} copies of the game.\n", keepMaximum, keepGames.Count);
                }
                else
                {
                    foreach (string filter in favouriteRomFilter)
                    {
                        if (keepGames.Count < keepMaximum)
                        {
                            GameEntry gameEntry = GetGameEntry_with_matching_filter(gamesById, filter);
                            if (null != gameEntry)
                            {
                                keepGames.Add(gameEntry);
                                gamesById.Remove(gameEntry);

                                outPut += string.Format("Keep {0}\n", gameEntry.ToString());
                                outPut += string.Format("Rom file: {0}\n", Path.GetFileName(gameEntry.RomPath));
                                outPut += string.Format("based on matching favourite filter: {0}\n", filter);

                            }

                        }
                        else
                            break;
                    }

                    while (keepGames.Count < keepMaximum && gamesById.Count > 0)
                    {
                        keepGames.Add(gamesById[0]);
                        gamesById.Remove(gamesById[0]);

                        outPut += string.Format("Keep {0}\n", gamesById[0].ToString());
                        outPut += string.Format("Rom file: {0}\n", Path.GetFileName(gamesById[0].RomPath));
                        outPut += string.Format("because keep maximum ({0}) not reached\n", keepMaximum);
                    }

                }

                Console.WriteLine(outPut.Trim());

                for (int i = gamesById.Count - (1); i >= 0; i--)
                {
                    Remove_game_from_main_list_and_delete_rom(gamesById[i]);
                }

            }
        }

        private static bool Remove_game_from_main_list_and_delete_rom(GameEntry removalGameEntry, string filter = null)
        {
            bool ret = false;

            try
            {
                string outPut = string.Format("Removing {0}\n", removalGameEntry.ToString());

                if (!string.IsNullOrEmpty(filter))
                {
                    outPut += string.Format("based on matching exclude filter: {0}\n", filter);
                }

                bool del = Delete_rom_file(removalGameEntry);

                Console.WriteLine(outPut.Trim());

                ret = Remove_game_from_main_list(removalGameEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not remove: ", removalGameEntry.ToString(), ex);
            }
            return ret;
        }

        private static GameEntry GetGameEntry_with_matching_filter(List<GameEntry> gamesById, string filter)
        {
            return gamesById.FirstOrDefault(o => o.FilterOptions.Contains(filter));
        }

        private static bool Remove_game_from_main_list(GameEntry removalGameEntry)
        {
            return auditParser.GameEntries.Remove(removalGameEntry);
        }

        private static bool Delete_rom_file(GameEntry removalGameEntry)
        {
            bool ret = false;

            try
            {
                string romFile = Path.Combine(appPath, removalGameEntry.RomPath);
                bool romExists = File.Exists(romFile);

                string outPut = string.Format("Rom file: {0}\n", romFile);
                outPut += string.Format("File exists: {0}\n", romExists);

                bool del = false;

                if (!verbose && romExists)
                {
                    del = TryDeleteFile(romFile);
                    outPut += string.Format("erased from disc: {0}\n", del);
                    if (!del)
                        return false;
                }

                Console.WriteLine(outPut.Trim());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not remove: ", removalGameEntry.ToString(), ex);
            }
            return ret;
        }

        private static bool TryDeleteFile(string file, int retrys = 10, int delay = 100)
        {
            bool ret = false;
            if (!string.IsNullOrEmpty(file))
            {
                try
                {
                    int retry = 0;

                    try
                    {
                        if (File.Exists(file))
                            File.SetAttributes(file, FileAttributes.Normal);
                        else
                            ret = true;
                    }
                    catch { }

                    while (File.Exists(file) && retry <= retrys)
                    {
                        try
                        {
                            File.Delete(file);
                            ret = true;
                        }
                        catch
                        {
                            Thread.Sleep(delay);
                            retry++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(string.Format("Error trying to delete file: {0}", file), ex);
                }
            }
            return ret;
        }

    }
}