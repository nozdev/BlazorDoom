﻿//
// Copyright (C) 1993-1996 Id Software, Inc.
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//



using System;
using System.IO;
using ManagedDoom.Audio;
using Microsoft.JSInterop;
using SFML.Window;

namespace ManagedDoom
{
    public static class ConfigUtilities
    {
        static IJSRuntime JSRuntime;

        static string[] names = new string[]
            {
                "DOOM2.WAD",
                "PLUTONIA.WAD",
                "TNT.WAD",
                "DOOM.WAD",
                "DOOM1.WAD"
            };

        public static SfmlMusic GetSfmlMusicInstance(string soundFontUrl, IJSRuntime _JSRuntime,Config config, Wad wad)
        {
            JSRuntime = _JSRuntime;
            // if (File.Exists(sfPath))
            if (true)
            {
                Console.WriteLine("SoundFont found.");
                return new SfmlMusic(JSRuntime,config, wad, soundFontUrl);
            }
            else
            {
                Console.WriteLine("SoundFont not found. Please put TimGM6mb.sf2 in the root directory.");
                return null;
            }
        }
    }
}
