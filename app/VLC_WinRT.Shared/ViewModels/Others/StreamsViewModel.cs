﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;
using VLC_WinRT.Database;
using VLC_WinRT.Model.Stream;
using VLC_WinRT.Utils;
using VLC_WinRT.Commands;
using Windows.UI.Xaml;
using Autofac;
using VLC_WinRT.Services.RunTime;

namespace VLC_WinRT.ViewModels.Others
{
    public class StreamsViewModel : BindableBase, IDisposable
    {
        private Visibility _noInternetPlaceholderEnabled = Visibility.Collapsed;
        
        public IEnumerable<IGrouping<string, StreamMedia>> StreamsHistoryAndFavoritesGrouped
        {
            get { return Locator.MediaLibrary.Streams?.GroupBy(x => x.Id.ToString()); }
        }

        public bool IsCollectionEmpty
        {
            get { return !Locator.MediaLibrary.Streams.Any(); }
        }

        public Visibility NoInternetPlaceholderEnabled
        {
            get { return _noInternetPlaceholderEnabled; }
            set { SetProperty(ref _noInternetPlaceholderEnabled, value); }
        }

        public PlayNetworkMRLCommand PlayStreamCommand { get; } = new PlayNetworkMRLCommand();

        public void OnNavigatedTo()
        {
            Task.Run(() => Initialize());
        }

        public void OnNavigatedFrom()
        {
            Dispose();
        }

        public async Task Initialize()
        {
            App.Container.Resolve<NetworkListenerService>().InternetConnectionChanged += StreamsViewModel_InternetConnectionChanged;
            Locator.MediaLibrary.Streams.CollectionChanged += Streams_CollectionChanged;
            await Locator.MediaLibrary.LoadStreamsFromDatabase();
        }

        private async void StreamsViewModel_InternetConnectionChanged(object sender, Model.Events.InternetConnectionChangedEventArgs e)
        {
            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, () => NoInternetPlaceholderEnabled = e.IsConnected ? Visibility.Collapsed : Visibility.Visible);
        }

        private async void Streams_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Low, () =>
            {
                OnPropertyChanged(nameof(StreamsHistoryAndFavoritesGrouped));
                OnPropertyChanged(nameof(IsCollectionEmpty));
            });
        }

        public void Dispose()
        {
            Locator.MediaLibrary.Streams.CollectionChanged -= Streams_CollectionChanged;
            App.Container.Resolve<NetworkListenerService>().InternetConnectionChanged -= StreamsViewModel_InternetConnectionChanged;
        }
    }
}
