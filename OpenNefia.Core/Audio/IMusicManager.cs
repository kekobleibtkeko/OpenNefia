﻿using Melanchall.DryWetMidi.Multimedia;
using OpenNefia.Core.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNefia.Core.Audio
{
    public interface IMusicManager
    {
        public bool IsPlaying { get; }

        public void Initialize();
        public void Shutdown();

        IEnumerable<OutputDevice> GetMidiOutputDevices();

        /// <summary>
        /// Plays a music file.
        /// </summary>
        /// <param name="musicId">Prototype of the music to play.</param>
        public void Play(PrototypeId<MusicPrototype> musicId);

        /// <summary>
        /// Restarts playing the current music.
        /// </summary>
        public void Restart();

        /// <summary>
        /// Stops playing music.
        /// </summary>
        public void Stop();
    }
}
