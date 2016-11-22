////////////////////////////////////////////////////////////////////////////////
//
//    Read Tags
//
////////////////////////////////////////////////////////////////////////////////
#define SOCKET
using System;
using Impinj.OctaneSdk;

//Add from here
using System.Collections.Generic;
using System.Text;
//using System.Net.Sockets;　　//System.Netの参照設定が必要です

//using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
//using System.Text;
//up to here 


namespace OctaneSdkExamples
{
#if SOCKET


    // State object for receiving data from remote device.
    public class StateObject
    {
        // Client socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 256;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }

    public class AsynchronousClient
    {
        // The port number for the remote device.
        private const int port = 65000;//11000;
        private Dictionary<string, int> seen_data = new Dictionary<string, int>();

        // ManualResetEvent instances signal completion.
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);
        // Create a TCP/IP socket.
        private Socket client = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

        // The response from the remote device.
        private static String response = String.Empty;

        public AsynchronousClient()
        {


        }
        public void StartC()
        {
            try
            {

            
            // Establish the remote endpoint for the socket.
            // The name of the 
            // remote device is "host.contoso.com".
            //IPHostEntry ipHostInfo = Dns.Resolve("host.contoso.com");

            IPAddress ipAddress = System.Net.IPAddress.Parse("192.168.100.85");//ipHostInfo.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);



            // Connect to the remote endpoint.
            client.BeginConnect(remoteEP,
                new AsyncCallback(ConnectCallback), client);
            connectDone.WaitOne();

            //StartClient(client, report, remoteEP);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }


        }
        public void CloseClient()
        {
            try
            {
                // Receive the response from the remote device.
                Receive(client);
                receiveDone.WaitOne();

                // Write the response to the console.
                Console.WriteLine("Response received : {0}", response);

                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void ClientSend(TagReport report)
        {
            // Sends the data from the report
            try
            {
                // Send test data to the remote device.
                foreach (Tag tag in report)
                {
                    //If you want the data as below can be sent instead of only the tag data
                    //String data = "Antenna : {0}, EPC : {1} {2}" + tag.AntennaPortNumber + tag.Epc.ToHexString() + tag.PeakRssiInDbm;
                    String epcStr = tag.Epc.ToHexString();
                    if (seen_data.ContainsKey(epcStr))
                    {
                        seen_data[epcStr]++;
                    }
                    else
                    {
                        seen_data.Add(epcStr, 0);
                        Send(client, epcStr);
                        sendDone.WaitOne();
                        Console.WriteLine(epcStr);
                        //Console.ReadKey();
                        //Console.WriteLine("Press any key to continue");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Receive(Socket client)
        {
            try
            {
                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Get the rest of the data.
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }
                    // Signal that all bytes have been received.
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


    }

#endif





    class Program
    {
        // Create an instance of the ImpinjReader class.
        static ImpinjReader reader = new ImpinjReader();
#if SOCKET
        static AsynchronousClient ac = new AsynchronousClient();
#endif

        static void Main(string[] args)
        {
            try
            {
                //Starts the client
#if SOCKET
                ac.StartC();
#endif
                // Connect to the reader.
                // Change the ReaderHostname constant in SolutionConstants.cs 
                // to the IP address or hostname of your reader.
                reader.Connect(SolutionConstants.ReaderHostname);

                // Get the default settings
                // We'll use these as a starting point
                // and then modify the settings we're 
                // interested in.
                Settings settings = reader.QueryDefaultSettings();

                // Tell the reader to include the antenna number
                // in all tag reports. Other fields can be added
                // to the reports in the same way by setting the 
                // appropriate Report.IncludeXXXXXXX property.
                settings.Report.IncludeAntennaPortNumber = true;
                settings.Report.IncludePeakRssi = true;
                settings.Report.IncludeLastSeenTime = true;

                // The reader can be set into various modes in which reader
                // dynamics are optimized for specific regions and environments.
                // The following mode, AutoSetDenseReader, monitors RF noise and interference and then automatically
                // and continuously optimizes the reader’s configuration
                settings.ReaderMode = ReaderMode.AutoSetDenseReader;
                settings.SearchMode = SearchMode.DualTarget;
                settings.Session = 2;

                // Enable antenna #1. Disable all others.
                settings.Antennas.DisableAll();
                settings.Antennas.GetAntenna(1).IsEnabled = true;

                // Set the Transmit Power and 
                // Receive Sensitivity to the maximum.
                settings.Antennas.GetAntenna(1).MaxTxPower = true;
                settings.Antennas.GetAntenna(1).MaxRxSensitivity = true;
                // You can also set them to specific values like this...
                settings.Antennas.GetAntenna(1).TxPowerInDbm = 26;
                //settings.Antennas.GetAntenna(1).RxSensitivityInDbm = -70;

                // Apply the newly modified settings.
                reader.ApplySettings(settings);

                // Assign the TagsReported event handler.
                // This specifies which method to call
                // when tags reports are available.
                reader.TagsReported += OnTagsReported;


                // Start reading.
                reader.Start();

                // Wait for the user to press enter.
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();

                // Stop reading.
                reader.Stop();

                // Disconnect from the reader.
                reader.Disconnect();
                //TODO correct placement
#if SOCKET
                ac.CloseClient();
#endif
            }
            catch (OctaneSdkException e)
            {
                // Handle Octane SDK errors.
                Console.WriteLine("Octane SDK exception: {0}", e.Message);
            }
            catch (Exception e)
            {
                // Handle other .NET errors.
                Console.WriteLine("Exception : {0}", e.Message);
            }
        }

        static void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            //Sends the report to the client which sends it to the server
#if SOCKET
            ac.ClientSend(report);
#else
            // Send test data to the remote device.
            foreach (Tag tag in report)
            {
                //If you want the data as below can be sent instead of only the tag data
                Console.WriteLine("Antenna : {0}, EPC : {1} {2} {3}" , tag.AntennaPortNumber , tag.Epc.ToHexString() , tag.PeakRssiInDbm, tag.LastSeenTime);
             
            }
#endif

        }

    }
}
