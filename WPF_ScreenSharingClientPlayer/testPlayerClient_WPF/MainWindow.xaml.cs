using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using MjpegProcessor;
using System.Drawing;
using System.Net;

namespace testPlayerClient_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MjpegDecoder _mjpeg;
        String myIP;

        Boolean buttonClicked = false;
        public MainWindow()
        {
            InitializeComponent();
       //     String machineName = Dns.GetHostName();
       //     myIP = "http://" + "192.168.29.16" + ":8080";
       //     UrlField.Text = myIP;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if ( !buttonClicked )
            {
                buttonClicked = true;
                playSharedScreen();
            }
            else
            {
                MessageBoxResult result =
                    MessageBox.Show("Screen Already Playing!!", "Confirmation", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void playSharedScreen()
        {
            _mjpeg = new MjpegDecoder();

            _mjpeg.ParseStream(new Uri(UrlField.Text));


            _mjpeg.FrameReady += mjpeg_FrameReady;
        }

        private void mjpeg_FrameReady(object sender, FrameReadyEventArgs e)
        {
            Screen.Source = e.BitmapImage;
        }
    }
}
