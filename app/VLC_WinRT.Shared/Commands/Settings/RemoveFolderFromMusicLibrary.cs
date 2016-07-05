﻿using VLC_WinRT.ViewModels;
using Windows.Storage;
using System;
using VLC_WinRT.Utils;

namespace VLC_WinRT.Commands.Settings
{
    public class RemoveFolderFromMusicLibrary : AlwaysExecutableCommand
    {
        public override async void Execute(object parameter)
        {
            var lib = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
            await lib.RequestRemoveFolderAsync(parameter as StorageFolder);
            await  Locator.SettingsVM.GetMusicLibraryFolders();
        }
    }
}
