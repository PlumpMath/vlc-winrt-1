﻿using libVLCX;
using System;
using Windows.Storage;

namespace VLC_WinRT.Model
{
    public interface IVLCMedia
    {
        int Id { get; set; }
        string Path { get; set; }
        string Name { get; set; }
        TimeSpan Duration { get; set; }
        StorageFile File { get; }
        bool IsCurrentPlaying { get; set; }
        Tuple<FromType, string> GetMrlAndFromType(bool preferToken = false);
        string Token { get; set; }
    }
}
