﻿using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using VLC.Helpers;
using VLC.Model;
using VLC.Utils;
using VLC.ViewModels;
using WinRTXamlToolkit.Controls.Extensions;

namespace VLC.UI.Legacy.Views.VariousPages
{
    public sealed partial class FeedbackPage
    {
        public FeedbackPage()
        {
            this.InitializeComponent();
        }

        private void SendFeedback_Click(object sender, RoutedEventArgs e)
        {
            var fbItem = new Feedback();

            if (InsiderCheckBox.IsChecked.HasValue && InsiderCheckBox.IsChecked.Value)
            {
                if (string.IsNullOrEmpty(BuildNumberTextBox.Text))
                {
                    StatusTextBox.Text = Utils.Strings.SpecifyBuild;
                    return;
                }
                int buildN;
                if (int.TryParse(BuildNumberTextBox.Text, out buildN) && buildN > 10000 && buildN < 11000) // UGLY but should do the trick for now
                {
                    fbItem.PlatformBuild = buildN;
                }
                else
                {
                    StatusTextBox.Text = Utils.Strings.SpecifiedBuildIncorrect;
                    return;
                }
            }

            fbItem.Comment = DetailsTextBox.Text;
            fbItem.Summary = SummaryTextBox.Text;

            var sendLogs = SendLogsCheckBox.IsChecked.HasValue && SendLogsCheckBox.IsChecked.Value;
            StatusTextBox.Text = Utils.Strings.SendingFeedback;
            ProgressRing.IsActive = true;
            Task.Run(() => SendFeedbackItem(fbItem, sendLogs));
        }

        public async Task SendFeedbackItem(Feedback fb, bool sendLogs)
        {
            try
            {
                var result = await LogHelper.SendFeedback(fb, sendLogs);

                await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    if (result.EnsureSuccessStatusCode().IsSuccessStatusCode)
                    {
                        Locator.NavigationService.Go(VLCPage.SettingsPage);
                        ToastHelper.Basic(Utils.Strings.FeedbackThankYou);
                    }
                    else
                    {
                        StatusTextBox.Text = Utils.Strings.ErrorSendingFeedback;
                        ProgressRing.IsActive = false;
#if DEBUG
                        var md = new MessageDialog(result.ReasonPhrase + " - " + result.Content + " - " + result.StatusCode, "Bug in the Request");
                        await md.ShowQueuedAsync();
#endif
                    }
                });
            }
            catch (Exception e)
            {
                await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    StatusTextBox.Text = Utils.Strings.ErrorSendingFeedback;
                    ProgressRing.IsActive = false;
#if DEBUG
                    var md = new MessageDialog(e.ToString(), "Bug");
                    await md.ShowQueuedAsync();
#endif
                });
            }
        }

        private void InsiderCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            BuildNumberTextBox.Visibility = Visibility.Visible;
        }

        private void InsiderCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            BuildNumberTextBox.Visibility = Visibility.Collapsed;
        }
    }
}
