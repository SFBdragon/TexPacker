﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SDL2;
using static SDL2.SDL;

namespace TexPacker
{
	static class Packer
	{
		public unsafe static void Pack(Config config)
		{
			Console.WriteLine("Checking directories...");

			for (int i = 0; i < config.Directories.Count; i++) {
				if (!Directory.Exists(config.Directories[i]))
					throw new Exception($"Input directory: '{config.Directories[i]}' does not exist.");
			}
			if (!Directory.Exists(config.Output))
				_ = Directory.CreateDirectory(config.Output);

			// Clear out previously built atlases
			foreach (string file in Directory.GetFiles(config.Output, $"*{config.Name}*.png"))
				File.Delete(file);
			foreach (string file in Directory.GetFiles(config.Output, $"*{config}.xml"))
				File.Delete(file);

			Console.WriteLine("Loading surfaces...");

			List<(string, IntPtr)> surfaces = new List<(string, IntPtr)>();

			int err = SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG | SDL_image.IMG_InitFlags.IMG_INIT_JPG | SDL_image.IMG_InitFlags.IMG_INIT_TIF);
			if (err < 0) throw new Exception(SDL_GetError());

			foreach (string dir in config.Directories) {
				List<string> dirs = new List<string>() { dir };
				dirs.AddRange(Directory.GetDirectories(dir, "*", new EnumerationOptions() { RecurseSubdirectories = true }));

				foreach (string file in dirs.SelectMany(d => Directory.GetFiles(d, config.FileFilter))) {
					string ext = Path.GetExtension(file);
					switch (ext) {
						case ".png":
						case ".bmp":
						case ".jpeg":
						case ".jpg":
						case ".tiff":
						case ".tif":
						case ".gif":
						case ".tga":
							IntPtr image = SDL_image.IMG_Load(file);
							if (((SDL_PixelFormat*)((SDL_Surface*)image)->format)->format != SDL_PIXELFORMAT_ABGR8888)
								image = SDL_ConvertSurfaceFormat(image, SDL_PIXELFORMAT_ABGR8888, 0);
							surfaces.Add((file.Substring(dir.Length).Replace('\\', '/').TrimStart('/'), image));
							break;
						default:
							continue;
					}
				}
			}

			Console.WriteLine("Computing, blitting and saving atlases...");

			int count = 0;
			var atlas = SDL_CreateRGBSurfaceWithFormat(0, config.AtlasWidth, config.AtlasHeight, 32, SDL_PIXELFORMAT_ABGR8888);
			var sources = new List<(string id, int index, bool rot, SDL_Rect rect)>();
			var mrbp = new MaxRectsBinPack(config.AtlasWidth, config.AtlasHeight, config.AllowRotations);

			// Until the full list is emptied out, find the next best image to place
			// If the mrbp starts returning 0 rectangles, create a new atlas and continue
			while (surfaces.Count > 0) {
				int lowestScore1 = int.MaxValue;
				int lowestScore2 = int.MaxValue;
				int bestIndex = -1;
				SDL_Rect best = new SDL_Rect();

				for (int i = 0; i < surfaces.Count; ++i) {
					int score1 = 0;
					int score2 = 0;
					SDL_Rect newNode = mrbp.ScoreRect(((SDL_Surface*)surfaces[i].Item2)->w, ((SDL_Surface*)surfaces[i].Item2)->h,
						MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestShortSideFit, ref score1, ref score2);

					if (score1 < lowestScore1 || score1 == lowestScore1 && score2 < lowestScore2) {
						lowestScore1 = score1;
						lowestScore2 = score2;
						best = newNode;
						bestIndex = i;
					}
				}

				if (bestIndex == -1) {
					_ = SDL_image.IMG_SavePNG(atlas, Path.Combine(config.Output, config.Name + count.ToString() + ".png"));
					SDL_FreeSurface(atlas);
					atlas = SDL_CreateRGBSurfaceWithFormat(0, config.AtlasWidth, config.AtlasHeight, 32, SDL_PIXELFORMAT_ABGR8888);

					mrbp.Init(config.AtlasWidth, config.AtlasHeight, config.AllowRotations);
					count++;
				} else {
					var sptr = (SDL_Surface*)surfaces[bestIndex].Item2;
					bool flipped = sptr->h == best.w;
					if (flipped) {
						var flipd = (SDL_Surface*)SDL_CreateRGBSurfaceWithFormat(0, sptr->h, sptr->w, 32, SDL_PIXELFORMAT_ABGR8888);

						for (int y = 0; y < sptr->h; y++) {
							for (int x = 0; x < sptr->w; x++) {
								// swapping the coords and inverting one produces rotation
								uint* target = (uint*)(flipd->pixels + x * flipd->pitch + (sptr->h - y - 1) * 4);
								*target = *(uint*)(sptr->pixels + y * sptr->pitch + x * 4);
							}
						}

						_ = SDL_BlitSurface(new IntPtr(flipd), IntPtr.Zero, atlas, ref best);
						SDL_FreeSurface(new IntPtr(flipd));
						SDL_FreeSurface(new IntPtr(sptr));
					} else {
						_ = SDL_BlitSurface(new IntPtr(sptr), IntPtr.Zero, atlas, ref best);
						SDL_FreeSurface(new IntPtr(sptr));
					}

					sources.Add((surfaces[bestIndex].Item1, count, flipped, best));
					surfaces.RemoveAt(bestIndex);
					mrbp.PlaceRect(best);
				}
			}

			// save current/last atlas
			_ = SDL_image.IMG_SavePNG(atlas, Path.Combine(config.Output, config.Name + count.ToString() + ".png"));
			SDL_FreeSurface(atlas);

			Console.WriteLine("Writing json data to disk...");

			// Write JSON data
			File.WriteAllText(Path.Combine(config.Output, config.Name + ".json"), Newtonsoft.Json.JsonConvert.SerializeObject(sources));

			sources.Clear();
			Console.WriteLine("Done");
		}
	}
}


//Console.WriteLine("Saving XML data...");

//// Marshaling for Rectangle -> byte[] conversion
//int s = Marshal.SizeOf<Rectangle>();
//byte[] buffer = new byte[s];
//IntPtr ptr = Marshal.AllocHGlobal(s);
//// Marshal blank rect to avoid warnings later with destroying previous ptr contents
//Marshal.StructureToPtr(new Rectangle(), ptr, false);

//// Create and write an xml file containing the source rectangles

//using var fs = new FileStream(Path.Combine(destDirectory, outputName + ".xml"), FileMode.Create);
//XmlWriter writer = XmlWriter.Create(fs, new XmlWriterSettings() {
//	Encoding = Encoding.UTF8,
//	NewLineChars = "\n",
//	CloseOutput = true
//});

//writer.WriteStartDocument();
//writer.WriteStartElement("frames");

//for (int i = 0; i < sources.Count; i++) {
//	writer.WriteStartElement("frame");

//	writer.WriteStartElement("path");
//	writer.WriteValue(sources[i].id);
//	writer.WriteEndElement();

//	writer.WriteStartElement("index");
//	writer.WriteValue(sources[i].index);
//	writer.WriteEndElement();

//	writer.WriteStartElement("rot");
//	writer.WriteValue(sources[i].rot);
//	writer.WriteEndElement();

//	Marshal.StructureToPtr(sources[i].rect, ptr, true);
//	Marshal.Copy(ptr, buffer, 0, s);

//	writer.WriteStartElement("rect");
//	writer.WriteBase64(buffer, 0, s);
//	writer.WriteEndElement();

//	writer.WriteEndElement();
//}
//writer.WriteEndElement();
//writer.WriteEndDocument();
//writer.Close();

//Marshal.FreeHGlobal(ptr);