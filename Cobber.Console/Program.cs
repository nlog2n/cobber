using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Cobber.Core;
using Cobber.Core.Project;

// Usage: 
// Cobber.Console.exe  -help
// Cobber.Console.exe -project <configuration file> 
// Cobber.Console.exe -preset <preset> -snkey <strong name key> -output <output directory> -input <input files>

// Allow input assemblies to be specified using wildcards e.g. -input C:\my_dlls\*.dll
// Any assemblies added using a wildcard are assumed not to be a main assembly (i.e. IsMain is set to false).

namespace Cobber.Console
{
    class Program
    {
        #region logger callback
        static void BeginAssembly(object sender, AssemblyEventArgs e)
        {
            WriteLineWithColor(ConsoleColor.Blue, string.Format("Processing '{0}'...", e.Assembly.Name));
        }
        static void EndAssembly(object sender, AssemblyEventArgs e)
        {
            //
        }
        static void Phase(object sender, LogEventArgs e)
        {
            WriteLineWithColor(ConsoleColor.Blue, e.Message);
        }
        static void Logging(object sender, LogEventArgs e)
        {
            WriteLine(e.Message);
        }
        static void Warning(object sender, LogEventArgs e)
        {
            WriteLineWithColor(ConsoleColor.Yellow, e.Message);
        }
        static void Progressing(object sender, ProgressEventArgs e)
        {
            //
        }
        static void Error(object sender, ExceptionEventArgs e)
        {
            WriteLineWithColor(ConsoleColor.Red, new string('*', 15));
            WriteLineWithColor(ConsoleColor.Red, "ERROR!!");
            WriteLineWithColor(ConsoleColor.Red, e.Exception.Message);
            WriteLineWithColor(ConsoleColor.Red, e.Exception.StackTrace);
            WriteLineWithColor(ConsoleColor.Red, new string('*', 15));
        }
        static void Finish(object sender, LogEventArgs e)
        {
            WriteLineWithColor(ConsoleColor.Green, new string('*', 15));
            WriteLineWithColor(ConsoleColor.Green, "SUCCESSED!!");
            WriteLineWithColor(ConsoleColor.Green, e.Message);
            WriteLineWithColor(ConsoleColor.Green, new string('*', 15));
        }

        static void WriteLineWithColor(ConsoleColor color, string txt)
        {
            ConsoleColor clr = System.Console.ForegroundColor;
            System.Console.ForegroundColor = color;
            System.Console.WriteLine(txt);
            System.Console.ForegroundColor = clr;
        }
        static void WriteLine(string txt)
        {
            System.Console.WriteLine(txt);
        }
        static void WriteLine()
        {
            System.Console.WriteLine();
        }
        #endregion

        static int ParseCommandLine(string[] args, out CobberProject proj)
        {
            proj = new CobberProject();
            for (int i = 0; i < args.Length; i++)
            {
                string action = args[i].ToLower();
                if (!action.StartsWith("-") || i + 1 >= args.Length)
                {
                    WriteLineWithColor(ConsoleColor.Red, string.Format("Error: invalid argument {0}!", action));
                    return 3;
                }
                action = action.Substring(1).ToLower();
                switch (action)
                {
                    case "project":
                        {
                            if (!File.Exists(args[i + 1]))
                            {
                                WriteLineWithColor(ConsoleColor.Red, string.Format("Error: File '{0}' not exist!", args[i + 1]));
                                return 2;
                            }
                            proj.Load(args[i + 1]); // load from xml file
                            //proj.BasePath = Path.GetDirectoryName(args[i + 1]); // also assume project folder is base folder

                            i += 1;
                        } break;
                    case "preset":
                        {
                            try
                            {
                                Preset preset  = (Preset)Enum.Parse(typeof(Preset), args[i + 1], true);
                                proj.ObfSettingName = preset.ToString().ToLower();
                                i += 1;
                            }
                            catch
                            {
                                WriteLineWithColor(ConsoleColor.Red, string.Format("Error: Invalid preset '{0}'!", args[i + 1]));
                                return 3;
                            }
                        } break;

                    case "input":
                        {
                            int parameterCounter = i + 1;

                            for (int j = i + 1; j < args.Length && !args[j].StartsWith("-"); j++)
                            {
                                parameterCounter = j;
                                string inputParameter = args[j];

                                int lastBackslashPosition = inputParameter.LastIndexOf('\\') + 1;
                                string filename = inputParameter.Substring(lastBackslashPosition, inputParameter.Length - lastBackslashPosition);
                                string path = inputParameter.Substring(0, lastBackslashPosition);

                                try
                                {
                                    string[] fileList = Directory.GetFiles(path, filename);
                                    if (fileList.Length == 0)
                                    {
                                        WriteLineWithColor(ConsoleColor.Red, string.Format("Error: No files matching '{0}' in directory '{1}'!", filename));
                                        return 2;
                                    }
                                    else if (fileList.Length == 1)
                                    {
                                        proj.Assemblies.Add(new CobberAssembly()
                                        {
                                            Name = fileList[0], // path
                                            IsMain = j == i + 1 && filename.Contains('?') == false && filename.Contains('*') == false
                                        });
                                    }
                                    else
                                    {
                                        foreach (string expandedFilename in fileList)
                                        {
                                            proj.Assemblies.Add(new CobberAssembly() { Name = expandedFilename, IsMain = false });
                                        }
                                    }
                                }
                                catch (DirectoryNotFoundException)
                                {
                                    WriteLineWithColor(ConsoleColor.Red, string.Format("Error: Directory '{0}' does not exist!", path));
                                    return 2;
                                }
                            }
                            i = parameterCounter;
                        } break; 

                    case "output":
                        {
                            if (!Directory.Exists(args[i + 1]))
                            {
                                WriteLineWithColor(ConsoleColor.Red, string.Format("Error: Directory '{0}' not exist!", args[i + 1]));
                                return 2;
                            }
                            proj.OutputPath = args[i + 1];
                            i += 1;
                        } break;
                    case "snkey":
                        {
                            if (!File.Exists(args[i + 1]))
                            {
                                WriteLineWithColor(ConsoleColor.Red, string.Format("Error: File '{0}' not exist!", args[i + 1]));
                                return 2;
                            }
                            proj.SNKeyPath = args[i + 1];
                            i += 1;
                        } break;
                }
            }

            if (proj.Assemblies.Count == 0) 
            {
                WriteLineWithColor(ConsoleColor.Red, "Error: no assemblies added!");
                return 4;
            }

            return 0;
        } 

        static int Main(string[] args)
        {
            try
            {
                if (args.Length < 2 || args[0] == "-help")
                {
                    WriteLine("Cobber Version v" + typeof(Core.Cobber).Assembly.GetName().Version);
                    WriteLine("Usage: Cobber.Console.exe -project <configuration file>");
                    WriteLine("       Cobber.Console.exe -preset <preset> -snkey <strong name key> -output <output directory> -input <input files>");
                    return 1;
                }

                Core.Cobber cr = new Core.Cobber();
                {
                    cr.Logger.BeginAssembly += BeginAssembly;
                    cr.Logger.EndAssembly += EndAssembly;
                    cr.Logger.Phase += Phase;
                    cr.Logger.Log += Logging;
                    cr.Logger.Warn += Warning;
                    cr.Logger.Progress += Progressing;
                    cr.Logger.Error += Error;
                    cr.Logger.Finish += Finish;
                }

                CobberProject proj;
                int error = ParseCommandLine(args, out proj);
                if (error != 0) return error;

                cr.Process(proj);
                return 0;
            }
            catch (Exception ex)
            {
                WriteLine("Error: " + ex.Message);
                return -1;
            }
        }
    }
}