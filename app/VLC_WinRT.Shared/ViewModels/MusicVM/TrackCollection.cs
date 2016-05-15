﻿/**********************************************************************
 * VLC for WinRT
 **********************************************************************
 * Copyright © 2013-2014 VideoLAN and Authors
 *
 * Licensed under GPLv2+ and MPLv2
 * Refer to COPYING file of the official project for license
 **********************************************************************/

using Windows.UI.Core;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SQLite;
using VLC_WinRT.Commands.MusicPlayer;
using VLC_WinRT.Model.Music;
using System.Collections.Generic;
using System.Linq;
using VLC_WinRT.BackgroundHelpers;
using VLC_WinRT.BackgroundAudioPlayer.Model;
using VLC_WinRT.Helpers;
using VLC_WinRT.Model;
using VLC_WinRT.Model.Video;
using VLC_WinRT.Utils;
using System.Diagnostics;
using VLC_WinRT.Services.RunTime;

namespace VLC_WinRT.ViewModels.MusicVM
{
    public class TrackCollection : BindableBase
    {
        private SmartCollection<IMediaItem> _tracksCollection;
        private SmartCollection<IMediaItem> _nonShuffledPlaylist;
        private int _currentTrack;
        private bool _isRunning;
        private bool _isShuffled;
        private bool _repeat;

        // ui related management
        private ObservableCollection<IMediaItem> _selectedTracks;

        [PrimaryKey, AutoIncrement, Column("_id")]
        public int Id { get; set; }

        public string Name { get; set; }

        public int CurrentTrack
        {
            get
            {
                return _currentTrack;
            }
            set
            {
                SetProperty(ref _currentTrack, value);
            }
        }

        [Ignore]
        public bool Repeat
        {
            get { return _repeat; }
            set
            {
                SetProperty(ref _repeat, value);
#if WINDOWS_PHONE_APP
                if (Locator.MediaPlaybackViewModel._mediaService is BGPlayerService)
                {
                    ((BGPlayerService)Locator.MediaPlaybackViewModel._mediaService).SetRepeat(value);
                }
#endif
            }
        }


        [Ignore]
        public bool CanGoPrevious
        {
            get
            {
                var previous = (CurrentTrack > 0);
                Locator.MediaPlaybackViewModel.SystemMediaTransportControlsBackPossible(previous);
                return previous;
            }
        }

        [Ignore]
        public bool CanGoNext
        {
            get
            {
                var next = (Playlist.Count != 1) && (CurrentTrack < Playlist.Count - 1);
                Locator.MediaPlaybackViewModel.SystemMediaTransportControlsNextPossible(next);
                return next;
            }
        }

        [Ignore]
        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }
            set
            {
                SetProperty(ref _isRunning, value);
            }
        }

        [Ignore]
        public PlayTrackCollCommand PlayTrackCollCommand { get; } = new PlayTrackCollCommand();

        [Ignore]
        public bool IsShuffled
        {
            get { return _isShuffled; }
            set { SetProperty(ref _isShuffled, value); }
        }

        #region public fields
        [Ignore]
        public SmartCollection<IMediaItem> Playlist
        {
            get { return _tracksCollection; }
            private set { SetProperty(ref _tracksCollection, value); }
        }

        [Ignore]
        public SmartCollection<IMediaItem> NonShuffledPlaylist
        {
            get { return _nonShuffledPlaylist; }
            set { SetProperty(ref _nonShuffledPlaylist, value); }
        }

        [Ignore]
        public ObservableCollection<IMediaItem> SelectedTracks
        {
            get { return _selectedTracks ?? (_selectedTracks = new ObservableCollection<IMediaItem>()); }
            set { SetProperty(ref _selectedTracks, value); }
        }

        #endregion

        #region ctors
        public TrackCollection(bool isRuntimePlaylist)
        {
            if (isRuntimePlaylist)
            {
                RestorePlaylist();
            }
            _tracksCollection = new SmartCollection<IMediaItem>();
            InitializePlaylist();
        }

        public TrackCollection()
        {
            _tracksCollection = new SmartCollection<IMediaItem>();
            InitializePlaylist();
        }
        #endregion

        #region methods
        public void InitializePlaylist()
        {
            Playlist.Clear();
            CurrentTrack = -1;
        }

        public async Task ResetCollection()
        {
            await App.BackgroundAudioHelper.ResetCollection(ResetType.NormalReset);
            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
            {
                Playlist.Clear();
                CurrentTrack = -1;
                NonShuffledPlaylist?.Clear();
                IsShuffled = false;
            });
        }

        public void SetActiveTrackProperty()
        {
            try
            {
                if (Playlist == null || !Playlist.Any() || _currentTrack == -1) return;
                foreach (var trackItem in Playlist)
                {
                    if (trackItem == null) continue;
                    trackItem.IsCurrentPlaying = false;
                }
                if (_currentTrack < Playlist?.Count && Playlist[_currentTrack] != null)
                {
                    Playlist[_currentTrack].IsCurrentPlaying = true;
                    Debug.WriteLine(Playlist[_currentTrack].Path + " Is the active track");
                }
                OnPropertyChanged("CanGoPrevious");
                OnPropertyChanged("CanGoNext");
            }
            catch (Exception exception)
            {

            }
        }

        public async Task SetPlaylist(IEnumerable<IMediaItem> tracks)
        {
            var playlist = new SmartCollection<IMediaItem>(tracks);
            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () => Playlist = playlist);
            var backgroundTracks = BackgroundTaskTools.CreateBackgroundTrackItemList(Locator.MediaPlaybackViewModel.TrackCollection.Playlist.ToTrackItemPlaylist());
            await App.BackgroundAudioHelper.AddToPlaylist(backgroundTracks);
        }

        public async Task Shuffle()
        {
            if (IsShuffled)
            {
                NonShuffledPlaylist = new SmartCollection<IMediaItem>(Playlist);
                Random r = new Random();
                for (int i = 0; i < Playlist.Count; i++)
                {
                    if (i > CurrentTrack)
                    {
                        int index1 = r.Next(i, Playlist.Count);
                        int index2 = r.Next(i, Playlist.Count);
                        Playlist.Move(index1, index2);
                    }
                }
            }
            else
            {
                await SetPlaylist(Locator.MediaPlaybackViewModel.TrackCollection.NonShuffledPlaylist);
            }
            await App.BackgroundAudioHelper.ResetCollection(ResetType.ShuffleReset);
            var backgorundTracks = BackgroundTaskTools.CreateBackgroundTrackItemList(Playlist.ToTrackItemPlaylist());
            await App.BackgroundAudioHelper.AddToPlaylist(backgorundTracks);
        }

        public void Remove(IMediaItem media)
        {
            Playlist.Remove(media);
        }

        public async Task Add(VideoItem videoItem, bool isPlayingPlaylist)
        {
            if (Playlist.FirstOrDefault(x => x.Path == videoItem.Path) != null) return;
            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
            {
                Playlist.Add(videoItem);
                OnPropertyChanged(nameof(CanGoNext));
            });
        }

        public async Task Add(TrackItem trackItem, bool isPlayingPlaylist)
        {
            if (Playlist.FirstOrDefault(x => x.Id == trackItem.Id) != null) return;
            trackItem.Index = (uint)Playlist.Count;
            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Low, () =>
            {
                Playlist.Add(trackItem);
                OnPropertyChanged(nameof(CanGoNext));
            });
            var backgroundTrack = BackgroundTaskTools.CreateBackgroundTrackItem(trackItem);
            await App.BackgroundAudioHelper.AddToPlaylist(backgroundTrack);
        }

        public async Task Add(List<TrackItem> trackItems)
        {
            var count = (uint)Playlist.Count;
            foreach (var track in trackItems)
            {
                track.Index = count;
                count++;
            }
            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Low, () =>
            {
                Playlist.AddRange(trackItems);
                OnPropertyChanged(nameof(CanGoNext));
            });
            var backgroundTracks = BackgroundTaskTools.CreateBackgroundTrackItemList(trackItems);
            await App.BackgroundAudioHelper.AddToPlaylist(backgroundTracks);
        }

        public async Task RestorePlaylist()
        {
            var playlist = Locator.MediaPlaybackViewModel.BackgroundTrackRepository.LoadPlaylist();
            if (!playlist.Any())
            {
                return;
            }
            var trackIds = playlist.Select(node => node.Id);
            Playlist = new SmartCollection<IMediaItem>();
            foreach (int trackId in trackIds)
            {
                var trackItem = await Locator.MediaLibrary.LoadTrackById(trackId);
                if (trackItem != null)
                    Playlist.Add(trackItem);
            }
            
            await Locator.MusicPlayerVM.UpdateTrackFromMF();
            App.BackgroundAudioHelper.RestorePlaylist();
            if (Locator.MusicPlayerVM.CurrentTrack != null)
            {
                IsRunning = true;
                await Locator.MediaPlaybackViewModel.SetMedia(Locator.MusicPlayerVM.CurrentTrack, false, false);
            }
        }
        #endregion

        /// <summary>
        /// Only this method should set the CurrentTrack property of TrackCollection.
        /// </summary>
        /// <param name="index"></param>
        public Task SetCurrentTrackPosition(int index)
        {
            return DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () => CurrentTrack = index);
        }
    }
}
