using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.IO;


namespace rtaNetworking.Streaming
{

    /// <summary>
    /// Provides a streaming server that can be used to stream any images source
    /// to any client.
    /// </summary>
    public class ImageStreamingServer:IDisposable
    {

        private List<Socket> _Clients;
        private Thread _Thread;
        public static int myPort;

        public ImageStreamingServer():this(Screen.Snapshots(480,true))
        {

        }

        /// <summary>
        /// ///////my function to get the port number
        /// </summary>
        /// <param name="imagesSource"></param>

        
   

        public ImageStreamingServer(IEnumerable<Image> imagesSource)
        {

            _Clients = new List<Socket>();
            _Thread = null;

            this.ImagesSource = imagesSource;
            this.Interval = 67;                           //frame rate = 1000ms / Interval

        }

        public int getPortNumber()
        {
            return myPort;
        }

        /// <summary>
        /// Gets or sets the source of images that will be streamed to the 
        /// any connected client.
        /// </summary>
        public IEnumerable<Image> ImagesSource { get; set; }

        /// <summary>
        /// Gets or sets the interval in milliseconds (or the delay time) between 
        /// the each image and the other of the stream (the default is . 
        /// </summary>
        public int Interval { get; set; }

        /// <summary>
        /// Gets a collection of client sockets.
        /// </summary>
        public IEnumerable<Socket> Clients { get { return _Clients; } }

        /// <summary>
        /// Returns the status of the server. True means the server is currently 
        /// running and ready to serve any client requests.
        /// </summary>
        public bool IsRunning { get { return (_Thread != null && _Thread.IsAlive); } }

        /// <summary>
        /// Starts the server to accepts any new connections on the specified port.
        /// </summary>
        /// <param name="port"></param>
        public void Start(int port)
        {

            lock (this)
            {
            
                _Thread = new Thread(new ParameterizedThreadStart(ServerThread));
                _Thread.IsBackground = true;
                _Thread.Start(port);
            }

        }

        /// <summary>
        /// Starts the server to accepts any new connections on the default port (8080).
        /// </summary>
        /// this was 0 referenced
      /*  public void Start()
        {
            this.Start(8080);
        }*/

        public void Stop()
        {

            if (this.IsRunning)
            {
                try
                {
                    _Thread.Join();
                    _Thread.Abort();
                }
                finally
                {

                    lock (_Clients)
                    {
                        
                        foreach (var s in _Clients)
                        {
                            try
                            {
                                s.Close();
                            }
                            catch { }
                        }
                        _Clients.Clear();

                    }

                    _Thread = null;
                }
            }
        }

        /// <summary>
        /// This the main thread of the server that serves all the new 
        /// connections from clients.
        /// </summary>
        /// <param name="state"></param>
        private void ServerThread(object state)
        {

            try
            {
                Socket Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                Server.Bind(new IPEndPoint(IPAddress.Any,(int)state));
                Server.Listen(10);    ///////////////5 given instead of 10

                System.Diagnostics.Debug.WriteLine(string.Format("Server started on port {0}.", state));

                myPort = (int)state;
                System.Diagnostics.Debug.WriteLine("Checking my port : " + myPort.ToString());

                
                foreach (Socket client in Server.IncommingConnectoins())
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ClientThread), client);
            
            }
            catch 
            {
                System.Diagnostics.Debug.WriteLine("Exception Caught for multiple entry");
            }

            this.Stop();
        }

        /// <summary>
        /// Each client connection will be served by this thread.
        /// </summary>
        /// <param name="client"></param>
        private void ClientThread(object client)
        {

            Socket socket = (Socket)client;

            System.Diagnostics.Debug.WriteLine(string.Format("New client from {0}",socket.RemoteEndPoint.ToString()));

            lock (_Clients)
                _Clients.Add(socket);

            try
            {
                using (MjpegWriter wr = new MjpegWriter(new NetworkStream(socket, true)))
                {

                    // Writes the response header to the client.
                    wr.WriteHeader();

                    // Streams the images from the source to the client.
                    foreach (var imgStream in Screen.Streams(this.ImagesSource))
                    {
                        if (this.Interval > 0)
                            Thread.Sleep(this.Interval);

                        wr.Write(imgStream);
                    }

                }
            }
            catch { }
            finally
            {
                lock (_Clients)
                    _Clients.Remove(socket);
            }
        }


        #region IDisposable Members

        public void Dispose()
        {
            this.Stop();
        }

        #endregion
    }

    static class SocketExtensions
    {

        public static IEnumerable<Socket> IncommingConnectoins(this Socket server)
        {
            while(true)
                yield return server.Accept();
        }

    }


    static class Screen
    {


        public static IEnumerable<Image> Snapshots()
        {
            return Screen.Snapshots(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height, true);
        }

        /// <summary>
        /// Returns a 
        /// </summary>
        /// <param name="delayTime"></param>
        /// <returns></returns>
        public static IEnumerable<Image> Snapshots(int height, bool showCursor)
        {

            Size size = new Size(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);

            Bitmap srcImage = new Bitmap(size.Width, size.Height);
            Graphics srcGraphics = Graphics.FromImage(srcImage);

            System.Diagnostics.Debug.WriteLine("screen width : " + size.Width + " screen height : " + size.Height);

            // height is actually the resolution like, if height is 480 then it is 480p
            // we then get the screen ratio and adjust the width according to that
            //////////////////////////////////////

            double ratio = ( (double) size.Width / (double) size.Height);

            System.Diagnostics.Debug.WriteLine("ratio : " + ratio);

            int width = (int) ( ratio * (double)height);

            //height = (int) ( width / ratio); //not necessary actually

            System.Diagnostics.Debug.WriteLine("changed width : " + width + " changed height : " + height);

            /////////////////////////////////////


            bool scaled = (width != size.Width || height != size.Height);

            Bitmap dstImage = srcImage;
            Graphics dstGraphics = srcGraphics;

            if (scaled)
            {
                dstImage = new Bitmap(width, height);
                dstGraphics = Graphics.FromImage(dstImage);
            }

            Rectangle src = new Rectangle(0, 0, size.Width, size.Height);
            Rectangle dst = new Rectangle(0, 0, width, height);
            Size curSize = new Size(32, 32);

            while (true)
            {
                srcGraphics.CopyFromScreen(0, 0, 0, 0, size);

                if (showCursor)
                    Cursors.Default.Draw(srcGraphics, new Rectangle(Cursor.Position, curSize));

                if (scaled)
                    dstGraphics.DrawImage(srcImage, dst, src, GraphicsUnit.Pixel);

                yield return dstImage;

            }

            srcGraphics.Dispose();
            dstGraphics.Dispose();

            srcImage.Dispose();
            dstImage.Dispose();

            yield break;
        }

        internal static IEnumerable<MemoryStream> Streams(this IEnumerable<Image> source)
        {
            MemoryStream ms = new MemoryStream();

            foreach (var img in source)
            {
                ms.SetLength(0);
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                yield return ms;
            }

            ms.Close();
            ms = null;

            yield break;
        }

    }
}
