﻿using VLC_WinRT.ViewModels;
using Windows.Storage;
using System;
using VLC_WinRT.Utils;

namespace VLC_WinRT.Commands.Settings
{
    public class RemoveFolderFromVideoLibrary : AlwaysExecutableCommand
    {
        public override async void Execute(object parameter)
        {
            var lib = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
            await lib.RequestRemoveFolderAsync(parameter as StorageFolder);
            await Locator.SettingsVM.GetVideoLibraryFolders();
        }
    }
}
