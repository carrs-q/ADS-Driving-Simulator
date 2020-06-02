using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class Client : MonoBehaviour
{
    public Action<Client> OnConnected = delegate { };
    public Action<Client> OnDisconnected = delegate { };
    public Action<string> OnLog = delegate { };
    public Action<Server.ServerMessage> OnMessageReceived = delegate { };

    private int connectionID;
    private int displayID;

    public void setdisplayID(int displayID)
    {
        this.displayID = displayID;
    }
    public int getConnectionID()
    {
        return this.connectionID;
    }
    public int getDisplayID()
    {
        return this.displayID;
    }
    public bool IsConnected
    {
        get { return socketConnection != null && socketConnection.Connected; }
    }

    public string IPAddress = "localhost";
    public int Port = 8052;

    private TcpClient socketConnection;
    private Thread clientReceiveThread;
    private NetworkStream stream;
    private bool running; 


    /// <summary> 	
    /// Setup socket connection. 	
    /// </summary> 	
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

    /// <summary> 	
    /// Runs in background clientReceiveThread; Listens for incoming data. 	
    /// </summary>     
    private void ListenForData()
    {
        try
        {
            socketConnection = new TcpClient(IPAddress, Port);
            OnConnected(this);
            Debug.Log("Connected");

            Byte[] bytes = new Byte[Controller.BUFFERSIZE];
            running = true;
            SendMessage("Hello");
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
                            Server.ServerMessage serverMessage = JsonUtility.FromJson<Server.ServerMessage>(serverJson);
                            MessageReceived(serverMessage);
                        }
                    }
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

    public void CloseConnection()
    {
        SendMessage("bye bye"); 
        running = false;
    }

    public void MessageReceived(Server.ServerMessage serverMessage)
    {
        OnMessageReceived(serverMessage);
    }

    /// <summary> 	
    /// Send message to server using socket connection. 	
    /// </summary> 	
    public bool SendMessage(string clientMessage)
    {
        if (socketConnection != null && socketConnection.Connected)
        {
            try
            {
                // Get a stream object for writing. 			
                NetworkStream stream = socketConnection.GetStream();
                if (stream.CanWrite)
                {
                    // Convert string message to byte array.                 
                    byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(clientMessage);
                    // Write byte array to socketConnection stream.                 
                    stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
                    OnSentMessage(clientMessage);
                    return true;
                }
            }
            catch (SocketException socketException)
            {
                Debug.Log("Socket exception: " + socketException);
            }
        }

        return false;
    }

    public virtual void OnSentMessage(string message)
    {

    }


    private void StopClient()
    {
        if (running)
        {
            SendMessage("!disconnect");
            running = false;
            stream.Close();
        }
      
    }

    void OnApplicationQuit()
    {
        StopClient();
    }
    void OnDestroy()
    {
        StopClient();
    }
}