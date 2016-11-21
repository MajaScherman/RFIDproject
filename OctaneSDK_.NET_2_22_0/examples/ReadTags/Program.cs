﻿////////////////////////////////////////////////////////////////////////////////
//
//    Read Tags
//
////////////////////////////////////////////////////////////////////////////////

using System;
using Impinj.OctaneSdk;

//Add from here
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;　　//System.Netの参照設定が必要です
//up to here 

namespace OctaneSdkExamples
{
    class Program
    {
        // Create an instance of the ImpinjReader class.
        static ImpinjReader reader = new ImpinjReader();


        static void Main(string[] args)
        {
            try
            {

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
                settings.Antennas.GetAntenna(1).TxPowerInDbm = 30;
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
            //Add from here
            // This event handler is called asynchronously 
            // when tag reports are available.
            // Loop through each tag in the report 
            // and print the data.
            var list = new List<string>();
            foreach (Tag tag in report)
            {
                string str = tag.Epc.ToHexString();
                list.Add(str);

                //if (tag.Epc.ToHexString() == "52554E303542443032323031")
                //{
                Console.WriteLine("Antenna : {0}, EPC : {1} {2}",
                                      tag.AntennaPortNumber, tag.Epc.ToHexString(), tag.PeakRssiInDbm);
                //}                
            }
#if SOCKET
            //Add from here
            //IPアドレスとポート番号を指定
            //string型とint型なのが不思議
            //勿論送信先のIPアドレスとポート番号です
            string ipAddress = "192.168.100.85";
            int port = 65000;
            //IPアドレスとポート番号を渡してサーバ側へ接続
            TcpClient client = new TcpClient(ipAddress, port);
            //NWのデータを扱うストリームを作成
            //            NetworkStream stream = client.GetStream();
            int offset = 0;
            NetworkStream stream = client.GetStream();

            foreach (var item in list)
            {
                byte[] tmp = Encoding.UTF8.GetBytes(item);
                stream.Write(tmp, offset, tmp.Length);
//                stream.Write(tmp, 0, tmp.Length);
                offset += tmp.Length;
                System.Threading.Thread.Sleep(10000);
//                stream.Close();
            }
            //サーバとの接続を終了
            client.Close();
            //Add up to here
#endif
        }
    }
}
