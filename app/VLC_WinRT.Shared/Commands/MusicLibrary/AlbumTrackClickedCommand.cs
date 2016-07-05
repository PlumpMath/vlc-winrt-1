﻿using Windows.UI.Xaml.Controls;
using VLC_WinRT.Model.Music;
using VLC_WinRT.ViewModels;
using VLC_WinRT.Model;
using VLC_WinRT.Utils;
using VLC_WinRT.Helpers;
using System.Threading.Tasks;

namespace VLC_WinRT.Commands.MusicLibrary
{
    public class AlbumTrackClickedCommand : AlwaysExecutableCommand
    {
        public override async void Execute(object parameter)
        {
            Locator.NavigationService.Go(VLCPage.MusicPlayerPage);
            TrackItem track = null;
            if (parameter is ItemClickEventArgs)
            {
                var args = parameter as ItemClickEventArgs;
                track = args.ClickedItem as TrackItem;
            }
            if (track == null)
            {
                // if the track is still null (for some reason), we need to break early.
                return;
            }

            var playlist = await Locator.MediaLibrary.LoadTracksByAlbumId(track.AlbumId);
            await Locator.MediaPlaybackViewModel.PlaybackService.SetPlaylist(playlist, true, true, track);
        }
    }
}
