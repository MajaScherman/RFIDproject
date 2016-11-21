using System;
using System.Text;
using System.Net;
using System.Net.Sockets;　　


class Program

{

　　static void Main(string[] args)
　　{
　　　　Console.WriteLine("To start the server, press any key");

　　　　Console.ReadLine();

　　　　try
　　　　{
　　　　　　int port = 2222;
　　　　　　IPAddress ip = IPAddress.Parse("192.168.24.78");

　　　　　　TcpListener server = new TcpListener(ip, port);
　　　　　　server.Start();

　　　　　　TcpClient client = server.AcceptTcpClient();

　　　　　　IPEndPoint endpoint = (IPEndPoint)client.Client.RemoteEndPoint;
　　　　　　IPAddress address = endpoint.Address;

　　　　　　NetworkStream stream = client.GetStream();

　　　　　　byte[] getData = new byte[1];

　　　　　　int cnt;

　　　　　　//Define a list for the received data

　　　　　　List<byte> bytelist=new List<byte>();

　　　　　　//cnt denotes the length of the received data

　　　　　　while((cnt = stream.Read(getData, 0, getData.length)) > 0)

　　　　　　{

　　　　　　　　　//Insert the received data to the list

　　　　　　　　　bytelist.Add(getData);
　　　　　　}

　　　　　　byte[] result = new byte[bytelist.Count];


　　　　　　for(int i = 0 ; i < result.Length ; i++){

　　　　　　　　　result[i] = bytelist[i];

　　　　　　}

　　　　　　//Encode bytes to strings

　　　　　　string data = Encoding.UTF8.GetString(result);
　　　　　　//Output results
　　　　　　Console.WriteLine("Received data:{0}", data);

　　　　　　//Finish server

　　　　　　client.Close();

　　　　}
　　　　catch (Exception e)
　　　　{
　　　　　　Console.WriteLine(e.Message);
　　　　}
　　　　finally
　　　　{
　　　　　　Console.WriteLine("To end the program, press any key");
　　　　　　Console.ReadLine();
　　　　}
　　}
}
