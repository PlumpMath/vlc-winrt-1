﻿using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using VLC_WinRT.Model.Video;
using VLC_WinRT.ViewModels;


namespace VLC_WinRT.Views.MainPages
{
    public sealed partial class MainPageHome : Page
    {
        public MainPageHome()
        {
            InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
            this.SizeChanged += OnSizeChanged;
            Loaded += OnLoaded;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Locator.Slideshow.GoDefaultPic();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            Locator.Slideshow.RestoreSlideshow();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            Responsive();
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            Responsive();
        }
        
        private void Responsive()
        {
        }
        
        private void AlbumsWrapGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            TemplateSizer.ComputeAlbums(sender as ItemsWrapGrid, AlbumsListView.ActualWidth);
        }

        private void VideoWrapGrid_Loaded(object sender, RoutedEventArgs e)
        {
            itemsWrapGrid = sender as ItemsWrapGrid;
            Window.Current.SizeChanged += Current_SizeChanged;
        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            Debug.WriteLine(VideosListView.ActualWidth);
            //TemplateSizer.ComputeCompactVideo(itemsWrapGrid, VideosListView.ActualWidth);

        }

        private void VideoWrapGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            TemplateSizer.ComputeCompactVideo(sender as ItemsWrapGrid, VideosListView.ActualWidth);
        }
    }
}