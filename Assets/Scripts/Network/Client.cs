using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class Client : MonoBehaviour
{
    public Action<Client> OnConnected = delegate { };
    public Action<Client> OnDisconnected = delegate { };
    public Action<ServerMessage> OnMessageReceived = delegate { };

    private int connectionID;
   
    private string IPAddress = "localhost";
    private int Port = 8052;

    private TcpClient socketConnection;
    private Thread clientReceiveThread;
    private NetworkStream stream;
    private bool running;

    public bool IsConnected
    {
        get { return socketConnection != null && socketConnection.Connected; }
    }
    public void ConnectToTcpServer(string IPAddress, int Port)
    {
        try
        {
            this.IPAddress = IPAddress;
            this.Port = Port;
            Debug.Log(string.Format("Connecting to {0}:{1}", IPAddress, Port));

            clientReceiveThread = new Thread(new ThreadStart(ListenForData));
            clientReceiveThread.IsBackground = true;
            clientReceiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.Log("On client connect exception " + e);
        }
    }
    public new void SendMessage(string clientMessage)
    {
        if (socketConnection != null && socketConnection.Connected)
        {
            try
            {
                NetworkStream stream = socketConnection.GetStream();
                if (stream.CanWrite)
                {
                    byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(clientMessage);
                    stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
                    Debug.Log("Message sent" + clientMessage);
                }
            }
            catch (SocketException socketException)
            {
                Debug.Log("Socket exception: " + socketException);
            }
        }
    }
    public void CloseConnection()
    {
        SendMessage("bye bye");
        running = false;
    }

    private void ListenForData()
    {
        try
        {
            socketConnection = new TcpClient(IPAddress, Port);
            OnConnected(this);
            Debug.Log("Connected");

            Byte[] bytes = new Byte[Controller.BUFFERSIZE];
            running = true;

            while (running)
            {
                // Get a stream object for reading
                using (stream = socketConnection.GetStream())
                {
                    int length;
                    // Read incoming stream into byte array. 					
                    while (running && stream.CanRead)
                    {
                        length = stream.Read(bytes, 0, bytes.Length);
                        if (length != 0)
                        {
                            var incomingData = new byte[length];
                            Array.Copy(bytes, 0, incomingData, 0, length);
                            // Convert byte array to string message. 						
                            string serverJson = Encoding.ASCII.GetString(incomingData);
                            ServerMessage serverMessage = JsonUtility.FromJson<ServerMessage>(serverJson);
                            OnMessageReceived(serverMessage);
                        }
                    }
                    Debug.Log("I can't write anymore");
                }
            }
            socketConnection.Close();
            Debug.Log("Disconnected from server");
            OnDisconnected(this);
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }
    private void StopClient()
    {
        if (running)
        {
            //SendMessage("!disconnect");
            stream.Close();
        }
        running = false;

    }
    private void OnApplicationQuit()
    {
        StopClient();
    }
    private void OnDestroy()
    {
        StopClient();
    }
}