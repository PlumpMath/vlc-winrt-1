﻿using Windows.UI.Xaml.Controls;
using VLC_WinRT.ViewModels;
using VLC_WinRT.ViewModels.MusicVM;
using VLC_WinRT.Model;
using VLC_WinRT.Utils;

namespace VLC_WinRT.Commands.MusicPlayer
{
    public class TrackCollectionClickedCommand : AlwaysExecutableCommand
    {
        public override void Execute(object parameter)
        {
            if (parameter is PlaylistItem)
            {
                Locator.MusicLibraryVM.CurrentTrackCollection = parameter as PlaylistItem;
                Locator.NavigationService.Go(VLCPage.PlaylistPage);
            }
            else if (parameter is SelectionChangedEventArgs)
            {
                Locator.MusicLibraryVM.CurrentTrackCollection =
                    (parameter as SelectionChangedEventArgs).AddedItems[0] as PlaylistItem;
                Locator.NavigationService.Go(VLCPage.PlaylistPage);
            }
            else if (parameter is ItemClickEventArgs)
            {
                Locator.MusicLibraryVM.CurrentTrackCollection =
                    (parameter as ItemClickEventArgs).ClickedItem as PlaylistItem;
                Locator.NavigationService.Go(VLCPage.PlaylistPage);
            }
        }
    }
}
