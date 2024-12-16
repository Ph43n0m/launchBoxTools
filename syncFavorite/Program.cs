using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine.Utility;

namespace syncFavorite
{
    internal class Program
    {
        static Arguments realArgs;
        static IniFileHandler iniHandler;
        static PlatformFileHandler platformFileHandler;
        static string appPath = Environment.CurrentDirectory;
        static string lb_platformPath;
        static bool copy = false;
        static bool delete = false;

        static void Main(string[] args)
        {
            try
            {
                if (args.Length > 0)
                {
                    if (args.Length > 0)
                    {
                        realArgs = new Arguments(args);

                        Set_local_variables_from_args();

                        if (isValidLaunchBoxPath())
                        {
                            Console.WriteLine("Start transfer...");

                            iniHandler = new IniFileHandler(Path.Combine(appPath, Const.INI_FILE_NAME));
                            iniHandler.DeleteSection(Const.INI_SECTION_STATS);

                            platformFileHandler = new PlatformFileHandler(Directory.GetFiles(lb_platformPath, "*.xml"), appPath);

                            Update_Platform_Section();

                            if (copy)
                                Copy_GameEntries_To_Target();

                            Console.WriteLine("Finish transfer...");
                        } else
                        {
                            Console.WriteLine("Invalid LaunchBox Path.");
                        }
                    }
                }
            }
            catch (Exception mex)
            {
                Console.WriteLine("MAIN ERROR: " + mex.ToString());
            }
        }

        private static void Copy_GameEntries_To_Target()
        {
            Dictionary<string, string> platformTargets = iniHandler.GetAllValues(Const.INI_SECTION_PLATFORMS);

            foreach (var target in platformTargets)
            {
                string[] multiTarget = target.Value.Split(';');

                foreach (string mTarget in multiTarget)
                {
                    if (Directory.Exists(mTarget))
                    {
                        string[] targetFiles = Directory.GetFiles(mTarget).Where(name => !name.EndsWith(".xml") && !name.EndsWith(".txt")).ToArray();
                        List<GameEntry> favEntries = platformFileHandler.GetGamesByPlatform(target.Key);

                        if (favEntries.Count > 0)
                        {
                            foreach (GameEntry game in favEntries)
                            {
                                string targetPath = Path.Combine(mTarget, Path.GetFileName(game.Path));
                                if (!File.Exists(targetPath))
                                {
                                    Console.WriteLine(string.Format("Copy game \"{0}\" to target: \"{1}\"", game.Name, targetPath));
                                    File.Copy(game.Path, targetPath);
                                }
                            }

                            if (delete)
                            {
                                foreach (string tf in targetFiles)
                                {
                                    if (null == favEntries.FirstOrDefault(o => Path.GetFileName(o.Path).ToLower().Equals(Path.GetFileName(tf).ToLower())))
                                    {
                                        Console.WriteLine(string.Format("Deleting file \"{0}\"", tf));
                                        File.Delete(tf);

                                    }
                                }
                            }
                        }
                        else
                        {
                            if (delete)
                            {
                                foreach (string tf in targetFiles)
                                {
                                    Console.WriteLine(string.Format("Deleting file \"{0}\"", tf));
                                    File.Delete(tf);
                                }
                            }
                        }


                    }
                    else
                    {
                        Console.WriteLine(string.Format("Target path \"{0}\" for \"{1}\" does not exists.", target.Key, mTarget));
                    }
                }
            }
        }

        private static void Update_Platform_Section()
        {
            foreach (string platform in platformFileHandler.GetDistinctPlatforms())
            {
                string readPlatform = iniHandler.ReadValue(Const.INI_SECTION_PLATFORMS, platform);
                if (string.IsNullOrEmpty(readPlatform))
                {
                    Console.WriteLine("Adding new Platform: " + platform);
                    iniHandler.WriteValue(Const.INI_SECTION_PLATFORMS, platform, "<TargetPath>");
                }

                iniHandler.WriteValue(Const.INI_SECTION_STATS, platform + "_count", platformFileHandler.GetGamesByPlatform(platform).Count.ToString());
            }
        }

        private static void Set_local_variables_from_args()
        {
            if (null != realArgs["version"])
            {
                Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Version.ToString());
                return;
            }

            if (String.IsNullOrEmpty(realArgs["path"]) | !Directory.Exists(realArgs["path"]))
            {
                appPath = Environment.CurrentDirectory;
            }
            else
            {
                appPath = realArgs["path"];
            }

            copy = (null != realArgs["c"] || null != realArgs["cp"] || null != realArgs["copy"]);
            delete = (null != realArgs["d"] || null != realArgs["del"] || null != realArgs["delete"]);

        }

        private static bool isValidLaunchBoxPath()
        {
            bool ret = false;

            try
            {
                lb_platformPath = Path.Combine(appPath, Const.LAUNCHBOX_PLATFORM_PATH);

                ret = Directory.Exists(lb_platformPath);
            }
            catch (Exception ee)
            {
                throw ee;
            }

            return ret;
        }
    }
}
