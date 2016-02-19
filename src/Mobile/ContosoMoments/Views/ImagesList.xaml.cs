﻿using ContosoMoments.Models;
using ContosoMoments.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using System.ComponentModel;

namespace ContosoMoments.Views
{
    public partial class ImagesList : ContentPage
    {
        public User User { get; set; }
        public Album Album { get; set; }

        private App _app;
        private ImagesListViewModel viewModel;

        public ImagesList(App app)
        {
            InitializeComponent();

            _app = app;
            viewModel = new ImagesListViewModel(App.MobileService, _app);

            BindingContext = viewModel;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;

            var tapUploadImage = new TapGestureRecognizer();
            tapUploadImage.Tapped += OnAddImage;
            imgUpload.GestureRecognizers.Add(tapUploadImage);

            var tapSyncImage = new TapGestureRecognizer();
            tapSyncImage.Tapped += OnSyncItems;
            imgSync.GestureRecognizers.Add(tapSyncImage);
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ErrorMessage" && viewModel.ErrorMessage != null) {
                DisplayAlert("Error occurred", viewModel.ErrorMessage, "Close");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (imagesList.ItemsSource == null) {
                using (var scope = new ActivityIndicatorScope(syncIndicator, true)) {
                    viewModel.User = User;
                    viewModel.Album = Album;
                    await LoadItems();
                }
            }
        }

        private async void OnAddImage(object sender, EventArgs e)
        {
            using (var scope = new ActivityIndicatorScope(syncIndicator, true)) {
                try {
                    IPlatform platform = DependencyService.Get<IPlatform>();
                    string sourceImagePath = await platform.TakePhotoAsync(App.UIContext);

                    if (sourceImagePath != null) {
                        var image = await _app.AddImage(viewModel.User, viewModel.Album, sourceImagePath);
                        await SyncItemsAsync(true, refreshView: false);
                        viewModel.Images.Add(image);
                    }

                }
                catch (Exception) {
                    await DisplayAlert("Upload failed", "Image upload failed. Please try again later", "Ok");
                }
            }
        }

        private async Task LoadItems()
        {
            await viewModel.LoadImagesAsync(viewModel.Album.AlbumId);
        }

        public async void OnRefresh(object sender, EventArgs e)
        {
            var success = false;
            try {
                await SyncItemsAsync(true, refreshView: true);
                success = true;
            }
            catch (Exception ex) {
                await DisplayAlert("Refresh Error", "Couldn't refresh data (" + ex.Message + ")", "OK");
            }
            imagesList.EndRefresh();

            if (!success)
                await DisplayAlert("Refresh Error", "Couldn't refresh data", "OK");

        }

        public async void OnSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var selectedImage = e.SelectedItem as ContosoMoments.Models.Image;

            if (selectedImage != null) {
                var detailsVM = new ImageDetailsViewModel(App.MobileService, selectedImage);
                var detailsView = new ImageDetailsView();
                detailsVM.Album = viewModel.Album;
                detailsVM.User = viewModel.User;
                detailsView.BindingContext = detailsVM;

                await Navigation.PushAsync(detailsView);
            }

            // prevents background getting highlighted
            imagesList.SelectedItem = null;
        }

        public async void OnSyncItems(object sender, EventArgs e)
        {
            await SyncItemsAsync(false, refreshView: true);
        }

        private async Task SyncItemsAsync(bool showActivityIndicator, bool refreshView)
        {
            using (var scope = new ActivityIndicatorScope(syncIndicator, showActivityIndicator)) {
                if (Utils.IsOnline() && await Utils.SiteIsOnline()) {
                    await _app.SyncAsync();
                }
                else {
                    await DisplayAlert("Working Offline", "Couldn't sync data - device is offline or Web API is not available. Please try again when data connection is back", "OK");
                }

                if (refreshView) {
                    await LoadItems();
                }
            }
        }

        public async void OnDelete(object sender, EventArgs e)
        {
            var res = await DisplayAlert("Delete image?", "Delete selected image?", "Yes", "No");

            if (res) {
                var selectedImage = (sender as MenuItem).BindingContext as ContosoMoments.Models.Image;

                try {
                    await viewModel.DeleteImageAsync(selectedImage);
                    OnRefresh(sender, e);
                }
                catch (Exception) {
                    await DisplayAlert("Delete error", "Couldn't delete the image. Please try again later.", "OK");
                }
            }
        }

        private class ActivityIndicatorScope : IDisposable
        {
            private bool showIndicator;
            private ActivityIndicator indicator;
            private Task indicatorDelay;

            public ActivityIndicatorScope(ActivityIndicator indicator, bool showIndicator)
            {
                this.indicator = indicator;
                this.showIndicator = showIndicator;

                if (showIndicator) {
                    indicatorDelay = Task.Delay(2000);
                    SetIndicatorActivity(true);
                }
                else {
                    indicatorDelay = Task.FromResult(0);
                }
            }

            private void SetIndicatorActivity(bool isActive)
            {
                this.indicator.IsVisible = isActive;
                this.indicator.IsRunning = isActive;
            }

            public void Dispose()
            {
                if (showIndicator) {
                    indicatorDelay.ContinueWith(t => SetIndicatorActivity(false), TaskScheduler.FromCurrentSynchronizationContext());
                }
            }
        }
    }
}
