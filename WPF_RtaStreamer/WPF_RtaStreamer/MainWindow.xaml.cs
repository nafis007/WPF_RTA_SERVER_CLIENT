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

using rtaNetworking.Streaming;
using System.Timers;
using System.Threading;

using System.Net; //Include this namespace

using System.Windows.Threading;

using System.Net.NetworkInformation;


 

namespace WPF_RtaStreamer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ImageStreamingServer _Server;
        private static int buttonClicked = 0;
        
        public void initializeTimer()
        {
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        
        public MainWindow()
        {
            InitializeComponent();
            
        }

        private DateTime time = DateTime.MinValue;


        private string getServerIpAndPortURL(ImageStreamingServer server)
       {
            string hostName = Dns.GetHostName(); // Retrive the Name of HOST

            int lastIpAddressIndex = Dns.GetHostByName(hostName).AddressList.Length - 1; //always get the last ip for streaming

            //System.Diagnostics.Debug.WriteLine("Checking last ip index : " + lastIpAddressIndex);

            string myIP = Dns.GetHostByName(hostName).AddressList[lastIpAddressIndex].ToString();

            string myPORT = server.getPortNumber().ToString();

            string ipAndPortURL = "http://" + myIP + ":" + myPORT;  //made like an url 
                                                                //since the client player only accepts this format

            return ipAndPortURL;
       }


        /// <summary>
        /// to avoid used port collision
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        private int getAvailablePort(int port)
        {
            Boolean isAvailable = false;  //just for debugging

            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            

            IPEndPoint[] objEndPoints = ipGlobalProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endPoint in objEndPoints)
            {
                System.Diagnostics.Debug.WriteLine("Checking bug : " + endPoint.Port);
                if (endPoint.Port == port)
                {
                    isAvailable = false;
                    port += 2;
                }
                else
                {
                    isAvailable = true;
                    
                }
            }

            System.Diagnostics.Debug.WriteLine("Checking port avaialability : " + isAvailable + " " + port);
            return port;
        }


        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if ( buttonClicked ==0 )
            {
                buttonClicked = 1;
                _Server = new ImageStreamingServer();

                int safePort = getAvailablePort(8080);                 //8080 is just the first try
                _Server.Start(safePort);                              //this where port is given inside Start()

                IpBox.Text = getServerIpAndPortURL(_Server);
                initializeTimer();
            }
            else
            {
                MessageBoxResult result = 
                    MessageBox.Show("Server Already Running!!", "Confirmation", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        
        }


        /// <summary>
        ///  for client count
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            // code goes here
            if (_Server != null)
            {
                int count = (_Server.Clients != null) ? _Server.Clients.Count() : 0;

                ClientCount.Text = "Clients: " + count.ToString();
            }
        
        }
           
    }
}
