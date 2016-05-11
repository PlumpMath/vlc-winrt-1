﻿using System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using VLC_WinRT.Helpers;

namespace VLC_WinRT.Controls
{
    public delegate void FlyoutCloseRequested(object sender, EventArgs e);
    public delegate void RightSidebarNavigated(object sender, EventArgs p);
    public delegate void RightSidebarClosed(object sender, EventArgs e);
    public delegate void ContentSizeChanged(double newWidth);
    
    [TemplatePart(Name = ContentPresenterName, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = FlyoutContentPresenterName, Type = typeof(Frame))]
    [TemplatePart(Name = FlyoutFadeInName, Type = typeof(Storyboard))]
    [TemplatePart(Name = FlyoutFadeOutName, Type = typeof(Storyboard))]
    [TemplatePart(Name = FlyoutPlaneProjectionName, Type = typeof(PlaneProjection))]
    [TemplatePart(Name = FlyoutGridContainerName, Type = typeof(Grid))]
    [TemplatePart(Name = FlyoutBackgroundGridName, Type = typeof(Grid))]
    [TemplatePart(Name = FooterContentPresenterName, Type = typeof(ContentPresenter))]
    public sealed class SplitShell : Control
    {
        public event FlyoutCloseRequested FlyoutCloseRequested;
        public event RightSidebarNavigated RightSidebarNavigated;
        public event RightSidebarClosed RightSidebarClosed;
        public event ContentSizeChanged ContentSizeChanged;
        public TaskCompletionSource<bool> TemplateApplied = new TaskCompletionSource<bool>();
        
        private DispatcherTimer _windowResizerTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };

        private const string ContentPresenterName = "ContentPresenter";
        private const string FlyoutContentPresenterName = "FlyoutContentPresenter";
        private const string FlyoutFadeInName = "FlyoutFadeIn";
        private const string FlyoutFadeOutName = "FlyoutFadeOut";
        private const string TopBarFadeOutName = "TopBarFadeOut";
        private const string TopBarFadeInName = "TopBarFadeIn";
        private const string FlyoutPlaneProjectionName = "FlyoutPlaneProjection";
        private const string FlyoutGridContainerName = "FlyoutGridContainer";
        private const string FlyoutBackgroundGridName = "FlyoutBackgroundGrid";
        private const string FooterContentPresenterName = "FooterContentPresenter";

        private Grid _flyoutGridContainer;
        private Grid _flyoutBackgroundGrid;
        private ContentPresenter _contentPresenter;
        private Frame _flyoutContentPresenter;
        private ContentPresenter _footerContentPresenter;

        private PlaneProjection _flyoutPlaneProjection;
        private Storyboard _flyoutFadeIn;
        private Storyboard _flyoutFadeOut;
        private Storyboard _topBarFadeOut;
        private Storyboard _topBarFadeIn;

        public async void SetContentPresenter(object contentPresenter)
        {
            await TemplateApplied.Task;
            _contentPresenter.Content = contentPresenter;
        }
        
        public async void SetRightPaneContentPresenter(object content)
        {
            await TemplateApplied.Task;
            _flyoutContentPresenter.Navigate((Type)content);
            ShowFlyout();
        }

        public async void SetFooterContentPresenter(object content)
        {
            await TemplateApplied.Task;
            _footerContentPresenter.Content = content;
        }
        public async void SetFooterVisibility(object visibility)
        {
            await TemplateApplied.Task;
            _footerContentPresenter.Visibility = (Visibility)visibility;
        }

        #region Content Property
        public DependencyObject Content
        {
            get { return (DependencyObject)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            "Content", typeof(DependencyObject), typeof(SplitShell), new PropertyMetadata(default(DependencyObject), ContentPropertyChangedCallback));


        private static void ContentPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var that = (SplitShell)dependencyObject;
            that.SetContentPresenter(dependencyPropertyChangedEventArgs.NewValue);
        }
        #endregion

        #region RightPaneContent Property

        public Type FlyoutContent
        {
            get { return (Type)GetValue(FlyoutContentProperty); }
            set { SetValue(FlyoutContentProperty, value); }
        }

        public static readonly DependencyProperty FlyoutContentProperty = DependencyProperty.Register(
            nameof(FlyoutContent), typeof(Type), typeof(SplitShell),
            new PropertyMetadata(default(Type), FlyoutContentPropertyChangedCallback));

        private static void FlyoutContentPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var that = (SplitShell)dependencyObject;
            that.SetRightPaneContentPresenter(dependencyPropertyChangedEventArgs.NewValue);
        }
        #endregion

        #region FooterContent Property

        public Visibility FooterVisibility
        {
            get { return (Visibility)GetValue(FooterVisibilityProperty); }
            set { SetValue(FooterVisibilityProperty, value); }
        }

        public static readonly DependencyProperty FooterVisibilityProperty = DependencyProperty.Register(
            "FooterVisibility", typeof(Visibility), typeof(SplitShell), new PropertyMetadata(Visibility.Visible, FooterVisibilityPropertyChangedCallback));

        private static void FooterVisibilityPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var that = (SplitShell)dependencyObject;
            that.SetFooterVisibility(dependencyPropertyChangedEventArgs.NewValue);
        }

        public DependencyObject FooterContent
        {
            get { return (DependencyObject)GetValue(FooterContentProperty); }
            set { SetValue(FooterContentProperty, value); }
        }

        public static readonly DependencyProperty FooterContentProperty = DependencyProperty.Register(
            "FooterContent", typeof(DependencyObject), typeof(SplitShell),
            new PropertyMetadata(default(DependencyObject), FooterContentPropertyChangedCallback));

        private static void FooterContentPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var that = (SplitShell)dependencyObject;
            that.SetFooterContentPresenter(dependencyPropertyChangedEventArgs.NewValue);
        }
        #endregion
        
        public SplitShell()
        {
            DefaultStyleKey = typeof(SplitShell);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _contentPresenter = (ContentPresenter)GetTemplateChild(ContentPresenterName);
            _flyoutContentPresenter = (Frame)GetTemplateChild(FlyoutContentPresenterName);
            _flyoutFadeIn = (Storyboard)GetTemplateChild(FlyoutFadeInName);
            _flyoutFadeOut = (Storyboard)GetTemplateChild(FlyoutFadeOutName);
            _topBarFadeOut = (Storyboard)GetTemplateChild(TopBarFadeOutName);
            _topBarFadeIn = (Storyboard)GetTemplateChild(TopBarFadeInName);
            _flyoutPlaneProjection = (PlaneProjection)GetTemplateChild(FlyoutPlaneProjectionName);
            _flyoutGridContainer = (Grid)GetTemplateChild(FlyoutGridContainerName);
            _flyoutBackgroundGrid = (Grid)GetTemplateChild(FlyoutBackgroundGridName);
            _footerContentPresenter = (ContentPresenter)GetTemplateChild(FooterContentPresenterName);

            Responsive();
            Window.Current.SizeChanged += Current_SizeChanged;
            _contentPresenter.Width = Window.Current.Bounds.Width;

            TemplateApplied.SetResult(true);

            _flyoutGridContainer.Visibility = Visibility.Collapsed;
            if (_flyoutBackgroundGrid != null)
            _flyoutBackgroundGrid.Tapped += FlyoutGridContainerOnTapped;

            _windowResizerTimer.Tick += _windowResizerTimer_Tick;

            _flyoutFadeOut.Completed += _flyoutFadeOut_Completed;
            _flyoutFadeIn.Completed += _flyoutFadeIn_Completed;

            _topBarFadeIn.Completed += _topBarFadeIn_Completed;
        }

        private void _flyoutFadeIn_Completed(object sender, object e)
        {
            RightSidebarNavigated?.Invoke(null, new EventArgs());
        }

        private void _flyoutFadeOut_Completed(object sender, object e)
        {
            _flyoutContentPresenter.Navigate(typeof(UI.Legacy.Views.UserControls.Shell.BlankPage));
        }
        
        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            Responsive();
        }

        void Responsive()
        {
            if (Window.Current.Bounds.Width < 650)
            {
                _flyoutContentPresenter.Height = Window.Current.Bounds.Height;
                _flyoutContentPresenter.Width = Window.Current.Bounds.Width;
            }
            else
            {
                _flyoutContentPresenter.Width = 650;
                _flyoutContentPresenter.Height = 
                    Window.Current.Bounds.Height < 900 * 0.7 ? Window.Current.Bounds.Height : Window.Current.Bounds.Height * 0.7;
            }
            _windowResizerTimer.Stop();
            _windowResizerTimer.Start();
        }

        private void _windowResizerTimer_Tick(object sender, object e)
        {
            _contentPresenter.Width = Window.Current.Bounds.Width;
            _windowResizerTimer.Stop();
            ContentSizeChanged?.Invoke(_contentPresenter.Width);
        }

        private void FlyoutGridContainerOnTapped(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            FlyoutCloseRequested?.Invoke(null, new EventArgs());
        }

        void ShowFlyout()
        {
            _flyoutFadeIn.Begin();
            IsFlyoutOpen = true;
        }

        public void HideFlyout()
        {
            _flyoutFadeOut.Begin();
            IsFlyoutOpen = false;
            RightSidebarClosed?.Invoke(null, new EventArgs());
        }

        public void HideTopBar()
        {
            _topBarFadeOut.Begin();
            _contentPresenter.Margin = new Thickness(0, 0, 0, - _footerContentPresenter.ActualHeight);
            IsTopBarOpen = false;
        }

        public void ShowTopBar()
        {
            _topBarFadeIn.Begin();
            IsTopBarOpen = true;
        }
        
        private void _topBarFadeIn_Completed(object sender, object e)
        {
            _contentPresenter.Margin = new Thickness(0);
        }

        public bool IsFlyoutOpen { get; private set; }
        public bool IsTopBarOpen { get; set; }
    }
}
