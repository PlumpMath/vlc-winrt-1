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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using VLC_WinRT.Database;
using VLC_WinRT.Helpers;
using VLC_WinRT.Helpers.MusicLibrary;
using VLC_WinRT.Model;
using VLC_WinRT.Model.Music;
using VLC_WinRT.Commands.MusicLibrary;
using VLC_WinRT.Model.Search;
using VLC_WinRT.Utils;
using VLC_WinRT.Commands.MusicPlayer;
using Windows.UI.Xaml;
using VLC_WinRT.Model.Library;
using VLC_WinRT.Commands.MediaLibrary;

namespace VLC_WinRT.ViewModels.MusicVM
{
    public class MusicLibraryVM : BindableBase
    {
        #region private fields
        private ObservableCollection<GroupItemList<ArtistItem>> _groupedArtists;
        private ObservableCollection<ArtistItem> _topArtists = new ObservableCollection<ArtistItem>();
        private ObservableCollection<ArtistItem> _recommendedArtists = new ObservableCollection<ArtistItem>(); // recommanded with MusicFlow

        private ObservableCollection<PlaylistItem> _trackCollections = new ObservableCollection<PlaylistItem>();

        private ObservableCollection<GroupItemList<AlbumItem>> _groupedAlbums;
        private List<AlbumItem> _recommendedAlbums = new List<AlbumItem>();

        #endregion
        #region private props
        private LoadingState _loadingStateArtists = LoadingState.NotLoaded;
        private LoadingState _loadingStateAlbums = LoadingState.NotLoaded;
        private LoadingState _loadingStateTracks = LoadingState.NotLoaded;
        private LoadingState _loadingStatePlaylists = LoadingState.NotLoaded;

        private ArtistItem _focusOnAnArtist; // recommended with MusicFlow
        private TrackItem _currentMedia;
        private AlbumItem _currentAlbum;
        private ArtistItem _currentArtist;
        private PlaylistItem _currentMediaCollection;
        private bool _isLoaded = false;
        private bool _isBusy = false;
        private MusicView _musicView;
        #endregion

        #region public fields
        public List<MusicView> MusicViewCollection { get; set; } = new List<MusicView>()
        {
            MusicView.Artists,
            MusicView.Albums,
            MusicView.Songs,
            MusicView.Playlists
        };

        public ObservableCollection<PlaylistItem> TrackCollections
        {
            get { return Locator.MediaLibrary.TrackCollections; }
        }
        
        public List<AlbumItem> RecommendedAlbums
        {
            get { return _recommendedAlbums; }
            set { SetProperty(ref _recommendedAlbums, value); }
        }

        public ObservableCollection<ArtistItem> TopArtists
        {
            get { return _topArtists; }
            set { SetProperty(ref _topArtists, value); }
        }

        public ObservableCollection<ArtistItem> RecommendedArtists
        {
            get { return _recommendedArtists; }
            set { SetProperty(ref _recommendedArtists, value); }
        }

        public ObservableCollection<GroupItemList<ArtistItem>> GroupedArtists
        {
            get { return _groupedArtists; }
            set { SetProperty(ref _groupedArtists, value); }
        }

        public IEnumerable<IGrouping<char, TrackItem>> GroupedTracks
        {
            get { return Locator.MediaLibrary.OrderTracks(); }
        }

        public ObservableCollection<GroupItemList<AlbumItem>> GroupedAlbums
        {
            get { return _groupedAlbums; }
            set { SetProperty(ref _groupedAlbums, value); }
        }
        #endregion
        #region public props


        public MusicView MusicView
        {
            get
            {
                var musicView = ApplicationSettingsHelper.ReadSettingsValue(nameof(MusicView), false);
                if (musicView == null)
                {
                    _musicView = MusicView.Albums;
                }
                else
                {
                    _musicView = (MusicView)musicView;
                }
                return _musicView;
            }
            set
            {
                ApplicationSettingsHelper.SaveSettingsValue(nameof(MusicView), (int)value, false);
                SetProperty(ref _musicView, value);
                OnPropertyChanged(nameof(AlbumsCollectionsButtonVisible));
                OnPropertyChanged(nameof(ShuffleButtonVisible));
                OnPropertyChanged(nameof(PlaylistButtonsVisible));
            }
        }

        public Visibility AlbumsCollectionsButtonVisible { get { return MusicView == MusicView.Albums ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility ShuffleButtonVisible
        {
            get
            {
                return (MusicView == MusicView.Albums || MusicView == MusicView.Artists || MusicView == MusicView.Songs) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility PlaylistButtonsVisible
        {
            get
            {
                return MusicView == MusicView.Playlists ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility MusicLibraryEmptyVisible => IsMusicLibraryEmpty ? Visibility.Visible : Visibility.Collapsed;

        public LoadingState LoadingStateArtists
        {
            get { return _loadingStateArtists; }
            set { SetProperty(ref _loadingStateArtists, value); }
        }
        public LoadingState LoadingStateAlbums
        {
            get { return _loadingStateAlbums; }
            set { SetProperty(ref _loadingStateAlbums, value); }
        }
        public LoadingState LoadingStateTracks
        {
            get { return _loadingStateTracks; }
            set { SetProperty(ref _loadingStateTracks, value); }
        }
        public LoadingState LoadingStatePlaylists
        {
            get { return _loadingStatePlaylists; }
            set { SetProperty(ref _loadingStatePlaylists, value); }
        }

        public bool IsLoaded
        {
            get { return _isLoaded; }
            set { SetProperty(ref _isLoaded, value); }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            set { SetProperty(ref _isBusy, value); }
        }

        public bool IsMusicLibraryEmpty => (LoadingStateArtists == LoadingState.Loaded && Locator.MediaLibrary.Artists?.Count == 0)
                                        || (LoadingStateAlbums == LoadingState.Loaded && Locator.MediaLibrary.Albums?.Count == 0)
                                        || (LoadingStateTracks == LoadingState.Loaded && Locator.MediaLibrary.Tracks?.Count == 0);

        public IndexMediaLibraryCommand IndexMediaLibraryCommand { get; private set; } = new IndexMediaLibraryCommand();

        public AddToPlaylistCommand AddToPlaylistCommand { get; private set; } = new AddToPlaylistCommand();

        public TrackCollectionClickedCommand TrackCollectionClickedCommand { get; private set; } = new TrackCollectionClickedCommand();

        public ShowCreateNewPlaylistPane ShowCreateNewPlaylistPaneCommand { get; private set; } = new ShowCreateNewPlaylistPane();

        public ChangeAlbumArtCommand ChangeAlbumArtCommand { get; private set; } = new ChangeAlbumArtCommand();

        public DownloadAlbumArtCommand DownloadAlbumArtCommand { get; private set; } = new DownloadAlbumArtCommand();

        public AlbumClickedCommand AlbumClickedCommand { get; private set; } = new AlbumClickedCommand();

        public ArtistClickedCommand ArtistClickedCommand { get; private set; } = new ArtistClickedCommand();

        public ResetCurrentArtistAndAlbumCommand ResetCurrentArtistAndAlbumCommand { get; private set; } = new ResetCurrentArtistAndAlbumCommand();

        public PlayArtistAlbumsCommand PlayArtistAlbumsCommand { get; private set; } = new PlayArtistAlbumsCommand();

        public PlayAlbumCommand PlayAlbumCommand { get; private set; } = new PlayAlbumCommand();

        public TrackClickedCommand TrackClickedCommand { get; private set; } = new TrackClickedCommand();

        public AlbumTrackClickedCommand AlbumTrackClickedCommand { get; private set; } = new AlbumTrackClickedCommand();

        public PlayAllRandomCommand PlayAllRandomCommand { get; private set; } = new PlayAllRandomCommand();

        public PlayAllSongsCommand PlayAllSongsCommand { get; private set; } = new PlayAllSongsCommand();

        public OpenAddAlbumToPlaylistDialog OpenAddAlbumToPlaylistDialogCommand { get; private set; } = new OpenAddAlbumToPlaylistDialog();

        public BingLocationShowCommand BingLocationShowCommand { get; private set; } = new BingLocationShowCommand();

        public DeletePlaylistCommand DeletePlaylistCommand { get; private set; } = new DeletePlaylistCommand();

        public DeletePlaylistTrackCommand DeletePlaylistTrackCommand { get; private set; } = new DeletePlaylistTrackCommand();

        public DeleteSelectedTracksInPlaylistCommand DeleteSelectedTracksInPlaylistCommand { get; private set; } = new DeleteSelectedTracksInPlaylistCommand();

        public StartTrackMetaEdit StartTrackMetaEdit { get; private set; } = new StartTrackMetaEdit();

        public SetAlbumViewOrderCommand SetAlbumViewOrder { get; private set; } = new SetAlbumViewOrderCommand();

        public ArtistItem FocusOnAnArtist // Music Flow recommandation
        {
            get { return _focusOnAnArtist; }
            set { SetProperty(ref _focusOnAnArtist, value); }
        }

        public ArtistItem CurrentArtist
        {
            get { return _currentArtist; }
            set
            {
                SetProperty(ref _currentArtist, value);
                OnPropertyChanged(nameof(IsCurrentArtistExist));
            }
        }

        public AlbumItem CurrentAlbum
        {
            get { return _currentAlbum; }
            set
            {
                SetProperty(ref _currentAlbum, value);
                OnPropertyChanged(nameof(CurrentArtist));
            }
        }

        public TrackItem CurrentTrack
        {
            get { return _currentMedia; }
            set { SetProperty(ref _currentMedia, value); }
        }

        public PlaylistItem CurrentTrackCollection
        {
            get { return _currentMediaCollection; }
            set { SetProperty(ref _currentMediaCollection, value); }
        }
        public bool IsCurrentArtistExist
        {
            get { return _currentArtist != null; }
        }
        #endregion

        public void ResetLibrary()
        {
            LoadingStateAlbums = LoadingState.NotLoaded;
            LoadingStateArtists = LoadingState.NotLoaded;
            LoadingStateTracks = LoadingState.NotLoaded;
            LoadingStatePlaylists = LoadingState.NotLoaded;

            RecommendedArtists?.Clear();
            RecommendedArtists = new ObservableCollection<ArtistItem>();

            RecommendedAlbums?.Clear();
            RecommendedAlbums = new List<AlbumItem>();
        }

        public void OnNavigatedTo()
        {
            ResetLibrary();
        }

        public void OnNavigatedToArtists()
        {
            if (LoadingStateArtists == LoadingState.NotLoaded && GroupedArtists == null)
            {
                InitializeArtists();
            }
            else
            {
                OnPropertyChanged(nameof(IsMusicLibraryEmpty));
                OnPropertyChanged(nameof(MusicLibraryEmptyVisible));
            }
        }

        public void OnNavigatedToAlbums()
        {
            if (LoadingStateAlbums == LoadingState.NotLoaded)
            {
                InitializeAlbums();
            }
        }

        public void OnNavigatedToTracks()
        {
            if (LoadingStateTracks == LoadingState.NotLoaded)
            {
                InitializeTracks();
            }
        }

        public void OnNavigatedToPlaylists()
        {
            if (LoadingStatePlaylists == LoadingState.NotLoaded)
            {
                InitializePlaylists();
            }
        }

        public void OnNavigatedFrom()
        {
            ResetLibrary();
        }

        public Task OnNavigatedFromArtists()
        {
            if (Locator.MediaLibrary.Artists != null)
            {
                Locator.MediaLibrary.Artists.CollectionChanged -= Artists_CollectionChanged;
                Locator.MediaLibrary.Artists.Clear();
            }

            return DispatchHelper.InvokeAsync(CoreDispatcherPriority.Low, () =>
            {
                GroupedArtists = null;
                LoadingStateArtists = LoadingState.NotLoaded;
            });
        }

        public Task OnNavigatedFromAlbums()
        {
            if (Locator.MediaLibrary.Albums != null)
            {
                Locator.MediaLibrary.Albums.CollectionChanged -= Albums_CollectionChanged;
                Locator.MediaLibrary.Albums.Clear();
            }

            RecommendedAlbums?.Clear();

            return DispatchHelper.InvokeAsync(CoreDispatcherPriority.Low, () =>
            {
                GroupedAlbums = null;
                LoadingStateAlbums = LoadingState.NotLoaded;
            });
        }

        Task InitializeAlbums()
        {
            return Task.Run(async () =>
            {
                await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Locator.MainVM.InformationText = Strings.LoadingMusic;
                    LoadingStateAlbums = LoadingState.Loading;
                    GroupedAlbums = new ObservableCollection<GroupItemList<AlbumItem>>();
                });

                if (Locator.MediaLibrary.Albums != null)
                    Locator.MediaLibrary.Albums.CollectionChanged += Albums_CollectionChanged;
                await Locator.MediaLibrary.LoadAlbumsFromDatabase();
                await RefreshRecommendedAlbums();
                await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Locator.MainVM.InformationText = String.Empty;
                    LoadingStateAlbums = LoadingState.Loaded;
                    OnPropertyChanged(nameof(IsMusicLibraryEmpty));
                    OnPropertyChanged(nameof(MusicLibraryEmptyVisible));
                });
            });
        }

        public async Task RefreshRecommendedAlbums()
        {
            if (MusicView != MusicView.Albums)
                return;
            var recommendedAlbums = await Locator.MediaLibrary.LoadRecommendedAlbumsFromDatabase();
            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
            {
                RecommendedAlbums = recommendedAlbums;
            });
        }

        private async void Albums_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (Locator.MediaLibrary.Albums?.Count == 0 || Locator.MediaLibrary.Albums?.Count == 1)
                {
                    await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        OnPropertyChanged(nameof(IsMusicLibraryEmpty));
                        OnPropertyChanged(nameof(MusicLibraryEmptyVisible));
                    });
                }

                if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Count > 0)
                {
                    foreach (var newItem in e.NewItems)
                    {
                        var album = (AlbumItem)newItem;
                        await InsertIntoGroupAlbum(album);
                    }
                }
                else
                    await OrderAlbums();
            }
            catch { }
        }

        public async Task OrderAlbums()
        {
            _groupedAlbums = Locator.MediaLibrary.OrderAlbums(Locator.SettingsVM.AlbumsOrderType, Locator.SettingsVM.AlbumsOrderListing);
            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Low, () =>
            {
                OnPropertyChanged(nameof(GroupedAlbums));
            });
        }

        Task InitializeArtists()
        {
            return Task.Run(async () =>
            {
                await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Locator.MainVM.InformationText = Strings.LoadingMusic;
                    LoadingStateArtists = LoadingState.Loading;
                    GroupedArtists = new ObservableCollection<GroupItemList<ArtistItem>>();
                });

                if (Locator.MediaLibrary.Artists != null)
                    Locator.MediaLibrary.Artists.CollectionChanged += Artists_CollectionChanged;
                await Locator.MediaLibrary.LoadArtistsFromDatabase();
                var recommendedArtists = await Locator.MediaLibrary.LoadRandomArtistsFromDatabase();
                await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
                {
                    RecommendedArtists = recommendedArtists;

                    Locator.MainVM.InformationText = String.Empty;
                    LoadingStateArtists = LoadingState.Loaded;
                    OnPropertyChanged(nameof(IsMusicLibraryEmpty));
                    OnPropertyChanged(nameof(MusicLibraryEmptyVisible));
                });
            });
        }

        private async void Artists_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Locator.MediaLibrary.Artists?.Count == 0 || Locator.MediaLibrary.Artists?.Count == 1)
            {
                await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
                {
                    OnPropertyChanged(nameof(IsMusicLibraryEmpty));
                    OnPropertyChanged(nameof(MusicLibraryEmptyVisible));
                });
            }

            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Count > 0)
            {
                foreach (var newItem in e.NewItems)
                {
                    var artist = (ArtistItem)newItem;
                    await InsertIntoGroupArtist(artist);
                }
            }
            else
                await OrderArtists();
        }

        async Task OrderArtists()
        {
            _groupedArtists = Locator.MediaLibrary.OrderArtists();
            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Low, () =>
            {
                OnPropertyChanged(nameof(GroupedArtists));
            });
        }

        Task InitializeTracks()
        {
            return Task.Run(async () =>
            {
                await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Locator.MainVM.InformationText = Strings.LoadingMusic;
                    LoadingStateTracks = LoadingState.Loading;
                });

                await Locator.MediaLibrary.LoadTracksFromDatabase();
                await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Locator.MainVM.InformationText = String.Empty;
                    LoadingStateTracks = LoadingState.Loaded;
                    OnPropertyChanged(nameof(IsMusicLibraryEmpty));
                    OnPropertyChanged(nameof(MusicLibraryEmptyVisible));
                });
                await OrderTracks();
            });
        }

        async Task OrderTracks()
        {
            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
            {
                OnPropertyChanged(nameof(GroupedTracks));
            });
        }

        Task InitializePlaylists()
        {
            return Task.Run(async () =>
            {
                await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Locator.MainVM.InformationText = Strings.LoadingMusic;
                    LoadingStatePlaylists = LoadingState.Loading;
                });

                await Locator.MediaLibrary.LoadPlaylistsFromDatabase();
                await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () =>
                {
                    OnPropertyChanged(nameof(TrackCollections));

                    Locator.MainVM.InformationText = String.Empty;
                    LoadingStatePlaylists = LoadingState.Loaded;
                });
            });
        }
        #region methods            

        async Task InsertIntoGroupAlbum(AlbumItem album)
        {
            try
            {
                if (Locator.SettingsVM.AlbumsOrderType == OrderType.ByArtist)
                {
                    var artist = GroupedAlbums.FirstOrDefault(x => (string)x.Key == Strings.HumanizedArtistName(album.Artist));
                    if (artist == null)
                    {
                        artist = new GroupItemList<AlbumItem>(album) { Key = Strings.HumanizedArtistName(album.Artist) };
                        int i = GroupedAlbums.IndexOf(GroupedAlbums.LastOrDefault(x => string.Compare((string)x.Key, (string)artist.Key, StringComparison.OrdinalIgnoreCase) < 0));
                        i++;
                        await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Low, () => GroupedAlbums?.Insert(i, artist));
                    }
                    else await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Low, () => artist.Add(album));
                }
                else if (Locator.SettingsVM.AlbumsOrderType == OrderType.ByDate)
                {
                    var year = GroupedAlbums.FirstOrDefault(x => (string)x.Key == Strings.HumanizedYear(album.Year));
                    if (year == null)
                    {
                        var newyear = new GroupItemList<AlbumItem>(album) { Key = Strings.HumanizedYear(album.Year) };
                        int i = GroupedAlbums.IndexOf(GroupedAlbums.LastOrDefault(x => string.Compare((string)x.Key, (string)newyear.Key) < 0));
                        i++;
                        await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Low, () => GroupedAlbums?.Insert(i, newyear));
                    }
                    else await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Low, () => year.Add(album));
                }
                else if (Locator.SettingsVM.AlbumsOrderType == OrderType.ByAlbum)
                {
                    var firstChar = GroupedAlbums.FirstOrDefault(x => (string)x.Key == Strings.HumanizedAlbumFirstLetter(album.Name));
                    if (firstChar == null)
                    {
                        var newChar = new GroupItemList<AlbumItem>(album) { Key = Strings.HumanizedAlbumFirstLetter(album.Name) };
                        int i = GroupedAlbums.IndexOf(GroupedAlbums.LastOrDefault(x => string.Compare((string)x.Key, (string)newChar.Key) < 0));
                        i++;
                        await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Low, () => GroupedAlbums?.Insert(i, newChar));
                    }
                    else await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Low, () => firstChar.Add(album));
                }
            }
            catch { }
        }

        async Task InsertIntoGroupArtist(ArtistItem artist)
        {
            try
            {
                var supposedFirstChar = Strings.HumanizedArtistFirstLetter(artist.Name);
                var firstChar = GroupedArtists.FirstOrDefault(x => (string)x.Key == supposedFirstChar);
                if (firstChar == null)
                {
                    var newChar = new GroupItemList<ArtistItem>(artist) { Key = supposedFirstChar };
                    if (GroupedArtists == null)
                        return;
                    int i = GroupedArtists.IndexOf(GroupedArtists.LastOrDefault(x => string.Compare((string)x.Key, (string)newChar.Key) < 0));
                    i++;
                    await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () => GroupedArtists.Insert(i, newChar));
                }
                else
                {
                    await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () => firstChar.Add(artist));
                }
            }
            catch { }
        }
        #endregion
    }
}
