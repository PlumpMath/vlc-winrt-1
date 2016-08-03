﻿using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using VLC.Model.Video;
using VLC.ViewModels;

namespace VLC.Views.MainPages.MusicPanes
{
    public sealed partial class AlbumCollectionBase : UserControl
    {
        public AlbumCollectionBase()
        {
            this.InitializeComponent();
            this.Loaded += AlbumCollectionBase_Loaded;
            this.Unloaded += AlbumCollectionBase_Unloaded;
            this.SizeChanged += AlbumCollectionBase_SizeChanged;
        }

        private void AlbumCollectionBase_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (AlbumsZoomedInView.ItemsPanelRoot == null) return;
            TemplateSizer.ComputeAlbums(AlbumsZoomedInView.ItemsPanelRoot as ItemsWrapGrid, AlbumsZoomedInView.ItemsPanelRoot.ActualWidth - 6);
        }

        private async void AlbumCollectionBase_Unloaded(object sender, RoutedEventArgs e)
        {
            await Locator.MusicLibraryVM.OnNavigatedFromAlbums();
        }

        private void AlbumCollectionBase_Loaded(object sender, RoutedEventArgs e)
        {
            Locator.MusicLibraryVM.OnNavigatedToAlbums();
        }
    }
}
