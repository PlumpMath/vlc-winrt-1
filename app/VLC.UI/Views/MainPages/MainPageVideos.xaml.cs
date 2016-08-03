﻿/**********************************************************************
 * VLC for WinRT
 **********************************************************************
 * Copyright © 2013-2014 VideoLAN and Authors
 *
 * Licensed under GPLv2+ and MPLv2
 * Refer to COPYING file of the official project for license
 **********************************************************************/

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using VLC.ViewModels;
using Microsoft.Xaml.Interactivity;
using VLC.Model.Video;
using VLC.Views.MainPages.MainVideoControls;
using VLC.ViewModels.VideoVM;

namespace VLC.Views.MainPages
{
    public sealed partial class MainPageVideos : Page
    {
        public MainPageVideos()
        {
            InitializeComponent();
            this.Loaded += MainPageMusic_Loaded;
        }

        void MainPageMusic_Loaded(object sender, RoutedEventArgs e)
        {
            Responsive(Window.Current.Bounds.Width);
            Locator.VideoLibraryVM.OnNavigatedTo();
            Window.Current.SizeChanged += Current_SizeChanged;
            this.Unloaded += AlbumsCollectionButtons_Unloaded;
        }

        void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            Responsive(e.Size.Width);
        }

        void AlbumsCollectionButtons_Unloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged -= Current_SizeChanged;
            Locator.VideoLibraryVM.OnNavigatedFrom();
        }

        void Responsive(double width)
        {
        }

        private void MainPageVideoContentPresenter_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (MainPageVideoContentPresenter.Content == null)
            {
                Switch(Locator.VideoLibraryVM.VideoView);
            }
            Locator.VideoLibraryVM.PropertyChanged += VideoLibraryVM_PropertyChanged;
        }

        private void VideoLibraryVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VideoLibraryVM.VideoView))
            {
                Switch(Locator.VideoLibraryVM.VideoView);
            }
        }

        void Switch(VideoView view)
        {
            PanelsListView.SelectedItem = view;
            switch (view)
            {
                case VideoView.Videos:
                    if (!(MainPageVideoContentPresenter.Content is AllVideosPivotItem))
                        MainPageVideoContentPresenter.Content = new AllVideosPivotItem();
                    break;
                case VideoView.Shows:
                    if (!(MainPageVideoContentPresenter.Content is ShowsPivotItem))
                        MainPageVideoContentPresenter.Content = new ShowsPivotItem();
                    break;
                case VideoView.CameraRoll:
                    if (!(MainPageVideoContentPresenter.Content is CameraRollPivotItem))
                        MainPageVideoContentPresenter.Content = new CameraRollPivotItem();
                    break;
            }
        }

        private void PanelsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems == null || e.AddedItems.Count == 0)
                return;
            var view = (VideoView)e.AddedItems[0];
            Locator.VideoLibraryVM.VideoView = view;
        }
    }
}