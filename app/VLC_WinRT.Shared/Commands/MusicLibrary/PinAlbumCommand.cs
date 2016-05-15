﻿using VLC_WinRT.Helpers;
using VLC_WinRT.Model;
using VLC_WinRT.Model.Music;
using VLC_WinRT.Utils;
using VLC_WinRT.ViewModels;

namespace VLC_WinRT.Commands.MusicLibrary
{
    public class PinAlbumCommand : AlwaysExecutableCommand
    {
        public override async void Execute(object parameter)
        {
            if (parameter is AlbumItem)
            {
                var album = parameter as AlbumItem;
                if (await UpdateTileHelper.CreateOrReplaceSecondaryTile(VLCItemType.Album, album.Id, album.Name))
                {
                    album.IsPinned = !album.IsPinned;
                    await Locator.MediaLibrary.Update(album);
                }
            }
        }
    }
}
