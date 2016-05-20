﻿/**********************************************************************
 * VLC for WinRT
 **********************************************************************
 * Copyright © 2013-2014 VideoLAN and Authors
 *
 * Licensed under GPLv2+ and MPLv2
 * Refer to COPYING file of the official project for license
 **********************************************************************/

using Windows.Storage.AccessCache;
using Windows.Media;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Storage;
using System;
using System.IO;
using Windows.UI.Core;
using VLC_WinRT.Helpers;
using VLC_WinRT.Model;
using VLC_WinRT.Services.Interface;
using Windows.System.Display;
using VLC_WinRT.Commands.MediaPlayback;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Autofac;
using VLC_WinRT.Helpers.MusicLibrary;
using VLC_WinRT.Model.Music;
using VLC_WinRT.Model.Stream;
using VLC_WinRT.Model.Video;
using VLC_WinRT.Services.RunTime;
using VLC_WinRT.ViewModels.MusicVM;
using VLC_WinRT.Commands;
using VLC_WinRT.BackgroundAudioPlayer.Model;
#if WINDOWS_PHONE_APP
using Windows.Media.Playback;
#endif
using libVLCX;
using VLC_WinRT.BackgroundHelpers;
using VLC_WinRT.Utils;
using WinRTXamlToolkit.Controls.Extensions;
using VLC_WinRT.Commands.VideoPlayer;
using VLC_WinRT.Commands.VideoLibrary;
using VLC_WinRT.SharedBackground.Database;
using System.Linq;
using VLC_WinRT.Helpers.UIHelpers;

namespace VLC_WinRT.ViewModels
{
    public sealed class MediaPlaybackViewModel : BindableBase, IDisposable
    {
        #region private props
        private MouseService _mouseService;
        private SystemMediaTransportControls _systemMediaTransportControls;
        private PlayerEngine _playerEngine;
        private bool _isPlaying;
        private MediaState _mediaState;
        private PlayingType _playingType;
        private TrackCollection _trackCollection;
        private TimeSpan _timeTotal;

        private int _currentSubtitle;
        private int _currentAudioTrack;
        
        private int _volume = 100;
        private int _speedRate;
        private long _audioDelay;
        private long _spuDelay;
        private bool _isStream;
        private int _bufferingProgress;
        #endregion

        #region private fields
        private List<DictionaryKeyValue> _subtitlesTracks = new List<DictionaryKeyValue>();
        private List<DictionaryKeyValue> _audioTracks = new List<DictionaryKeyValue>();
        private List<VLCChapterDescription> _chapters = new List<VLCChapterDescription>();
        private bool _isBuffered;
        #endregion

        #region public props
        public MouseService MouseService { get { return _mouseService; } }
        
        public bool UseVlcLib { get; set; }
        
        public IMediaItem CurrentMedia
        {
            get
            {
                if (TrackCollection.CurrentTrack == -1) return null;
                if (TrackCollection.CurrentTrack == TrackCollection.Playlist.Count)
                    TrackCollection.CurrentTrack--;
                if (TrackCollection.Playlist.Count == 0) return null;
                return TrackCollection.Playlist[TrackCollection.CurrentTrack];
            }
        }

        public IMediaService _mediaService
        {
            get
            {
                switch (_playerEngine)
                {
                    case PlayerEngine.VLC:
                        return Locator.VLCService;
#if WINDOWS_PHONE_APP
                    case PlayerEngine.BackgroundMFPlayer:
                        return Locator.BGPlayerService;
#endif
                    default:
                        //todo : Implement properly BackgroundPlayerService 
                        //todo : so we get rid ASAP of this default switch
                        return Locator.VLCService;
                }
            }
        }

        public PlayingType PlayingType
        {
            get { return _playingType; }
            set { SetProperty(ref _playingType, value); }
        }

        public bool IsPlaying
        {
            get
            {
                return _isPlaying;
            }
            set
            {
                if (value != _isPlaying)
                {
                    SetProperty(ref _isPlaying, value);
                }
                OnPropertyChanged("PlayingType");
            }
        }

        public MediaState MediaState
        {
            get { return _mediaState; }
            set { SetProperty(ref _mediaState, value); }
        }

        public int Volume
        {
            get
            {
                return _volume;
            }
            set
            {
                Debug.WriteLine("new volume set: " + value);
                if (value > 0 && value <= 100)
                {
                    _mediaService?.SetVolume(value);
                    SetProperty(ref _volume, value);
                }
            }
        }

        public int SpeedRate
        {
            get
            {
                return _speedRate;
            }
            set
            {
                SetProperty(ref _speedRate, value);
                float r = (float)value / 100;
                SetRate(r);
            }
        }

        /// <summary>
        /// Gets and Sets the AudioDelay in MilliSeconds
        /// Warning : VLC API needs microseconds, hence the 1000 multiplication
        /// </summary>
        public long AudioDelay
        {
            get
            {
                return _audioDelay;
            }
            set
            {
                if (_mediaService is VLCService)
                {
                    (_mediaService as VLCService).SetAudioDelay(value * 1000);
                    SetProperty(ref _audioDelay, value);
                }
            }
        }


        /// <summary>
        /// Gets and Sets the SpuDelay in MilliSeconds
        /// Warning : VLC API needs microseconds, hence the 1000 multiplication
        /// </summary>
        public long SpuDelay
        {
            get { return _spuDelay; }
            set
            {
                if (_mediaService is VLCService)
                {
                    (_mediaService as VLCService).SetSpuDelay(value * 1000);
                    SetProperty(ref _spuDelay, value);
                }
            }
        }

        public bool IsStream
        {
            get { return _isStream; }
            set { SetProperty(ref _isStream, value); }
        }

        public TrackCollection TrackCollection
        {
            get
            {
                _trackCollection = _trackCollection ?? new TrackCollection(true);
                return _trackCollection;
            }
        }

        public PlayPauseCommand PlayOrPauseCommand { get; } = new PlayPauseCommand();

        public PlayNextCommand PlayNextCommand { get; } = new PlayNextCommand();

        public PlayPreviousCommand PlayPreviousCommand { get; } = new PlayPreviousCommand();

        public SetSubtitleTrackCommand SetSubtitleTrackCommand { get; } = new SetSubtitleTrackCommand();

        public OpenSubtitleCommand OpenSubtitleCommand { get; } = new OpenSubtitleCommand();

        public SetAudioTrackCommand SetAudioTrackCommand { get; } = new SetAudioTrackCommand();

        public PickMediaCommand PickMediaCommand { get; } = new PickMediaCommand();

        public StopVideoCommand GoBack { get; } = new StopVideoCommand();

        public ChangePlaybackSpeedRateCommand ChangePlaybackSpeedRateCommand { get; } = new ChangePlaybackSpeedRateCommand();

        public ChangeVolumeCommand ChangeVolumeCommand { get; } = new ChangeVolumeCommand();

        public ChangeAudioDelayCommand ChangeAudioDelayCommand { get; } = new ChangeAudioDelayCommand();

        public ChangeSpuDelayCommand ChangeSpuDelayCommand { get; } = new ChangeSpuDelayCommand();

        public TimeSpan TimeTotal
        {
            get { return _timeTotal; }
            set { SetProperty(ref _timeTotal, value); }
        }

        public int BufferingProgress => _bufferingProgress;

        public bool IsBuffered
        {
            get { return _isBuffered; }
            set { SetProperty(ref _isBuffered, value); }
        }

        /**
         * Elasped time in milliseconds
         */
        public long Time
        {
            get
            {
                return _mediaService.GetTime();
            }
            set
            {
                _mediaService.SetTime(value);
            }
        }

        public float Position
        {
            get
            {
                return _mediaService.GetPosition();
            }
            set
            {
                _mediaService.SetPosition(value);
            }
        }

        public DictionaryKeyValue CurrentSubtitle
        {
            get
            {
                if (_currentSubtitle < 0 || _currentSubtitle >= Subtitles.Count)
                    return null;
                return Subtitles[_currentSubtitle];
            }
            set
            {
                _currentSubtitle = Subtitles.IndexOf(value);
                if (value != null)
                    SetSubtitleTrackCommand.Execute(value.Id);
            }
        }

        public DictionaryKeyValue CurrentAudioTrack
        {
            get
            {
                if (_currentAudioTrack == -1 || _currentAudioTrack >= AudioTracks.Count)
                    return null;
                return AudioTracks[_currentAudioTrack];
            }
            set
            {
                _currentAudioTrack = AudioTracks.IndexOf(value);
                if (value != null)
                    SetAudioTrackCommand.Execute(value.Id);
            }
        }

        public VLCChapterDescription CurrentChapter
        {
            get
            {
                if (!(_mediaService is VLCService)) return null;
                var vlcService = (VLCService)_mediaService;
                var currentChapter = vlcService.MediaPlayer?.chapter();
                if (_chapters?.Count > 0 && currentChapter.HasValue)
                {
                    return _chapters[currentChapter.Value];
                }
                return null;
            }
            set
            {
                if (!(_mediaService is VLCService)) return;
                var vlcService = (VLCService)_mediaService;
                if (value == CurrentChapter) return;
                var index = _chapters.IndexOf(value);
                if (index > -1)
                {
                    vlcService.MediaPlayer.setChapter(index);
                }
            }
        }
        #endregion

        #region public fields
        public BackgroundTrackDatabase BackgroundTrackRepository { get; set; } = new BackgroundTrackDatabase();
        public List<DictionaryKeyValue> AudioTracks
        {
            get { return _audioTracks; }
            set { _audioTracks = value; }
        }

        public List<DictionaryKeyValue> Subtitles
        {
            get { return _subtitlesTracks; }
            set { _subtitlesTracks = value; }
        }

        public List<VLCChapterDescription> Chapters
        {
            get { return _chapters; }
            set { _chapters = value; }
        }

        #endregion

        #region constructors
        public MediaPlaybackViewModel()
        {
            _mouseService = App.Container.Resolve<MouseService>();
        }
        #endregion

        #region methods
        public async Task OpenFile(StorageFile file)
        {
            if (file == null) return;
            if (string.IsNullOrEmpty(file.Path))
            {
                // It's definitely a stream since it doesn't add a proper path but a FolderRelativeId
                // WARNING : Apps should use vlc://openstream/?from=url&url= for this matter
                var mrl = file.FolderRelativeId;
                var lastIndex = mrl.LastIndexOf("\\", StringComparison.OrdinalIgnoreCase);
                if (lastIndex != -1)
                    mrl = mrl.Remove(0, lastIndex + "\\".Length);
                await PlayStream(mrl);
                return;
            }

            var token = StorageApplicationPermissions.FutureAccessList.Add(file);
            var fileType = VLCFileExtensions.FileTypeHelper(file.FileType);
            if (fileType == VLCFileExtensions.VLCFileType.Video)
            {
                await PlayVideoFile(file, token);
            }
            else if (fileType == VLCFileExtensions.VLCFileType.Audio)
            {
                await PlayAudioFile(file, token);
            }
            else if (fileType == VLCFileExtensions.VLCFileType.Subtitle)
            {
                if (IsPlaying && PlayingType == PlayingType.Video)
                    OpenSubtitleCommand.Execute(file);
            }
        }

        public async Task PlayMedia(IMediaItem mediaItem)
        {
            if (mediaItem is VideoItem || mediaItem is StreamMedia)
            {
                Locator.NavigationService.Go(VLCPage.VideoPlayerPage);
                await Locator.MediaPlaybackViewModel.SetMedia(mediaItem);
            }
        }

        /// <summary>
        /// Navigates to the Video Player with the request MRL as parameter
        /// </summary>
        /// <param name="mrl">The stream MRL to be played</param>
        /// <returns></returns>
        public async Task PlayStream(string streamMrl)
        {
            try
            {
                var stream = await Locator.MediaLibrary.LoadStreamFromDatabaseOrCreateOne(streamMrl);
                await Locator.MediaPlaybackViewModel.SetMedia(stream);
            }
            catch (Exception e)
            {
                LogHelper.Log(StringsHelper.ExceptionToString(e));
                return;
            }
            Locator.NavigationService.Go(VLCPage.VideoPlayerPage);
        }

        /// <summary>
        /// Navigates to the Audio Player screen with the requested file a parameter.
        /// </summary>
        /// <param name="file">The file to be played.</param>
        /// <param name="token">Token is for files that are NOT in the sandbox, such as files taken from the filepicker from a sd card but not in the Video/Music folder.</param>
        public async Task PlayAudioFile(StorageFile file, string token = null)
        {
            var trackItem = await Locator.MediaLibrary.GetTrackItemFromFile(file, token);
            await PlaylistHelper.PlayTrackFromFilePicker(trackItem);
        }

        /// <summary>
        /// Navigates to the Video Player screen with the requested file a parameter.
        /// </summary>
        /// <param name="file">The file to be played.</param>
        /// <param name="token">Token is for files that are NOT in the sandbox, such as files taken from the filepicker from a sd card but not in the Video/Music folder.</param>
        public async Task PlayVideoFile(StorageFile file, string token = null)
        {
            var video = await MediaLibraryHelper.GetVideoItem(file);
            if (token != null)
                video.Token = token;
            Locator.VideoPlayerVm.CurrentVideo = video;
            await PlaylistHelper.Play(video);
        }
        
        private async void UpdateTime(Int64 time)
        {
            await UpdateTimeFromUIThread();
        }

        private async Task UpdateTimeFromUIThread()
        {
            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Low, () =>
            {
                OnPropertyChanged(nameof(Time));
                // Assume position also changes when time does.
                // We could/should also watch OnPositionChanged event, but let's save us
                // the cost of another dispatched call.
                OnPropertyChanged(nameof(Position));
            });
        }

        public async Task CleanViewModel()
        {
            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, async () =>
            {
                _mediaService.Stop();
                PlayingType = PlayingType.NotPlaying;
                IsPlaying = false;
                TimeTotal = TimeSpan.Zero;
#if WINDOWS_PHONE_APP
                try
                {
                    // music clean
                    if (BackgroundAudioHelper.Instance?.CurrentState != MediaPlayerState.Stopped)
                    {
                        BackgroundAudioHelper.Instance?.Pause();
                        await App.BackgroundAudioHelper.ResetCollection(ResetType.NormalReset);
                    }
                }
                catch
                {
                }
#endif
                await TrackCollection.ResetCollection();
                TrackCollection.IsRunning = false;
            });
        }
        
        public async void OnLengthChanged(Int64 length)
        {
            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (length < 0) return;
                TimeTotal = TimeSpan.FromMilliseconds(length);
            });
        }

        private async void OnStopped(IMediaService mediaService)
        {
            Debug.WriteLine("OnStopped event called from " + mediaService);
            mediaService.MediaFailed -= _mediaService_MediaFailed;
            mediaService.StatusChanged -= PlayerStateChanged;
            mediaService.TimeChanged -= UpdateTime;

            mediaService.OnLengthChanged -= OnLengthChanged;
            mediaService.OnStopped -= OnStopped;
            mediaService.OnEndReached -= OnEndReached;
            mediaService.OnBuffering -= MediaServiceOnOnBuffering;

            if (mediaService is VLCService)
            {
                var vlcService = (VLCService)mediaService;
                var em = vlcService.MediaPlayer.eventManager();
                em.OnTrackAdded -= OnTrackAdded;
                em.OnTrackDeleted -= OnTrackDeleted;

                _audioTracks.Clear();
                _subtitlesTracks.Clear();
                await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
                {
                    CurrentAudioTrack = null;
                    CurrentSubtitle = null;
                    OnPropertyChanged(nameof(AudioTracks));
                    OnPropertyChanged(nameof(Subtitles));
                    OnPropertyChanged(nameof(CurrentAudioTrack));
                    OnPropertyChanged(nameof(CurrentSubtitle));
                });
            }
            else if (mediaService is MFService)
            {
                var mfService = (MFService)mediaService;
                mfService.Instance.Source = null;
            }
            mediaService.SetNullMediaPlayer();
        }

        public Task SetMedia(IMediaItem media, bool forceVlcLib = false, bool autoPlay = true)
        {
            return Task.Run(async () =>
            {
                if (media == null)
                    throw new ArgumentNullException(nameof(media), "Media parameter is missing. Can't play anything");
                Stop();
                UseVlcLib = forceVlcLib;

                if (media is VideoItem)
                {
                    // First things first: we need to pause the slideshow here before any action is done by VLC
                    await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        PlayingType = PlayingType.Video;
                        IsStream = false;
                        if (Locator.NavigationService.CurrentPage == VLCPage.VideoPlayerPage)
                        {
                            Locator.NavigationService.GoBack_Default();
                        }
                        Locator.NavigationService.Go(VLCPage.VideoPlayerPage);
                    });
                    var video = (VideoItem)media;
                    await InitializePlayback(video, autoPlay);
                    await Locator.VideoPlayerVm.TryUseSubtitleFromFolder();

                    if (video.TimeWatched != TimeSpan.FromSeconds(0))
                        await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () => Locator.MediaPlaybackViewModel.Time = (Int64)video.TimeWatched.TotalMilliseconds);
#if WINDOWS_PHONE_APP
                    try
                    {
                        var messageDictionary = new ValueSet();
                        messageDictionary.Add(BackgroundAudioConstants.ClearUVC, "");
                        BackgroundMediaPlayer.SendMessageToBackground(messageDictionary);
                    }
                    catch
                    {
                    }
#else
                    await SetMediaTransportControlsInfo(string.IsNullOrEmpty(video.Name) ? Strings.Video : video.Name);
#endif
                    UpdateTileHelper.UpdateMediumTileWithVideoInfo();
                }
                else if (media is TrackItem)
                {
                    await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        IsStream = false;
                        PlayingType = PlayingType.Music;
                    });
                    var track = (TrackItem)media;
                    StorageFile currentTrackFile;
                    try
                    {
                        currentTrackFile = track.File ?? await StorageFile.GetFileFromPathAsync(track.Path);
                    }
                    catch (Exception exception)
                    {
                        await Locator.MediaLibrary.RemoveTrackFromCollectionAndDatabase(track);
                        await Task.Delay(500);

                        if (TrackCollection.CanGoNext)
                        {
                            await PlayNext();
                        }
                        else
                        {
                            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Low, () => Locator.NavigationService.GoBack_Specific());
                        }
                        return;
                    }
                    await InitializePlayback(track, autoPlay);
                    if (_playerEngine != PlayerEngine.BackgroundMFPlayer)
                    {
                        await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            await Locator.MusicPlayerVM.SetCurrentArtist();
                            await Locator.MusicPlayerVM.SetCurrentAlbum();
                            await Locator.MusicPlayerVM.UpdatePlayingUI();
                            await Locator.MusicPlayerVM.Scrobble();
#if WINDOWS_PHONE_APP
#else
                            await Locator.MusicPlayerVM.UpdateWindows8UI();
#endif
                            if (Locator.MusicPlayerVM.CurrentArtist != null)
                            {
                                Locator.MusicPlayerVM.CurrentArtist.PlayCount++;
                                await Locator.MediaLibrary.Update(Locator.MusicPlayerVM.CurrentArtist);
                            }
                        });
                    }
                    ApplicationSettingsHelper.SaveSettingsValue(BackgroundAudioConstants.CurrentTrack, TrackCollection.CurrentTrack);
                }
                else if (media is StreamMedia)
                {
                    UseVlcLib = true;
                    await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        Locator.VideoPlayerVm.CurrentVideo = null;
                        Locator.MediaPlaybackViewModel.PlayingType = PlayingType.Video;
                        IsStream = true;
                    });
                    await InitializePlayback(media, autoPlay);
                }
            });
        }

        public async Task InitializePlayback(IMediaItem media, bool autoPlay)
        {
            // First set the player engine
            // For videos AND music, we have to try first with Microsoft own player
            // Then we register to Failed callback. If it doesn't work, we set ForceVlcLib to true
            if (UseVlcLib)
                _playerEngine = PlayerEngine.VLC;
            else
            {
                var path = "";
                if (!string.IsNullOrEmpty(media.Path))
                    path = Path.GetExtension(media.Path);
                else if (media.File != null)
                    path = media.File.FileType;
                if (media is TrackItem)
                {
                    if (VLCFileExtensions.MFSupported.Contains(path.ToLower()))
                    {
#if WINDOWS_PHONE_APP
                        _playerEngine = PlayerEngine.BackgroundMFPlayer;
#else
                        _playerEngine = PlayerEngine.VLC;
#endif
                    }
                    else
                    {
#if WINDOWS_PHONE_APP
                        ToastHelper.Basic(Strings.FailFilePlayBackground, false, "background");
#endif
                        _playerEngine = PlayerEngine.VLC;
                        _mediaService.Stop();
                    }
                }
                else
                {
                    _playerEngine = PlayerEngine.VLC;
                }
            }
            
            _mediaService.MediaFailed += _mediaService_MediaFailed;
            _mediaService.StatusChanged += PlayerStateChanged;
            _mediaService.TimeChanged += UpdateTime;

            if (autoPlay)
            {
                // Reset the libVLC log file
                await LogHelper.ResetBackendFile();
            }

            // Send the media we want to play
            await _mediaService.SetMediaFile(media);

            _mediaService.OnLengthChanged += OnLengthChanged;
            _mediaService.OnStopped += OnStopped;
            _mediaService.OnEndReached += OnEndReached;
            _mediaService.OnBuffering += MediaServiceOnOnBuffering;

            switch (_playerEngine)
            {
                case PlayerEngine.VLC:
                    var vlcService = (VLCService)_mediaService;
                    if (vlcService.MediaPlayer == null) return;
                    var em = vlcService.MediaPlayer.eventManager();
                    em.OnTrackAdded += Locator.MediaPlaybackViewModel.OnTrackAdded;
                    em.OnTrackDeleted += Locator.MediaPlaybackViewModel.OnTrackDeleted;
                    var mem = vlcService.MediaPlayer.media().eventManager();
                    mem.OnParsedStatus += Mem_OnParsedStatus;
                    if (!autoPlay) return;
                    vlcService.Play();
                    break;
                case PlayerEngine.MediaFoundation:
                    var mfService = (MFService)_mediaService;
                    if (mfService == null) return;

                    if (!autoPlay) return;
                    _mediaService.Play();
                    break;
                case PlayerEngine.BackgroundMFPlayer:
                    if (!autoPlay) return;
                    _mediaService.Play(CurrentMedia.Id);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () => SpeedRate = 100);
        }

        private async void MediaServiceOnOnBuffering(int f)
        {
            _bufferingProgress = f;
            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
            {
                IsBuffered = f == 100;
                OnPropertyChanged(nameof(BufferingProgress));
            });
        }

        async void _mediaService_MediaFailed(object sender, EventArgs e)
        {
            if (sender is MFService)
            {
                // MediaFoundation failed to open the media, switching to VLC player
                await SetMedia(CurrentMedia, true);
            }
            else
            {
                await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    if (!Locator.MainVM.IsInternet && IsStream)
                    {
#if WINDOWS_UWP
                        await DialogHelper.DisplayDialog(Strings.ConnectionLostPleaseCheck, Strings.Sorry);
#else
                        var lostStreamDialog = new MessageDialog(Strings.ConnectionLostPleaseCheck, Strings.Sorry);
                        await lostStreamDialog.ShowAsyncQueue();
#endif
                    }
                    // ensure we call Stop so we unregister all events
                    Stop();
                });
            }
        }


        async void OnEndReached()
        {
            switch (PlayingType)
            {
                case PlayingType.Music:
                    break;
                case PlayingType.Video:
                    await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Low, () =>
                    {
                        if (Locator.VideoPlayerVm.CurrentVideo != null)
                            Locator.VideoPlayerVm.CurrentVideo.TimeWatchedSeconds = 0;
                    });
                    if (Locator.VideoPlayerVm.CurrentVideo != null)
                        await Locator.MediaLibrary.UpdateVideo(Locator.VideoPlayerVm.CurrentVideo).ConfigureAwait(false);
                    break;
                case PlayingType.NotPlaying:
                    break;
                default:
                    break;
            }

            bool canGoNext = TrackCollection.Playlist.Count > 0 && TrackCollection.CanGoNext;
            if (!canGoNext)
            {
                // Playlist is finished
                if (TrackCollection.Repeat)
                {
                    // ... One More Time!
                    await StartAgain();
                    return;
                }
                await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Low, () =>
                {
                    App.RootPage.StopCompositionAnimationOnSwapChain();
                    TrackCollection.IsRunning = false;
                    IsPlaying = false;
                    PlayingType = PlayingType.NotPlaying;
                    if (!Locator.NavigationService.GoBack_Default())
                    {
                        Locator.NavigationService.Go(Locator.SettingsVM.HomePage);
                    }
                });
            }
            else
            {
                await PlayNext();
            }
        }

        public async Task StartAgain()
        {
            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, async () =>
            {
                TrackCollection.CurrentTrack = 0;
                await Locator.MediaPlaybackViewModel.SetMedia(CurrentMedia, false);
            });
        }
        
        public async Task PlayNext()
        {
            if (TrackCollection.CanGoNext)
            {
                await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    TrackCollection.CurrentTrack++;
                    await Locator.MediaPlaybackViewModel.SetMedia(CurrentMedia, false);
                });
            }
            else
            {
                UpdateTileHelper.ClearTile();
            }
        }

        public async Task PlayPrevious()
        {
            if (TrackCollection.CanGoPrevious)
            {
                await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () => TrackCollection.CurrentTrack--);
                await Locator.MediaPlaybackViewModel.SetMedia(CurrentMedia, false);
            }
            else
            {
                UpdateTileHelper.ClearTile();
            }
        }

        public void SetSizeVideoPlayer(uint x, uint y)
        {
            _mediaService.SetSizeVideoPlayer(x, y);
        }

        public async Task UpdatePosition()
        {
            if (Locator.VideoPlayerVm.CurrentVideo != null)
            {
                Locator.VideoPlayerVm.CurrentVideo.TimeWatchedSeconds = (int)((double)Time / 1000); ;
                await Locator.MediaLibrary.UpdateVideo(Locator.VideoPlayerVm.CurrentVideo).ConfigureAwait(false);
            }
        }

        public void SetSubtitleTrack(int i)
        {
            switch (_playerEngine)
            {
                case PlayerEngine.VLC:
                    var vlcService = (VLCService)_mediaService;
                    vlcService.SetSubtitleTrack(i);
                    break;
                case PlayerEngine.MediaFoundation:
                    break;
                case PlayerEngine.BackgroundMFPlayer:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SetAudioTrack(int i)
        {
            switch (_playerEngine)
            {
                case PlayerEngine.VLC:
                    var vlcService = (VLCService)_mediaService;
                    vlcService.SetAudioTrack(i);
                    break;
                case PlayerEngine.MediaFoundation:
                    break;
                case PlayerEngine.BackgroundMFPlayer:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void OpenSubtitleMrl(string mrl)
        {
            switch (_playerEngine)
            {
                case PlayerEngine.VLC:
                    var vlcService = (VLCService)_mediaService;
                    vlcService.SetSubtitleFile(mrl);
                    break;
                case PlayerEngine.MediaFoundation:
                    break;
                case PlayerEngine.BackgroundMFPlayer:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SetRate(float rate)
        {
            switch (_playerEngine)
            {
                case PlayerEngine.VLC:
                    _mediaService.SetSpeedRate(rate);
                    break;
                case PlayerEngine.MediaFoundation:
                    _mediaService.SetSpeedRate(rate);
                    break;
                case PlayerEngine.BackgroundMFPlayer:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Stop()
        {
            _mediaService.Stop();
        }
        #endregion

        #region Events
        private async void PlayerStateChanged(object sender, MediaState e)
        {
            try
            {
                await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Low, () =>
                {
                    IsPlaying = e == MediaState.Playing || e == MediaState.Buffering;
                    MediaState = e;

                    switch (MediaState)
                    {
                        case MediaState.NothingSpecial:
                            break;
                        case MediaState.Opening:
                            break;
                        case MediaState.Buffering:
                            break;
                        case MediaState.Playing:
                            if (_systemMediaTransportControls != null)
                                _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                            break;
                        case MediaState.Paused:
                            if (_systemMediaTransportControls != null)
                                _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                            break;
                        case MediaState.Stopped:
                            break;
                        case MediaState.Ended:
                            break;
                        case MediaState.Error:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });
            }
            catch { }
        }

        public async void OnTrackAdded(TrackType type, int trackId)
        {
            if (type == TrackType.Unknown || type == TrackType.Video)
                return;
            List<DictionaryKeyValue> target;
            IList<TrackDescription> source;
            if (type == TrackType.Audio)
            {
                target = _audioTracks;
                source = ((VLCService)_mediaService).MediaPlayer?.audioTrackDescription();
            }
            else
            {
                target = _subtitlesTracks;
                source = ((VLCService)_mediaService).MediaPlayer?.spuDescription();
            }

            target?.Clear();
            foreach (var t in source)
            {
                target?.Add(new DictionaryKeyValue()
                {
                    Id = t.id(),
                    Name = t.name(),
                });
            }

            // This assumes we have a "Disable" track for both subtitles & audio
            if (type == TrackType.Subtitle && CurrentSubtitle == null && _subtitlesTracks?.Count > 1)
            {
                _currentSubtitle = 1;
                await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () => OnPropertyChanged("CurrentSubtitle"));
            }
            else if (type == TrackType.Audio && CurrentAudioTrack == null && _audioTracks?.Count > 1)
            {
                _currentAudioTrack = 1;
                await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () => OnPropertyChanged("CurrentAudioTrack"));
            }
        }

        public async void OnTrackDeleted(TrackType type, int trackId)
        {
            if (type == TrackType.Unknown || type == TrackType.Video)
                return;
            List<DictionaryKeyValue> target;
            if (type == TrackType.Audio)
                target = _audioTracks;
            else
                target = _subtitlesTracks;

            target.RemoveAll((t) => t.Id == trackId);
            if (target.Count > 0)
                return;
            if (type == TrackType.Subtitle)
            {
                _currentSubtitle = -1;
                await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () => OnPropertyChanged("CurrentSubtitle"));
            }
            else
            {
                _currentAudioTrack = -1;
                await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () => OnPropertyChanged("CurrentAudioTrack"));
            }
        }

        private async void Mem_OnParsedStatus(ParseStatus parsedStatus)
        {
            if (parsedStatus != ParseStatus.Done)
                return;

            if (!(_mediaService is VLCService)) return;
            var vlcService = (VLCService)_mediaService;
            var mP = vlcService?.MediaPlayer;
            // Get chapters
            var chapters = mP?.chapterDescription(-1);
            foreach (var c in chapters)
            {
                var vlcChapter = new VLCChapterDescription(c);
                _chapters.Add(vlcChapter);
            }
            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
            {
                OnPropertyChanged(nameof(Chapters));
                OnPropertyChanged(nameof(CurrentChapter));
            });

            // Get subtitle delay etc
            if (mP != null)
            {
                _audioDelay = mP.audioDelay();
                _spuDelay = mP.spuDelay();
            }
        }

        public async void UpdateCurrentChapter()
        {
            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
            {
                OnPropertyChanged(nameof(CurrentChapter));
            });
        }
        #endregion

        #region MediaTransportControls

        public void SetMediaTransportControls(SystemMediaTransportControls systemMediaTransportControls)
        {
#if WINDOWS_PHONE_APP
            if (BackgroundAudioHelper.Instance?.CurrentState == MediaPlayerState.Playing)
            {

            }
            else
            {
                ForceMediaTransportControls(systemMediaTransportControls);
            }
#else
            ForceMediaTransportControls(systemMediaTransportControls);
#endif
        }

        void ForceMediaTransportControls(SystemMediaTransportControls systemMediaTransportControls)
        {
            try
            {
                _systemMediaTransportControls = systemMediaTransportControls;
                if (_systemMediaTransportControls != null)
                {
                    _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Closed;
                    _systemMediaTransportControls.ButtonPressed += SystemMediaTransportControlsOnButtonPressed;
                    _systemMediaTransportControls.IsEnabled = false;
                }
            }
            catch (Exception exception)
            { }
        }

        public async Task SetMediaTransportControlsInfo(string artistName, string albumName, string trackName, string albumUri)
        {
            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
            {
                try
                {
                    if (_systemMediaTransportControls == null) return;
                    _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                    _systemMediaTransportControls.IsEnabled = true;
                    _systemMediaTransportControls.IsPauseEnabled = true;
                    _systemMediaTransportControls.IsPlayEnabled = true;

                    SystemMediaTransportControlsDisplayUpdater updater = _systemMediaTransportControls.DisplayUpdater;
                    updater.Type = MediaPlaybackType.Music;
                    // Music metadata.
                    updater.MusicProperties.AlbumArtist = artistName;
                    updater.MusicProperties.Artist = artistName;
                    updater.MusicProperties.Title = trackName;

                    // Set the album art thumbnail.
                    // RandomAccessStreamReference is defined in Windows.Storage.Streams

                    if (albumUri != null && !string.IsNullOrEmpty(albumUri))
                    {
                        updater.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(albumUri));
                    }

                    // Update the system media transport controls.
                    updater.Update();
                }
                catch (Exception exception)
                { }
            });
        }

        public async Task SetMediaTransportControlsInfo(string title)
        {
            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
            {
                try
                {
                    if (_systemMediaTransportControls == null) return;
                    LogHelper.Log("PLAYVIDEO: Updating SystemMediaTransportControls");
                    SystemMediaTransportControlsDisplayUpdater updater = _systemMediaTransportControls.DisplayUpdater;
                    updater.Type = MediaPlaybackType.Video;
                    _systemMediaTransportControls.IsPreviousEnabled = false;
                    _systemMediaTransportControls.IsNextEnabled = false;
                    //Video metadata
                    updater.VideoProperties.Title = title;
                    //TODO: add full thumbnail suport
                    updater.Thumbnail = null;
                    updater.Update();
                }
                catch (Exception e)
                {
                    LogHelper.Log(StringsHelper.ExceptionToString(e));
                }
            });
        }

        private async void SystemMediaTransportControlsOnButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Pause:
                case SystemMediaTransportControlsButton.Play:
                    _mediaService.Pause();
                    break;
                case SystemMediaTransportControlsButton.Stop:
                    Stop();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    if (Locator.MediaPlaybackViewModel.PlayingType == PlayingType.Music)
                        await Locator.MediaPlaybackViewModel.PlayPrevious();
                    break;
                case SystemMediaTransportControlsButton.Next:
                    if (Locator.MediaPlaybackViewModel.PlayingType == PlayingType.Music)
                        await Locator.MediaPlaybackViewModel.PlayNext();
                    break;
            }
        }

        public void SystemMediaTransportControlsBackPossible(bool backPossible)
        {
            try
            {
                if (_systemMediaTransportControls != null) _systemMediaTransportControls.IsPreviousEnabled = backPossible;
            }
            catch { }
        }

        public void SystemMediaTransportControlsNextPossible(bool nextPossible)
        {
            try
            {
                if (_systemMediaTransportControls != null) _systemMediaTransportControls.IsNextEnabled = nextPossible;
            }
            catch
            {
            }
        }
        #endregion

        public void Pause()
        {
            try
            {
                _mediaService.Pause();
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            _mediaService.Stop();
        }
    }
}
