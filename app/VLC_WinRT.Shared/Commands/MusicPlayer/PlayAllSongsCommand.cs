﻿using System.Linq;
using Windows.UI.Xaml.Controls;
using VLC_WinRT.Model;
using VLC_WinRT.Model.Music;
using VLC_WinRT.ViewModels;
using System.Collections.ObjectModel;
using VLC_WinRT.Utils;
using VLC_WinRT.Helpers;

namespace VLC_WinRT.Commands.MusicPlayer
{
    public class PlayAllSongsCommand : AlwaysExecutableCommand
    {
        public override async void Execute(object parameter)
        {
            var tracks = await Locator.MediaLibrary.LoadTracks();
            if (tracks == null || !tracks.Any()) return;
            Locator.NavigationService.Go(VLCPage.MusicPlayerPage);
            var itemClickArgs = parameter as ItemClickEventArgs;
            var index = 0;
            if (itemClickArgs != null)
            {
                var selectedTrack = itemClickArgs.ClickedItem as TrackItem;
                index = tracks.IndexOf(tracks.FirstOrDefault(x => x.Id == selectedTrack.Id));
            }
            if (index > -1)
                await PlaylistHelper.AddTrackCollectionToPlaylistAndPlay(tracks.ToPlaylist(), true, index);
        }
    }
}
