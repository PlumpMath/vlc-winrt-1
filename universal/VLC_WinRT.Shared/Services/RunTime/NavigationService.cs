﻿using System;
using VLC_WinRT.Model;
using VLC_WinRT.ViewModels;
using VLC_WinRT.Views.MainPages;
using VLC_WinRT.Views.MusicPages;
using VLC_WinRT.Views.MusicPages.AlbumPageControls;
using VLC_WinRT.Views.MusicPages.ArtistPageControls;
using VLC_WinRT.Views.MusicPages.ArtistPages;
using VLC_WinRT.Views.MusicPages.PlaylistControls;
using VLC_WinRT.Views.VariousPages;
using VLC_WinRT.Views.VideoPages;
#if WINDOWS_PHONE_APP
using Windows.Phone.UI.Input;
#endif

namespace VLC_WinRT.Services.RunTime
{
    public class NavigationService
    {
        public VLCPage CurrentPage;
        public bool PreventAppExit { get; set; } = false;
        public delegate void Navigated(object sender, VLCPage newPage);
        public Navigated ViewNavigated = delegate { };
        public NavigationService()
        {
#if WINDOWS_PHONE_APP
            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
#endif    
        }

#if WINDOWS_PHONE_APP
        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            e.Handled = true;
            if(CurrentPage == VLCPage.MainPageHome)
                e.Handled = false;
            GoBack_Specific();
        }
#endif

        public void GoBack_Specific()
        {
            switch (CurrentPage)
            {
                case VLCPage.MainPageHome:
                    break;
                case VLCPage.MainPageVideo:
                    break;
                case VLCPage.MainPageMusic:
                    break;
                case VLCPage.MainPageFileExplorer:
                    break;
                case VLCPage.AlbumPage:
                    GoBack_HideFlyout();
                    break;
                case VLCPage.ArtistPage:
                    GoBack_HideFlyout();
                    break;
                case VLCPage.PlaylistPage:
                    GoBack_Default();
                    break;
                case VLCPage.CurrentPlaylistPage:
                    GoBack_Default();
                    break;
                case VLCPage.VideoPlayerPage:
                    Locator.MediaPlaybackViewModel.GoBack.Execute(null);
                    break;
                case VLCPage.MusicPlayerPage:
                    GoBack_Default();
                    break;
                case VLCPage.SettingsPage:
                    GoBack_Default();
                    break;
                case VLCPage.SpecialThanksPage:
                    GoBack_Default();
                    break;
                case VLCPage.ArtistShowsPage:
                    GoBack_HideFlyout();
                    break;
                case VLCPage.AddAlbumToPlaylistDialog:
                    GoBack_HideFlyout();
                    break;
                case VLCPage.CreateNewPlaylistDialog:
                    GoBack_HideFlyout();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool CanGoBack()
        {
            if (isFlyout(CurrentPage))
                return true;
            if (IsCurrentPageAMainPage())
                return false;
            return App.ApplicationFrame.CanGoBack;
        }

        // Returns false if it can't go back
        public bool GoBack_Default()
        {
            bool canGoBack = CanGoBack();
            if (canGoBack)
            {
                App.ApplicationFrame.GoBack();
                ViewNavigated(null, CurrentPage);
            }
            return canGoBack;
        }

        public void GoBack_HideFlyout()
        {
            App.RootPage.SplitShell.HideFlyout();
            // Restoring the currentPage
            CurrentPage = PageTypeToVLCPage(App.ApplicationFrame.CurrentSourcePageType);
            ViewNavigated(null, CurrentPage);
        }

        public void Go(VLCPage desiredPage)
        {
            if (!isFlyout(desiredPage) && desiredPage == CurrentPage) return;
            switch (desiredPage)
            {
                case VLCPage.MainPageHome:
                    App.ApplicationFrame.Navigate(typeof(MainPageHome));
                    break;
                case VLCPage.MainPageVideo:
                    App.ApplicationFrame.Navigate(typeof(MainPageVideos));
                    break;
                case VLCPage.MainPageMusic:
                    App.ApplicationFrame.Navigate(typeof(MainPageMusic));
                    break;
                case VLCPage.MainPageFileExplorer:
                    App.ApplicationFrame.Navigate(typeof(MainPageRemovables));
                    break;
                case VLCPage.AlbumPage:
                    App.RootPage.SplitShell.RightFlyoutContent = new AlbumPageBase();
                    break;
                case VLCPage.ArtistPage:
                    App.RootPage.SplitShell.RightFlyoutContent = new ArtistPageBase();
                    break;
                case VLCPage.PlaylistPage:
                    App.ApplicationFrame.Navigate(typeof(PlaylistPage));
                    break;
                case VLCPage.CurrentPlaylistPage:
                    App.ApplicationFrame.Navigate(typeof(MusicPlaylistPage));
                    break;
                case VLCPage.VideoPlayerPage:
                    App.ApplicationFrame.Navigate(typeof(VideoPlayerPage));
                    break;
                case VLCPage.MusicPlayerPage:
                    App.ApplicationFrame.Navigate(typeof(MusicPlayerPage));
                    break;
                case VLCPage.SettingsPage:
                    App.ApplicationFrame.Navigate(typeof(SettingsPage));
                    break;
                case VLCPage.SpecialThanksPage:
                    App.ApplicationFrame.Navigate(typeof(SpecialThanks));
                    break;
                case VLCPage.ArtistShowsPage:
                    App.RootPage.SplitShell.RightFlyoutContent = new ArtistShowsPage();
                    break;
                case VLCPage.AddAlbumToPlaylistDialog:
                    var addToPlaylist = new AddAlbumToPlaylistBase();
                    App.RootPage.SplitShell.RightFlyoutContent = addToPlaylist;
                    break;
                case VLCPage.CreateNewPlaylistDialog:
                    var createPlaylist = new CreateNewPlaylist();
                    App.RootPage.SplitShell.RightFlyoutContent = createPlaylist;
                    break;
                default:
                    break;
            }
            if (isFlyout(desiredPage))
                CurrentPage = desiredPage;
            ViewNavigated(null, CurrentPage);
        }

        bool isFlyout(VLCPage page)
        {
            return page == VLCPage.AlbumPage ||
                page == VLCPage.ArtistPage ||
                page == VLCPage.AddAlbumToPlaylistDialog ||
                page == VLCPage.CreateNewPlaylistDialog ||
                page == VLCPage.ArtistShowsPage;
        }

        /// <summary>
        /// This callback is fired for Pages only, not flyouts
        /// </summary>
        public void PageNavigatedCallback(Type page)
        {
            CurrentPage = PageTypeToVLCPage(page);
        }

        VLCPage PageTypeToVLCPage(Type page)
        {
            if (page == typeof(MainPageHome))
                return VLCPage.MainPageHome;
            if (page == typeof(MainPageVideos))
                return VLCPage.MainPageVideo;
            if (page == typeof(MainPageMusic))
                return VLCPage.MainPageMusic;
            if (page == typeof(MainPageRemovables))
                return VLCPage.MainPageFileExplorer;
            if (page == typeof(PlaylistPage))
                return VLCPage.PlaylistPage;
            if (page == typeof(MusicPlaylistPage))
                return VLCPage.CurrentPlaylistPage;
            if (page == typeof(VideoPlayerPage))
                return VLCPage.VideoPlayerPage;
            if (page == typeof(MusicPlayerPage))
                return VLCPage.MusicPlayerPage;
            if (page == typeof(SettingsPage))
                return VLCPage.SettingsPage;
            if (page == typeof(SpecialThanks))
                return VLCPage.SpecialThanksPage;
            if (page == typeof(ArtistShowsPage))
                return VLCPage.ArtistShowsPage;
            return VLCPage.None;
        }

        public bool IsCurrentPageAMainPage()
        {
            return CurrentPage == VLCPage.MainPageHome
                || CurrentPage == VLCPage.MainPageVideo
                || CurrentPage == VLCPage.MainPageMusic
                || CurrentPage == VLCPage.MainPageFileExplorer;
        }
    }
}
