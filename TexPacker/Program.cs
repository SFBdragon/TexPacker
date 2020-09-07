
/*
  The MIT License (MIT)
  Copyright © 2020 Shaun Beautement

  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
  documentation files (the “Software”), to deal in the Software without restriction, including without 
  limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of 
  the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

  The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

  THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
  THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
  TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using SDL2;
using Newtonsoft.Json;

namespace TexPacker
{
	class Program
	{
		static void Main()
		{
			Console.Title = "TexPacker";
			string path = SDL.SDL_GetBasePath();
			string cfg = Path.Combine(path, ConfigName);
			
			Console.WriteLine("--- Welcome to TexPacker ---");
			Console.WriteLine("Supports .png, .jpg, .tif, .bmp, .tga, and .gif; atlases formatted as .png.");

			List<Config> configs;
			if (File.Exists(Path.Combine(path, ConfigName))) {
				Console.WriteLine("Config file found, loading...");
				configs = JsonConvert.DeserializeObject<List<Config>>(File.ReadAllText(cfg));
			} else {
				Console.WriteLine("Config file not found, creating...");
				configs = new List<Config>() {
					new Config() {
						Name = "Atlas",
						IncludeSubDirectories = false,
						AllowRotations = true,
						AtlasWidth = 4096,
						AtlasHeight = 4096,
						Directories = { Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) },
						FileFilter = "*",
						IdName = "TestConfig",
						Output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AtlasTest"),
					}
				};
			}

			Console.WriteLine("Type 'help' to display command list...");

			int active = -1;
			bool quit = false;
			while (!quit) {
				Console.Write("> ");
				string[] command = Console.ReadLine().Trim().Split(' ').Select(str => str.Trim(' ', '[', ']', '"', '\'')).ToArray();
				if (command.Length > 0) {
					switch (command[0]) {
						case "help":
							Console.WriteLine(Help);
							break;
						case "load":
							configs = JsonConvert.DeserializeObject<List<Config>>(File.ReadAllText(cfg));
							break;
						case "save":
							File.WriteAllText(cfg, JsonConvert.SerializeObject(configs));
							break;
						case "dir":
							Console.WriteLine(cfg);
							break;
						case "quit":
							File.WriteAllText(cfg, JsonConvert.SerializeObject(configs));
							quit = true;
							break;
						case "list":
							for (int i = 0; i < configs.Count; i++)
								Console.WriteLine(i.ToString() + ": " + configs[i].IdName);
							break;
						case "peek": {
							if (command.Length > 1) {
								try {
									int index = int.Parse(command[1]);
									if (index < configs.Count)
										ListFields(configs[index]);
									else
										Console.WriteLine("Invalid index.");
#pragma warning disable CA1031
								} catch (FormatException) {
									if (configs.Any(c => c.IdName == command[1]))
										ListFields(configs.First(c => c.IdName == command[1]));
									else
										Console.WriteLine("Config does not exist with that id.");
								}
#pragma warning restore CA1031
							} else {
								Console.WriteLine("Invalid number of parameters");
							}
							break;
						}
						case "pick": {
							if (command.Length > 1) {
								try {
									int index = int.Parse(command[1]);
									if (index < configs.Count)
										active = index;
									else
										Console.WriteLine("Invalid index.");
#pragma warning disable CA1031
								} catch (FormatException) {
									if (configs.Any(c => c.IdName == command[1]))
										active = configs.FindIndex(c => c.IdName == command[1]);
									else
										Console.WriteLine("Config does not exist with that id.");
								}
#pragma warning restore CA1031
							} else {
								Console.WriteLine("Invalid number of parameters");
							}
							break;
						}
						case "add":
							if (command.Length > 1) {
								configs.Add(new Config() {
									IdName = command[1]
								});
							} else {
								configs.Add(new Config());
							}
							break;
						case "del":
							if (command.Length > 1) {
								if (configs.Any(c => c.IdName == command[1]))
									_ = configs.Remove(configs.First(c => c.IdName == command[1]));
								else
									Console.WriteLine("Config does not exist with that id.");
							} else {
								configs.Clear();
							}
							break;
						case "mod": {
							if (active == -1) {
								Console.WriteLine("Active config is not set.");
								break;
							}
							if (command.Length > 2) {
								switch (command[1]) {
									case "id":
										configs[active].IdName = string.Join(' ', command[2..^0]);
										break;
									case "name":
										configs[active].Name = string.Join(' ', command[2..^0]);
										break;
									case "+dir":
										configs[active].Directories.Add(string.Join(' ', command[2..^0]));
										break;
									case "-dir":
										_ = configs[active].Directories.Remove(string.Join(' ', command[2..^0]));
										break;
									case "subdir":
										configs[active].IncludeSubDirectories = BoolFromString(command[2]);
										break;
									case "output":
										configs[active].Output = string.Join(' ', command[2..^0]);
										break;
									case "width":
										try {
											configs[active].AtlasWidth = int.Parse(command[2]);
#pragma warning disable CA1031
										} catch (FormatException) {
											Console.WriteLine("Parse failed.");
										}
#pragma warning restore CA1031
										break;
									case "height":
										try {
											configs[active].AtlasHeight = int.Parse(command[2]);
#pragma warning disable CA1031
										} catch (FormatException) {
											Console.WriteLine("Parse failed.");
										}
#pragma warning restore CA1031
										break;
									case "rot":
										configs[active].AllowRotations = BoolFromString(command[2]);
										break;
									case "filter":
										configs[active].FileFilter = command[2];
										break;
								}
							} else {
								Console.WriteLine("Invalid number of parameters.");
							}
							break;
						}
						case "show": {
							if (command.Length > 1) {
								switch (command[1]) {
									case "id":
										Console.WriteLine(configs[active].IdName);
										break;
									case "name":
										Console.WriteLine(configs[active].Name);
										break;
									case "dirs":
										foreach (string dir in configs[active].Directories)
											Console.WriteLine(dir);
										break;
									case "subdir":
										Console.WriteLine(configs[active].IncludeSubDirectories);
										break;
									case "output":
										Console.WriteLine(configs[active].Output);
										break;
									case "width":
										Console.WriteLine(configs[active].AtlasWidth);
										break;
									case "height":
										Console.WriteLine(configs[active].AtlasHeight);
										break;
									case "rot":
										Console.WriteLine(configs[active].AllowRotations);
										break;
									case "filter":
										Console.WriteLine(configs[active].FileFilter);
										break;
								}
							}
							break;
						}
						case "pack":
							if (command.Length == 2) {
								Config config;
								try {
									config = configs[int.Parse(command[1])];
#pragma warning disable CA1031
								} catch (FormatException) {
									if (configs.Any(c => c.IdName == command[1])) {
										config = configs.First(c => c.IdName == command[1]);
									} else {
										Console.WriteLine("Config does not exist with that id.");
										break;
									}
								}
#pragma warning restore CA1031

								Packer.Pack(config);
							} else if (command.Length > 8) {
								Config config = new Config();

								try {
									if (!Directory.Exists(command[1])) throw new Exception();
									config.Directories.Add(command[1]);
									config.Output = command[2];

									config.Name = command[3];
									config.FileFilter = command[4].Length > 0 ? "*" : command[4];
									config.IncludeSubDirectories = BoolFromString(command[5]);
									config.AllowRotations = BoolFromString(command[6]);

									config.AtlasWidth = int.Parse(command[7]);
									config.AtlasHeight = int.Parse(command[8]);
#pragma warning disable CA1031
								} catch {
									Console.WriteLine("Parameters invalid.");
								}
#pragma warning restore CA1031
							} else {
								Console.WriteLine("Invalid number of parameters.");
							}
							break;
						default:
							Console.WriteLine("Command not recognised.");
							break;
					}
				}
			}
		}

		const string ConfigName = "Configs.json";
		const string Help = @"
   help - Displays this information.
   load - Loads config file from disk, overwrites memory.
   save - Saves config file from disk, overwrites file.
   dir - Prints out config filepath.
   quit - Exits the program, saves config file.

   list - Displays list of configurations' ids.
   peek [id/index] - Displays config data.
   pick [id/index] - Sets specified config as active.

   add [id] - Adds a config entry with the specified id (no spaces).
   del [id] - Removes all config entries with the specified id (blank for all).

   mod [field] [value] - Modifies active config.
      field: 'id' / 'name' / '+dir' / '-dir' / 'subdir' / 'output' / 'width' / 'height' 
      / 'rot' / 'filter'
   show [field] - Prints the specified value of the active config.
      field: 'id' / 'name' / 'dirs' / 'subdir' / 'output' / 'width' / 'height' 
      / 'rot' / 'filter'

   pack [id/index] - Creates and packs atlases of given config.
   pack [in dir] [out dir] [name] [filter (*/?)] [subdir?] [rot] [width] [height]
      - Creates and packs atlases of given info.
";

		static bool BoolFromString(string val)
		{
			val = val.ToUpper();
			return
				val == "TRUE" ||
				val == "T" ||
				val == "YES" ||
				val == "T" ||
				val == "1" ||
				val == "COPY"
				;
		}
		static void ListFields(Config cfg)
		{
			Console.WriteLine("id: " + cfg.IdName);

			for (int i = 0; i < cfg.Directories.Count; i++)
				Console.WriteLine("dir (" + i.ToString() + "): " + cfg.Directories[i]);
			Console.WriteLine("output: " + cfg.Output);

			Console.WriteLine("name: " + cfg.Name);
			Console.WriteLine("filter: " + cfg.FileFilter);
			Console.WriteLine("include subdirs: " + cfg.IncludeSubDirectories);
			Console.WriteLine("allow rotation: " + cfg.AllowRotations);
			
			Console.WriteLine("width: " + cfg.AtlasWidth);
			Console.WriteLine("height: " + cfg.AtlasHeight);
		}
	}
}
