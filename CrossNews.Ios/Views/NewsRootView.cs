﻿using System;
using CrossNews.Core.Extensions;
using CrossNews.Core.ViewModels;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Platforms.Ios.Views;
using UIKit;

namespace CrossNews.Ios.Views
{
    [MvxRootPresentation(WrapInNavigationController = true)]
    public class NewsRootView : MvxTabBarViewController<NewsRootViewModel>
    {
        private bool _loaded;
        private UIViewController _lastSelected = null;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.BackgroundColor = UIColor.White;

            var settingsIcon = UIImage.FromBundle("SettingsIcon");
            var settingsButton = new UIBarButtonItem { Image = settingsIcon };
            settingsButton.Clicked += (sender, args) => ViewModel.ShowSettingsCommand.TryExecute();
            NavigationItem.RightBarButtonItem = settingsButton;
        }

        private void OnViewControllerSelected(object sender, UITabBarSelectionEventArgs e)
        {
            if (e.ViewController == _lastSelected && e.ViewController is IScrollableView scrollable)
            {
                scrollable.ScrollToTop();
            }

            _lastSelected = e.ViewController;
            return;
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            if (ViewModel == null || _loaded)
            {
                return;
            }

            _loaded = true;
            ViewControllerSelected += OnViewControllerSelected;
            ViewModel.ShowInitialViewModelsCommand.TryExecute();
        }

        protected override void SetTitleAndTabBarItem(UIViewController viewController, MvxTabPresentationAttribute attribute)
        {
            base.SetTitleAndTabBarItem(viewController, attribute);
            if (string.IsNullOrEmpty(Title))
            {
                Title = attribute.TabName;
            }
        }

        public override void ItemSelected(UITabBar tabbar, UITabBarItem item) => Title = item.Title;
    }
}
