﻿/**********************************************************************
 * VLC for WinRT
 **********************************************************************
 * Copyright © 2013-2014 VideoLAN and Authors
 *
 * Licensed under GPLv2+ and MPLv2
 * Refer to COPYING file of the official project for license
 **********************************************************************/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using VLC_WinRT.Commands;
using VLC_WinRT.Helpers;
using VLC_WinRT.Model.Video;
using VLC_WinRT.Services.RunTime;
using VLC_WinRT.Utils;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml;
using libVLCX;
using System.Diagnostics;
using Windows.Graphics.Display;
using Windows.UI.Xaml.Media;
using VLC_WinRT.Commands.VideoPlayer;
using VLC_WinRT.Model;

namespace VLC_WinRT.ViewModels.VideoVM
{
    public class VideoPlayerVM : BindableBase
    {
        #region private props
        private VideoItem _currentVideo;

        private VLCSurfaceZoom currentSurfaceZoom = VLCSurfaceZoom.SURFACE_BEST_FIT;
        private bool isVideoPlayerOptionsPanelVisible;
        private bool isVideoPlayerSubtitlesSettingsVisible;
        private bool isVideoPlayerAudioTracksSettingsVisible;
        private bool isVideoPlayerVolumeSettingsVisible;
        private List<VLCSurfaceZoom> zooms;

        #endregion

        #region private fields
        #endregion

        #region public props
        public VideoItem CurrentVideo
        {
            get { return _currentVideo; }
            set { SetProperty(ref _currentVideo, value); }
        }

        public VLCSurfaceZoom CurrentSurfaceZoom
        {
            get
            {
                return currentSurfaceZoom;
            }
            set
            {
                SetProperty(ref currentSurfaceZoom, value);
                ChangeSurfaceZoom(value);
            }
        }

        public bool IsVideoPlayerOptionsPanelVisible
        {
            get { return isVideoPlayerOptionsPanelVisible; }
            set { SetProperty(ref isVideoPlayerOptionsPanelVisible, value); }
        }

        public bool IsVideoPlayerSubtitlesSettingsVisible
        {
            get { return isVideoPlayerSubtitlesSettingsVisible; }
            set { SetProperty(ref isVideoPlayerSubtitlesSettingsVisible, value); }
        }

        public bool IsVideoPlayerAudioTracksSettingsVisible
        {
            get { return isVideoPlayerAudioTracksSettingsVisible; }
            set { SetProperty(ref isVideoPlayerAudioTracksSettingsVisible, value); }
        }

        public bool IsVideoPlayerVolumeSettingsVisible
        {
            get { return isVideoPlayerVolumeSettingsVisible; }
            set { SetProperty(ref isVideoPlayerVolumeSettingsVisible, value); }
        }

        public ActionCommand ToggleIsVideoPlayerOptionsPanelVisible { get; private set; } = new ActionCommand(() =>
        {
            Locator.NavigationService.Go(VLCPage.VideoPlayerOptionsPanel);
            Locator.VideoPlayerVm.IsVideoPlayerOptionsPanelVisible = false;
            Locator.VideoPlayerVm.IsVideoPlayerAudioTracksSettingsVisible = false;
            Locator.VideoPlayerVm.IsVideoPlayerSubtitlesSettingsVisible = false;
            Locator.VideoPlayerVm.IsVideoPlayerVolumeSettingsVisible = false;
        });


        public ActionCommand ToggleIsVideoPlayerSubtitlesSettingsVisible { get; private set; } = new ActionCommand(() =>
        {
            Locator.VideoPlayerVm.IsVideoPlayerAudioTracksSettingsVisible = false;
            Locator.VideoPlayerVm.IsVideoPlayerOptionsPanelVisible = false;
            Locator.VideoPlayerVm.IsVideoPlayerVolumeSettingsVisible = false;
            Locator.VideoPlayerVm.IsVideoPlayerSubtitlesSettingsVisible = !Locator.VideoPlayerVm.IsVideoPlayerSubtitlesSettingsVisible;
        });

        public ActionCommand ToggleIsVideoPlayerAudioTracksSettingsVisible { get; private set; } = new ActionCommand(() =>
        {
            Locator.VideoPlayerVm.IsVideoPlayerSubtitlesSettingsVisible = false;
            Locator.VideoPlayerVm.IsVideoPlayerOptionsPanelVisible = false;
            Locator.VideoPlayerVm.IsVideoPlayerVolumeSettingsVisible = false;
            Locator.VideoPlayerVm.IsVideoPlayerAudioTracksSettingsVisible = !Locator.VideoPlayerVm.IsVideoPlayerAudioTracksSettingsVisible;
        });


        public ActionCommand ToggleIsVideoPlayerVolumeSettingsVisible { get; private set; } = new ActionCommand(() =>
        {
            Locator.VideoPlayerVm.IsVideoPlayerAudioTracksSettingsVisible = false;
            Locator.VideoPlayerVm.IsVideoPlayerSubtitlesSettingsVisible = false;
            Locator.VideoPlayerVm.IsVideoPlayerOptionsPanelVisible = false;
            Locator.VideoPlayerVm.IsVideoPlayerVolumeSettingsVisible = !Locator.VideoPlayerVm.IsVideoPlayerVolumeSettingsVisible;
        });

        public SurfaceZoomToggleCommand SurfaceZoomToggleCommand { get; private set; } = new SurfaceZoomToggleCommand();

        public InitPiPCommand InitPiPCommand { get; private set; } = new InitPiPCommand();
        #endregion

        #region public fields

        public List<VLCSurfaceZoom> Zooms
        {
            get
            {
                if (zooms == null || !zooms.Any())
                {
                    zooms = new List<VLCSurfaceZoom>()
                    {
                        VLCSurfaceZoom.SURFACE_BEST_FIT,
                        VLCSurfaceZoom.SURFACE_FIT_HORIZONTAL,
                        VLCSurfaceZoom.SURFACE_FIT_VERTICAL,
                        VLCSurfaceZoom.SURFACE_STRETCH
                    };
                }
                return zooms;
            }
        }
        #endregion

        #region constructors
        #endregion

        #region methods
        public void OnNavigatedTo()
        {
            // If no playback was ever started, ContinueIndexing can be null
            // If we navigate back and forth to the main page, we also don't want to 
            // re-mark the task as completed.
            Locator.MediaLibrary.ContinueIndexing = new TaskCompletionSource<bool>();
            DisplayHelper.PrivateDisplayCall(true);
            Locator.Slideshow.IsPaused = true;
            if (Locator.SettingsVM.ForceLandscape)
            {
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            }
        }

        public void OnNavigatedFrom()
        {
            if (Locator.MediaLibrary.ContinueIndexing != null && !Locator.MediaLibrary.ContinueIndexing.Task.IsCompleted)
            {
                Locator.MediaLibrary.ContinueIndexing.TrySetResult(true);
            }
            Locator.VideoPlayerVm.IsVideoPlayerAudioTracksSettingsVisible = false;
            Locator.VideoPlayerVm.IsVideoPlayerOptionsPanelVisible = false;
            Locator.VideoPlayerVm.IsVideoPlayerSubtitlesSettingsVisible = false;
            Locator.VideoPlayerVm.IsVideoPlayerVolumeSettingsVisible = false;
            Locator.Slideshow.IsPaused = false;
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;
            DisplayHelper.PrivateDisplayCall(false);
        }

        public async Task<bool> TryUseSubtitleFromFolder()
        {
            // Trying to get the path of the current video
            var videoPath = "";
            if (CurrentVideo.File != null)
            {
                videoPath = CurrentVideo.File.Path;
            }
            else if (!string.IsNullOrEmpty(CurrentVideo.Path))
            {
                videoPath = CurrentVideo.Path;
            }
            else return false;

            var folderPath = "";
            var fileNameWithoutExtensions = "";
            try
            {
                folderPath = System.IO.Path.GetDirectoryName(videoPath);
                fileNameWithoutExtensions = System.IO.Path.GetFileNameWithoutExtension(videoPath);
            }
            catch
            {
                return false;
            }
            try
            {
                // Since we checked Video Libraries capability and SD Card compatibility, and DLNA discovery
                // I think WinRT will let us create a StorageFolder instance of the parent folder of the file we're playing
                // Unfortunately, if the video is opened via a filepicker AND that the video is in an unusual folder, like C:/randomfolder/
                // This might now work, hence the try catch
                var storageFolderParent = await StorageFolder.GetFolderFromPathAsync(folderPath);
                // Here we need to search for a file with the same name, but with .srt or .ssa (when supported) extension
                StorageFile storageFolderParentSubtitle = null;
                storageFolderParentSubtitle = await storageFolderParent.GetFileAsync(fileNameWithoutExtensions + ".srt");
                if (storageFolderParentSubtitle != null)
                {
                    Locator.MediaPlaybackViewModel.OpenSubtitleCommand.Execute(storageFolderParentSubtitle);
                    return true;
                }
            }
            catch
            {
                // Folder is not accessible cause outside of the sandbox
                // OR
                // File doesn't exist
            }
            return false;
        }

        public void ChangeSurfaceZoom(VLCSurfaceZoom desiredZoom)
        {
            if (!(Locator.MediaPlaybackViewModel._mediaService is VLCService)) return;

            var screenWidth = App.RootPage.SwapChainPanel.ActualWidth;
            var screenHeight = App.RootPage.SwapChainPanel.ActualHeight;

            var vlcService = (VLCService)Locator.MediaPlaybackViewModel._mediaService;
            var videoTrack = vlcService.MediaPlayer?.media()?.tracks()?.FirstOrDefault(x => x.type() == TrackType.Video);
            if (videoTrack == null) return;
            var videoHeight = videoTrack.height();
            var videoWidth = videoTrack.width();

            var sarDen = videoTrack.sarDen();
            var sarNum = videoTrack.sarNum();

            double var = 0, displayedVideoWidth;
            if (sarDen == sarNum)
            {
                // Assuming it's 1:1 pixel
                var = (float)videoWidth / videoHeight;
            }
            else
            {
                var = (videoWidth * (double)sarNum / sarDen) / videoHeight;
            }

            var screenar = (float)screenWidth / screenHeight;
            double displayedVideoHeight = 0;
            if (var > screenar)
            {
                displayedVideoHeight = screenWidth * ((float)videoHeight / videoWidth);
                displayedVideoWidth = screenWidth;
            }
            else
            {
                displayedVideoHeight = screenHeight;
                displayedVideoWidth = displayedVideoHeight * var;
            }

            double bandesNoiresVertical = screenHeight - displayedVideoHeight;
            double bandesNoiresHorizontal = screenWidth - displayedVideoWidth;

            var scaleTransform = new ScaleTransform();
            var verticalScale = Math.Abs(displayedVideoHeight / (displayedVideoHeight - bandesNoiresVertical));
            var horizontalScale = Math.Abs(displayedVideoWidth / (displayedVideoWidth - bandesNoiresHorizontal));

            scaleTransform.CenterX = screenWidth / 2;
            scaleTransform.CenterY = screenHeight / 2;
            switch (desiredZoom)
            {
                case VLCSurfaceZoom.SURFACE_BEST_FIT:
                    scaleTransform.ScaleX = scaleTransform.ScaleY = 1;
                    break;
                case VLCSurfaceZoom.SURFACE_FIT_HORIZONTAL:
                    scaleTransform.ScaleX = horizontalScale;
                    scaleTransform.ScaleY = horizontalScale;
                    scaleTransform.CenterX = screenWidth / 2;
                    scaleTransform.CenterY = screenHeight / 2;
                    break;
                case VLCSurfaceZoom.SURFACE_FIT_VERTICAL:
                    scaleTransform.ScaleX = verticalScale;
                    scaleTransform.ScaleY = verticalScale;
                    break;
                case VLCSurfaceZoom.SURFACE_STRETCH:
                    if (bandesNoiresVertical > 0)
                    {
                        scaleTransform.ScaleX = 1;
                        scaleTransform.ScaleY = verticalScale;
                    }
                    else if (bandesNoiresHorizontal > 0)
                    {
                        scaleTransform.ScaleX = horizontalScale;
                        scaleTransform.ScaleY = 1;
                    }
                    break;
                //case VLCSurfaceZoom.SURFACE_FILL:
                //    break;
                //case VLCSurfaceZoom.SURFACE_16_9:
                //    break;
                //case VLCSurfaceZoom.SURFACE_4_3:
                //    break;
                //case VLCSurfaceZoom.SURFACE_ORIGINAL:
                //    break;
                //case VLCSurfaceZoom.SURFACE_CUSTOM_ZOOM:
                //    break;
                default:
                    break;
            }
            App.RootPage.SwapChainPanel.RenderTransform = scaleTransform;
        }
        #endregion
    }
}
