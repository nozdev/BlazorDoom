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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using ManagedDoom.Audio;
using ManagedDoom.SoftwareRendering;
using ManagedDoom.UserInput;
using Microsoft.JSInterop;
using SFML.Graphics;
using SFML.Window;

namespace ManagedDoom
{
  public sealed class DoomApplication : IDisposable
  {
    private Config config;

    public RenderWindow window { get; internal set; }

    private CommonResource resource;
    public SfmlRenderer renderer;
    private SfmlSound sound;
    private SfmlMusic music;
    private SfmlUserInput userInput;

    private static List<DoomEvent> Events { get; } = new List<DoomEvent>();
    private List<DoomEvent> PreviousDownEvents { get; set; } = new List<DoomEvent>();

    private GameOptions options;

    private DoomMenu menu;

    private OpeningSequence opening;

    private DemoPlayback demoPlayback;

    private TicCmd[] cmds;
    private DoomGame game;

    private WipeEffect wipe;
    private bool wiping;

    private ApplicationState currentState;
    private ApplicationState nextState;
    private bool needWipe;

    private bool sendPause;

    private bool quit;
    private string quitMessage;

    public static HttpClient Http { get; internal set; }
    public static Stream WadStream { get; internal set; }

    public DoomApplication(string soundFontUrl,IJSRuntime JSRuntime, CommandLineArgs args, String[] configLines, HttpClient http, Stream wadStream,
        string wadUrl)
    {
      Http = http;
      WadStream = wadStream;
      configLines = new string[] {
                "video_screenwidth=320",
                "video_screenHeight=200",
            };
      config = new Config(configLines);

      try
      {
        config.video_screenwidth = Math.Clamp(config.video_screenwidth, 320, 3200);
        config.video_screenheight = Math.Clamp(config.video_screenheight, 200, 2000);
        var videoMode = VideoMode.CanvasMode;
        var style = Styles.Close | Styles.Titlebar;
        if (config.video_fullscreen)
        {
          style = Styles.Fullscreen;
        }
        window = new RenderWindow(videoMode, ApplicationInfo.Title, style);
        window.Clear(new Color(64, 64, 64));
        window.Display();

        if (args.deh.Present)
        {
          DeHackEd.ReadFiles(args.deh.Value);
        }

        // resource = new CommonResource(GetWadPaths(args));
        resource = new CommonResource(new string[] { wadUrl });

        renderer = new SfmlRenderer(JSRuntime,config, window, resource);

        if (!args.nosound.Present && !args.nosfx.Present)
        {
          sound = new SfmlSound(JSRuntime, config, resource.Wad);
        }

        if (!args.nosound.Present && !args.nomusic.Present)
        {
          Console.WriteLine("Getting GetSfmlMusicInstance instance");
          music = ConfigUtilities.GetSfmlMusicInstance(soundFontUrl, JSRuntime, config, resource.Wad);
        }

        userInput = new SfmlUserInput(config, window, !args.nomouse.Present);



        options = new GameOptions();
        options.GameVersion = resource.Wad.GameVersion;
        options.GameMode = resource.Wad.GameMode;
        options.MissionPack = resource.Wad.MissionPack;
        options.Renderer = renderer;
        options.Sound = sound;
        options.Music = music;
        options.UserInput = userInput;

        menu = new DoomMenu(this);

        opening = new OpeningSequence(resource, options);

        cmds = new TicCmd[Player.MaxPlayerCount];
        for (var i = 0; i < Player.MaxPlayerCount; i++)
        {
          cmds[i] = new TicCmd();
        }
        game = new DoomGame(resource, options);

        wipe = new WipeEffect(renderer.WipeBandCount, renderer.WipeHeight);
        wiping = false;

        currentState = ApplicationState.None;
        nextState = ApplicationState.Opening;
        needWipe = false;

        sendPause = false;

        quit = false;
        quitMessage = null;

        CheckGameArgs(args);
      }
      catch (Exception e)
      {
        Dispose();
        ExceptionDispatchInfo.Throw(e);
      }

    }

    private string[] GetWadPaths(CommandLineArgs args)
    {
      var wadPaths = new List<string>();

      if (args.iwad.Present)
      {
        wadPaths.Add(args.iwad.Value);
      }
      /*else
      {
          wadPaths.Add(ConfigUtilities.GetDefaultIwadPath());
      }*/
      // TODO: add wads

      if (args.file.Present)
      {
        foreach (var path in args.file.Value)
        {
          wadPaths.Add(path);
        }
      }

      return wadPaths.ToArray();
    }

    private void CheckGameArgs(CommandLineArgs args)
    {
      if (args.warp.Present)
      {
        nextState = ApplicationState.Game;
        options.Episode = args.warp.Value.Item1;
        options.Map = args.warp.Value.Item2;
        game.DeferedInitNew();
      }

      if (args.skill.Present)
      {
        options.Skill = (GameSkill)(args.skill.Value - 1);
      }

      if (args.deathmatch.Present)
      {
        options.Deathmatch = 1;
      }

      if (args.altdeath.Present)
      {
        options.Deathmatch = 2;
      }

      if (args.fast.Present)
      {
        options.FastMonsters = true;
      }

      if (args.respawn.Present)
      {
        options.RespawnMonsters = true;
      }

      if (args.nomonsters.Present)
      {
        options.NoMonsters = true;
      }

      if (args.loadgame.Present)
      {
        nextState = ApplicationState.Game;
        game.LoadGame(args.loadgame.Value);
      }

      if (args.playdemo.Present)
      {
        nextState = ApplicationState.DemoPlayback;
        demoPlayback = new DemoPlayback(resource, options, args.playdemo.Value);
      }

      if (args.timedemo.Present)
      {
        nextState = ApplicationState.DemoPlayback;
        demoPlayback = new DemoPlayback(resource, options, args.timedemo.Value);
      }
    }

    public void Run(int[] downKeys, int[] upKeys)
    {
      // Make certain situation ignore maintained keys (menu, map, etc.)
      PreviousDownEvents = Events.Where((e) => e.Type == EventType.KeyDown).ToList();
      Events.Clear();
      foreach (var upKey in upKeys)
      {
        Events.Add(new DoomEvent(EventType.KeyUp, (DoomKey)upKey));
      }
      foreach (var downKey in downKeys)
      {
        Events.Add(new DoomEvent(EventType.KeyDown, (DoomKey)downKey));
      }

      DoEvents();
      //Console.WriteLine("events: {0} ms", (float)(watch.ElapsedMilliseconds) / 1000);
      // watch.Restart();
      if (Update() == UpdateResult.Completed)
      {
        // config.Save(ConfigUtilities.GetConfigPath());
        // TODO: handle config save
        return;
      }
      //Console.WriteLine("update: {0} ms", watch.ElapsedMilliseconds);
    }

    public static bool IsKeyPressed(DoomKey key)
    {
      return Events.Any(e =>
      {
        return e.Type == EventType.KeyDown && e.Key == key;
      }
          );
    }

    public void NewGame(GameSkill skill, int episode, int map)
    {
      game.DeferedInitNew(skill, episode, map);
      nextState = ApplicationState.Game;
    }

    public void EndGame()
    {
      nextState = ApplicationState.Opening;
    }

    private void DoEvents()
    {
      if (wiping)
      {
        return;
      }

      foreach (var e in Events)
      {
        if (!PreviousDownEvents.Contains(e) && menu.DoEvent(e))
        {
          continue;
        }

        if (e.Type == EventType.KeyDown)
        {
          if (CheckFunctionKey(e.Key))
          {
            continue;
          }
        }

        if (currentState == ApplicationState.Game)
        {
          if (e.Key == DoomKey.Pause && e.Type == EventType.KeyDown)
          {
            sendPause = true;
            continue;
          }

          if (!(e.Key == DoomKey.Tab && PreviousDownEvents.Contains(e)) && game.DoEvent(e))
          {
            continue;
          }
        }
        else if (currentState == ApplicationState.DemoPlayback)
        {
          demoPlayback.DoEvent(e);
        }
      }


    }

    private bool CheckFunctionKey(DoomKey key)
    {
      switch (key)
      {
        case DoomKey.F1:
          menu.ShowHelpScreen();
          return true;

        case DoomKey.F2:
          menu.ShowSaveScreen();
          return true;

        case DoomKey.F3:
          menu.ShowLoadScreen();
          return true;

        case DoomKey.F4:
          menu.ShowVolumeControl();
          return true;

        case DoomKey.F6:
          menu.QuickSave();
          return true;

        case DoomKey.F7:
          if (currentState == ApplicationState.Game)
          {
            menu.EndGame();
          }
          else
          {
            options.Sound.StartSound(Sfx.OOF);
          }
          return true;

        case DoomKey.F8:
          renderer.DisplayMessage = !renderer.DisplayMessage;
          if (currentState == ApplicationState.Game && game.State == GameState.Level)
          {
            string msg;
            if (renderer.DisplayMessage)
            {
              msg = DoomInfo.Strings.MSGON;
            }
            else
            {
              msg = DoomInfo.Strings.MSGOFF;
            }
            game.World.ConsolePlayer.SendMessage(msg);
          }
          menu.StartSound(Sfx.SWTCHN);
          return true;

        case DoomKey.F9:
          menu.QuickLoad();
          return true;

        case DoomKey.F10:
          menu.Quit();
          return true;

        case DoomKey.F11:
          var gcl = renderer.GammaCorrectionLevel;
          gcl++;
          if (gcl > renderer.MaxGammaCorrectionLevel)
          {
            gcl = 0;
          }
          renderer.GammaCorrectionLevel = gcl;
          if (currentState == ApplicationState.Game && game.State == GameState.Level)
          {
            string msg;
            if (gcl == 0)
            {
              msg = DoomInfo.Strings.GAMMALVL0;
            }
            else
            {
              msg = "Gamma correction level " + gcl;
            }
            game.World.ConsolePlayer.SendMessage(msg);
          }
          return true;

        case DoomKey.Add:
        case DoomKey.Quote:
          if (currentState == ApplicationState.Game &&
              game.State == GameState.Level &&
              game.World.AutoMap.Visible)
          {
            return false;
          }
          else
          {
            renderer.WindowSize = Math.Min(renderer.WindowSize + 1, renderer.MaxWindowSize);
            options.Sound.StartSound(Sfx.STNMOV);
            return true;
          }

        case DoomKey.Subtract:
        case DoomKey.Hyphen:
          if (currentState == ApplicationState.Game &&
              game.State == GameState.Level &&
              game.World.AutoMap.Visible)
          {
            return false;
          }
          else
          {
            renderer.WindowSize = Math.Max(renderer.WindowSize - 1, 0);
            options.Sound.StartSound(Sfx.STNMOV);
            return true;
          }

        default:
          return false;
      }
    }
    private UpdateResult Update()
    {
      if (!wiping)
      {
        menu.Update();

        if (nextState != currentState)
        {
          if (nextState != ApplicationState.Opening)
          {
            opening.Reset();
          }

          if (nextState != ApplicationState.DemoPlayback)
          {
            demoPlayback = null;
          }

          currentState = nextState;
        }

        if (quit)
        {
          return UpdateResult.Completed;
        }

        if (needWipe)
        {
          needWipe = false;
          StartWipe();
        }
      }

      if (!wiping)
      {
        switch (currentState)
        {
          case ApplicationState.Opening:
            if (opening.Update() == UpdateResult.NeedWipe)
            {
              StartWipe();
            }
            break;

          case ApplicationState.DemoPlayback:
            var result = demoPlayback.Update();
            if (result == UpdateResult.NeedWipe)
            {
              StartWipe();
            }
            else if (result == UpdateResult.Completed)
            {
              Quit("FPS: " + demoPlayback.Fps.ToString("0.0"));
            }
            break;

          case ApplicationState.Game:
            userInput.BuildTicCmd(cmds[options.ConsolePlayer]);
            if (sendPause)
            {
              sendPause = false;
              cmds[options.ConsolePlayer].Buttons |= (byte)(TicCmdButtons.Special | TicCmdButtons.Pause);
            }
            if (game.Update(cmds) == UpdateResult.NeedWipe)
            {
              StartWipe();
            }
            break;

          default:
            throw new Exception("Invalid application state!");
        }
      }

      if (wiping)
      {
        var result = wipe.Update();
        renderer.RenderWipe(this, wipe);
        if (result == UpdateResult.Completed)
        {
          wiping = false;
        }
      }
      else
      {
        renderer.Render(this);
      }

      //Console.WriteLine(options.Music);
      if (musicFrameCount % 2 == 0)
      {
        //tweak to accelerate music
        options.Music.CustomAdvanceFrame();
        musicFrameCount = 0;
      }
      musicFrameCount++;
      options.Music.CustomAdvanceFrame();
      options.Sound.Update();
      return UpdateResult.None;
    }

    static int musicFrameCount = 0;

    private void StartWipe()
    {
      wipe.Start();
      renderer.InitializeWipe();
      wiping = true;
    }

    public void PauseGame()
    {
      if (currentState == ApplicationState.Game &&
          game.State == GameState.Level &&
          !game.Paused && !sendPause)
      {
        sendPause = true;
      }
    }

    public void ResumeGame()
    {
      if (currentState == ApplicationState.Game &&
          game.State == GameState.Level &&
          game.Paused && !sendPause)
      {
        sendPause = true;
      }
    }

    public bool SaveGame(int slotNumber, string description)
    {
      if (currentState == ApplicationState.Game && game.State == GameState.Level)
      {
        game.SaveGame(slotNumber, description);
        return true;
      }
      else
      {
        return false;
      }
    }

    public void LoadGame(int slotNumber)
    {
      game.LoadGame(slotNumber);
      nextState = ApplicationState.Game;
    }

    public void Quit()
    {
      quit = true;
    }

    public void Quit(string message)
    {
      quit = true;
      quitMessage = message;
    }

    public void Dispose()
    {
      if (userInput != null)
      {
        userInput.Dispose();
        userInput = null;
      }

      if (music != null)
      {
        music.Dispose();
        music = null;
      }

      if (sound != null)
      {
        sound.Dispose();
        sound = null;
      }

      if (renderer != null)
      {
        renderer.Dispose();
        renderer = null;
      }

      if (resource != null)
      {
        resource.Dispose();
        resource = null;
      }

      if (window != null)
      {
        window.Dispose();
        window = null;
      }
    }

    public ApplicationState State => currentState;
    public OpeningSequence Opening => opening;
    public DemoPlayback DemoPlayback => demoPlayback;
    public GameOptions Options => options;
    public DoomGame Game => game;
    public DoomMenu Menu => menu;
    public string QuitMessage => quitMessage;

    public static Stream SoundFontStream { get; internal set; }
  }
}
