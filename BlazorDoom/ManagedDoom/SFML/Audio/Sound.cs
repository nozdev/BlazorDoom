
using System.Collections.Generic;
using System;
using ManagedDoom;
using SFML.System;
using Time = System.TimeSpan;
using Microsoft.JSInterop;
namespace SFML.Audio
{
    public class Sound
    {
        public SoundStatus Status { get; internal set; }
        public SoundBuffer SoundBuffer { get; internal set; }
        public float Pitch { get; internal set; }
        public float Volume { get; internal set; }
        public Time PlayingOffset { get; internal set; }
        public Vector3f Position { get; internal set; }

        internal void Stop()
        {
            // TODO: implement
        }

        internal void Play(IJSRuntime JSRuntime,int channel = 0)
        {
            int[] samples = Array.ConvertAll(SoundBuffer.samples, Convert.ToInt32);

            JSRuntime.InvokeAsync<string>("playSound", samples, (int)SoundBuffer.sampleRate, channel);

            //BlazorDoom.Renderer.playSoundOnJS(samples, (int)SoundBuffer.sampleRate, channel);
        }

        public override string ToString()
        {
            return $"{SoundBuffer.samples.Length} samples. Pitch: {Pitch}, Volume: {Volume}, Position: {Position}";
        }

        internal void Pause()
        {
            // TODO: implement
        }

        internal void Dispose()
        {
            // TODO: implement
        }
    }
}