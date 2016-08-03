﻿using System.Threading.Tasks;
using VLC.ViewModels;
using Windows.UI.Xaml.Controls;

namespace VLC.UI.Legacy.Views.MusicPages.TagEditorPages
{
    public sealed partial class TrackEditorPage : Page
    {
        public TrackEditorPage()
        {
            this.InitializeComponent();
        }

        public async Task SaveChanges()
        {
            if (TrackNameTextBox.Text != Locator.MusicLibraryVM.CurrentTrack.Name)
            {
                if (!string.IsNullOrEmpty(TrackNameTextBox.Text))
                    Locator.MusicLibraryVM.CurrentTrack.Name = TrackNameTextBox.Text;

                await Locator.MediaLibrary.Update(Locator.MusicLibraryVM.CurrentTrack);
            }

            Locator.NavigationService.GoBack_Specific();
        }

        private async void SaveChanges_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            await SaveChanges();
        }
    }
}
