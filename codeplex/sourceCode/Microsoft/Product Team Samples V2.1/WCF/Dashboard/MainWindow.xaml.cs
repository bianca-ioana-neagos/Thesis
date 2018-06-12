// *********************************************************
//
//  Copyright (c) Microsoft. All rights reserved.
//  This code is licensed under the Apache 2.0 License.
//  THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OR
//  CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
//  INCLUDING, WITHOUT LIMITATION, ANY IMPLIED WARRANTIES
//  OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
//  PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
// *********************************************************

namespace WpfWcfDashboard
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.ServiceModel;
    using System.Threading;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Shapes;
    using EventTypes;
    using WpfWcfDashboard.ServiceReference1;
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IWCFObserverCallback
    {
        Polyline myPolyline;
        Brush[] colors = {Brushes.Green, Brushes.DarkOrange, Brushes.Red};
        WCFObserverClient _client;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void PlainChart_Loaded(object sender, RoutedEventArgs e)
        {
            InstanceContext context = new InstanceContext(this);
            myPolyline = new Polyline();
            myPolyline.Stroke = Brushes.DeepSkyBlue;
            myPolyline.StrokeThickness = 2;
            PlainChart.Children.Add(myPolyline);
            Thread.Sleep(1000);
            _client = new WCFObserverClient(context);
            _client.Open();
            _client.Subscribe();
            Kpi1.Content = "CPU Subscribed";
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            try
            {
                _client.Unsubscribe();
                _client.Close();
            }
            catch(Exception ex)
            {
                Kpi1.Content = "Exception" + ex.Message;
            }
        }

        void IWCFObserverCallback.OnCompleted()
        {
            Kpi1.Content = "Completed";
        }

        void IWCFObserverCallback.OnError(Exception error)
        {
            Kpi1.Content = "Error: " + error.Message;
        }

        void IWCFObserverCallback.OnNext(OutputEvent value)
        {
            int expectedMax = 100;
            int input = Math.Abs(value.O % expectedMax);
            byte colorIndex = value.Color;
            Debug.WriteLine("colorIndex: " + colorIndex);
            Kpi1.Content = value.O;
            Kpi1.Foreground = colors[colorIndex];
            var last = myPolyline.Points.Count !=0 ? myPolyline.Points.Last():new Point(0,0);
            myPolyline.Points.Add(new Point(last.X + 1, PlainChart.ActualHeight - 10 - input * PlainChart.ActualHeight / expectedMax));
            
            var trans = 25;
            if (last.X % trans == 0)
            {
                TranslateTransform tt = new TranslateTransform(0, 0);
                DoubleAnimation dax = new DoubleAnimation(myPolyline.RenderTransform.Value.OffsetX, PlainChart.ActualWidth - 2 * trans - last.X,
                    new Duration(TimeSpan.FromMilliseconds(450)));
                var easing = new ExponentialEase();
                easing.EasingMode = EasingMode.EaseIn;
                
                dax.EasingFunction = easing;
                tt.BeginAnimation(TranslateTransform.XProperty, dax);
                myPolyline.RenderTransform = tt;
            }
        }

    }
}
