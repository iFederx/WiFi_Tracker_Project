using System;
using System.Text;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Media;
using System.IO;

namespace ServerSocketWpfApp {
    public partial class MainWindow : Window {
        SocketPermission permission;
        Socket sListener;
        IPEndPoint ipEndPoint;
        Socket handler;
        

        private TextBox tbAux = new TextBox();
        string fileName = "out1.txt";
        FileStream fs = null;

        public MainWindow() {
            InitializeComponent();
            tbAux.SelectionChanged += tbAux_SelectionChanged;

            Start_Button.IsEnabled = true;
        }

        private void tbAux_SelectionChanged (object sender, RoutedEventArgs e) {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart) delegate() {
                tbMsgReceived.Text = tbAux.Text;
            });
        }

        public static string GetLocalIPAddress() {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList) {
                if (ip.AddressFamily == AddressFamily.InterNetwork) {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            try {              

                /*// Resolves a host name to an IPHostEntry instance 
                IPHostEntry ipHost = Dns.GetHostEntry();*/

                // Gets first IP address associated with a localhost 
                IPAddress ipAddr = IPAddress.Parse(GetLocalIPAddress());

                // Creates a network endpoint 
                ipEndPoint = new IPEndPoint(ipAddr, 1500);

                // Create one Socket object to listen the incoming connection 
                sListener = new Socket(
                    ipAddr.AddressFamily,
                    SocketType.Stream,
                    ProtocolType.Tcp
                    );

                // Associates a Socket with a local endpoint 
                sListener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                sListener.Bind(ipEndPoint);

                tbStatus.Text = "Server started at: "+ipAddr.ToString();

                Start_Button.IsEnabled = false;
                Close_Button.IsEnabled = true;
            }
            catch (Exception exc) {
                MessageBox.Show(exc.ToString());
            }
        
            try {
                // Places a Socket in a listening state and specifies the maximum 
                // Length of the pending connections queue 
                sListener.Listen(10);

                // Begins an asynchronous operation to accept an attempt 
                AsyncCallback aCallback = new AsyncCallback(AcceptCallback);
                sListener.BeginAccept(aCallback, sListener);

                tbStatus.Text = "Server is listening at " + ipEndPoint.Address + ":" + ipEndPoint.Port;

            }
            catch (Exception exc) {
                MessageBox.Show(exc.ToString());
            }
        }

        public void AcceptCallback(IAsyncResult ar) {
            Socket listener = null;

            // A new Socket to handle remote host communication 
            Socket handler = null;
            try {
                // Receiving byte array 
                byte[] buffer = new byte[1024];

                // Get Listening Socket object 
                listener = (Socket) ar.AsyncState;

                // Create a new socket 
                handler = listener.EndAccept(ar);

                // Using the Nagle algorithm 
                handler.NoDelay = false;

                // Creates one object array for passing data 
                object[] obj = new object[2];
                obj[0] = buffer;
                obj[1] = handler;

                // Begins to asynchronously receive data 
                handler.BeginReceive(
                    buffer,        // An array of type Byt for received data 
                    0,             // The zero-based position in the buffer  
                    buffer.Length, // The number of bytes to receive 
                    SocketFlags.None,// Specifies send and receive behaviors 
                    new AsyncCallback(ReceiveCallback),//An AsyncCallback delegate 
                    obj            // Specifies infomation for receive operation 
                    );

                // Begins an asynchronous operation to accept an attempt 
                AsyncCallback aCallback = new AsyncCallback(AcceptCallback);
                listener.BeginAccept(aCallback, listener);
            }
            catch (Exception exc) {
                MessageBox.Show(exc.ToString());
            }
        }

        public void ReceiveCallback(IAsyncResult ar) {
            try {
                fs = new FileStream(fileName, FileMode.Append);

                // Fetch a user-defined object that contains information 
                object[] obj = new object[2];
                obj = (object[])ar.AsyncState;

                // Received byte array 
                byte[] buffer = (byte[])obj[0];

                // A Socket to handle remote host communication. 
                handler = (Socket)obj[1];

                // Received message 
                string content = string.Empty;

                // The number of bytes received. 
                int bytesRead = handler.EndReceive(ar); //si smette di ricevere dati sul socket

                if (bytesRead > 0) { //cioè: se ho ricevuto qualcosa sul socket
                    content += (Encoding.UTF8.GetString(buffer, 0, bytesRead)); //aggiungo al contenuto solo ciò che ho effettivamente letto tirandolo fuori dal buffer

                    //TODO: if che riconosce i vari comandi
                    if (content.IndexOf("REGISTER") > -1) {
                        string msg = "OKPORT(1501)\r\n";
                        byte[] bufOut = Encoding.UTF8.GetBytes(msg);

                        //handler.BeginSend(bufOut, 0, bufOut.Length, SocketFlags.None, /**/, obj);

                        System.Console.WriteLine(content);
                    }

                    if (content.IndexOf("PONG") > -1) {
                        System.Console.WriteLine(content);
                    }

                    if (content.IndexOf("FILE") > -1) {
                        System.Console.WriteLine(content);
                    }


                    // If message contains "close", finish receiving
                    if (content.IndexOf("close") > -1) {
                        // Convert byte array to string
                        string str = content.Substring(0, content.LastIndexOf("close"));

                        //this is used because the UI couldn't be accessed from an external Thread
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart) delegate () {
                            tbAux.Text = "Read " + str.Length * 2 + " bytes from client.\n Data: " + str;
                        });
                    } else {
                        // Continues to asynchronously receive data
                        byte[] buffernew = new byte[1024]; //è un nuovo buffer da 1MB
                        obj[0] = buffernew;
                        obj[1] = handler; //è il socket passivo
                        handler.BeginReceive(buffernew, 0, buffernew.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), obj);
                    }

                    this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate () {
                        tbAux.Text += content;
                        using (StreamWriter writer = new StreamWriter(fs)) {
                            writer.Write(content);
                        }
                    });
                }
            }
            catch (Exception exc) {
                MessageBox.Show(exc.ToString());
            }
        }

        public void SendCallback(IAsyncResult ar) {

        }

        private void Close_Click(object sender, RoutedEventArgs e) {
            try {
                if (sListener.Connected) {
                    sListener.Shutdown(SocketShutdown.Receive);
                    sListener.Close();
                }
                tbStatus.Text = "Server offline";
                Close_Button.IsEnabled = false;
            }
            catch (Exception exc) {
                MessageBox.Show(exc.ToString());
            }
            Start_Button.IsEnabled = true;
        }

        private void Clean_Click(object sender, RoutedEventArgs e) {
            tbMsgReceived.Text = String.Empty;
            tbAux.Text = String.Empty;
        }

        private void Listen_Click(object sender, RoutedEventArgs e) {

        }
    }
}
