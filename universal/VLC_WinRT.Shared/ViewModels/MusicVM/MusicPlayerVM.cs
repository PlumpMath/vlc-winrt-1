﻿/**********************************************************************
 * VLC for WinRT
 **********************************************************************
 * Copyright © 2013-2014 VideoLAN and Authors
 *
 * Licensed under GPLv2+ and MPLv2
 * Refer to COPYING file of the official project for license
 **********************************************************************/

using VLC_WinRT.DataRepository;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Core;
using VLC_WinRT.Commands.MediaPlayback;
using VLC_WinRT.Commands.Music;
using VLC_WinRT.Helpers;
using VLC_WinRT.Model.Music;
using VLC_WinRT.Commands.Social;
using VLC_WinRT.Common;
using VLC_WinRT.Database.DataRepository;
using VLC_WinRT.BackgroundAudioPlayer.Model;
#if WINDOWS_PHONE_APP
using Windows.Media.Playback;
#endif

namespace VLC_WinRT.ViewModels.MusicVM
{
    public class MusicPlayerVM : BindableBase
    {
        #region private props
        private AlbumItem _currentAlbum;
        private ArtistItem _currentArist;
        private ArtistDataRepository _artistDataRepository = new ArtistDataRepository();
        private AlbumDataRepository _albumDataRepository = new AlbumDataRepository();
        #endregion

        #region private fields
        #endregion

        #region public props
        public BackgroundTrackRepository BackgroundTrackRepository { get; set; } = new BackgroundTrackRepository();

        public AlbumItem CurrentAlbum
        {
            get { return _currentAlbum; }
            set
            {
                SetProperty(ref _currentAlbum, value);
                OnPropertyChanged();
            }
        }

        public ArtistItem CurrentArtist
        {
            get { return _currentArist; }
            set
            {
                SetProperty(ref _currentArist, value);
                OnPropertyChanged();
            }
        }

        public TrackItem CurrentTrack
        {
            get
            {
                if (Locator.MediaPlaybackViewModel.TrackCollection.CurrentTrack == -1
                    || Locator.MediaPlaybackViewModel.TrackCollection.CurrentTrack == Locator.MediaPlaybackViewModel.TrackCollection.Playlist.Count)
                    return null;
                if (Locator.MediaPlaybackViewModel.TrackCollection.CurrentTrack > Locator.MediaPlaybackViewModel.TrackCollection.Playlist.Count)
                {
                    App.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Locator.MediaPlaybackViewModel.TrackCollection.CurrentTrack = 0);
                    return null;
                }
                var media = Locator.MediaPlaybackViewModel.CurrentMedia;
                return (media is TrackItem) ? (TrackItem)media : null;
            }
        }

        public GoToMusicPlayerPage GoToMusicPlayerPage { get; } = new GoToMusicPlayerPage();

        public ShuffleCommand Shuffle { get; }= new ShuffleCommand();

        public ShareNowPlayingMusicCommand ShareNowPlayingMusicCommand { get; }=new ShareNowPlayingMusicCommand();

        public GoToMusicPlaylistPageCommand GoToMusicPlaylistPageCommand { get; } = new GoToMusicPlaylistPageCommand();
        #endregion

        public MusicPlayerVM()
        {
        }
                
        public async Task UpdateWindows8UI()
        {
            // Setting the info for windows 8 controls
            var resourceLoader = new ResourceLoader();
            string artistName = CurrentTrack.ArtistName ?? resourceLoader.GetString("UnknownArtist");
            string albumName = CurrentTrack.AlbumName;
            string trackName = CurrentTrack.Name ?? resourceLoader.GetString("UnknownTrack");
            var picture = Locator.MusicPlayerVM.CurrentAlbum != null ? Locator.MusicPlayerVM.CurrentAlbum.AlbumCoverUri : null;

            await Locator.MediaPlaybackViewModel.SetMediaTransportControlsInfo(artistName, albumName, trackName, picture);

            var notificationOnNewSong = ApplicationSettingsHelper.ReadSettingsValue("NotificationOnNewSong");
            if (notificationOnNewSong != null && (bool)notificationOnNewSong)
            {
                var notificationOnNewSongForeground = ApplicationSettingsHelper.ReadSettingsValue("NotificationOnNewSongForeground");
                if (Locator.MainVM.IsBackground || (notificationOnNewSongForeground != null && (bool)notificationOnNewSongForeground))
                {
                    ToastHelper.ToastImageAndText04(trackName, albumName, artistName, (Locator.MusicPlayerVM.CurrentAlbum == null) ? null : Locator.MusicPlayerVM.CurrentAlbum.AlbumCoverUri ?? null);
                }
            }
        }
        
        public async Task UpdateTrackFromMF()
        {
            await App.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
#if WINDOWS_PHONE_APP
                // TODO : this shouldn't be here
                Locator.MediaPlaybackViewModel.OnLengthChanged((long)BackgroundMediaPlayer.Current.NaturalDuration.TotalMilliseconds);
#endif          
                if (!ApplicationSettingsHelper.Contains(BackgroundAudioConstants.CurrentTrack)) return;
                int index = (int)ApplicationSettingsHelper.ReadSettingsValue(BackgroundAudioConstants.CurrentTrack);
                Locator.MediaPlaybackViewModel.TrackCollection.CurrentTrack = index;
                await SetCurrentArtist();
                await SetCurrentAlbum();
                await UpdatePlayingUI();
            });
        }

        public async Task UpdatePlayingUI()
        {
            await App.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Locator.MediaPlaybackViewModel.TrackCollection.IsRunning = true;
                Locator.MediaPlaybackViewModel.TrackCollection.SetActiveTrackProperty();
                OnPropertyChanged("TrackCollection");
                OnPropertyChanged("PlayingType");
                OnPropertyChanged("CurrentTrack");
                UpdateTileHelper.UpdateMediumTileWithMusicInfo();
            });
        }

        public async Task SetCurrentArtist()
        {
            if (CurrentTrack == null) return;
            if (CurrentArtist != null && CurrentArtist.Id == CurrentTrack.ArtistId) return;
            CurrentArtist = await _artistDataRepository.LoadArtist(CurrentTrack.ArtistId);
        }

        public async Task SetCurrentAlbum()
        {
            if (CurrentTrack == null) return;
            if (CurrentArtist == null) return;
            if (CurrentAlbum != null && CurrentAlbum.Id == CurrentTrack.AlbumId) return;
            CurrentAlbum = await _albumDataRepository.LoadAlbum(CurrentTrack.AlbumId);
        }
    }
}