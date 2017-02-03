using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using FFIList.WindowsPhone.Common;
using MvvmCross.WindowsCommon.Views;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace FFIList.WindowsPhone.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class StartView : MvxWindowsPage
    {
        public StartView()
        {
            this.InitializeComponent();
            NavigationHelper = new NavigationHelper(this);
        }

        public NavigationHelper NavigationHelper { get; }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            NavigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            NavigationHelper.OnNavigatedFrom(e);
        }
    }
}
