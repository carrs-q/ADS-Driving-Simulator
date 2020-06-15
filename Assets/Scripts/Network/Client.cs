using System;
using System.Net;
using System.Text;
using UnityEngine;
using System.Net.Sockets;

public class Client : MonoBehaviour
{
    public Action OnConnected = delegate { };
    public Action OnDisconnected = delegate { };
    public Action<string> OnMessage = delegate { };

    private int connectionID;
    private string IPAddress = "localhost";
    private int Port = 8052;
    private byte[] buffer = new byte[Controller.BUFFERSIZE];

    public Socket clientSocket;
    public IPEndPoint endPoint;
    private bool socketConnected = false;

    //Connect
    public void ConnectToServer(string ip, int port){
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            endPoint = new IPEndPoint(System.Net.IPAddress.Parse(ip), port);
            clientSocket.BeginConnect(endPoint, ConnectCallback, clientSocket);
            socketConnected = true;

            //TODO: Send first message with Display
            //SendData(clientSocket, "This is a test");

        }
        catch (Exception e)
        {
            Debug.Log("Error while connecting to Server " + e);
        }
    }
    private void ConnectCallback(IAsyncResult ar){
        try{
            Socket client = (Socket)ar.AsyncState;
            client.EndConnect(ar);
            OnConnected();
            Debug.Log("Connected to " + client.RemoteEndPoint);
        }
        catch(Exception e){
            Debug.Log("Error connecting: " + e);
        }
    }
    public bool IsConnected(){
        //TODO make check if connection is active
        return socketConnected;
    }

    //Send
    public void SendToServer(string message){
        byte[] byteMessage = Encoding.ASCII.GetBytes(message);

        clientSocket.BeginSend(byteMessage, 0, byteMessage.Length, 0, 
            new AsyncCallback(SendCallBack), clientSocket);

    }
    private void SendCallBack(IAsyncResult ar){
        try{
            Socket client = (Socket)ar.AsyncState;
            int bytesSent = client.EndSend(ar);
            Debug.Log("Message sent with " + bytesSent + " bytes");
        }
        catch(Exception e){
            Debug.Log("Error sending message");
        }
    }
    
    //Receive
    public void ClientUpdate()
    {
        try{
            clientSocket.BeginReceive(buffer, 0, buffer.Length, 0,
                new AsyncCallback(ReceiveCallback), clientSocket);
        }
        catch(Exception e){
            OnDisconnected();
            Debug.Log("Error receiving data");
        }
    }
    private void ReceiveCallback(IAsyncResult ar)
    {
        try{
            Socket client = (Socket)ar.AsyncState;
            int bytesReceived = client.EndReceive(ar);

            if(bytesReceived == 0){
                Debug.Log("nothing to receive");
                return;
            }

            var data = new byte[bytesReceived];
            Array.Copy(buffer, data, bytesReceived);

            client.BeginReceive(buffer, 0, buffer.Length, 0,
                new AsyncCallback(ReceiveCallback), client);

            string serverMessage = Encoding.Default.GetString(buffer);
            OnMessage(serverMessage);
        }
        catch(Exception e){
            Debug.Log("Receiving failed");
        }
    }


    //Shutdown
    public void StopClient()
    {
        if(socketConnected)
        {
            clientSocket.Close();
            socketConnected = false;
        }
    }
    private void OnApplicationQuit()
    {
        StopClient();
    }
    private void OnDestroy()
    {
        StopClient();
    }
    private void OnDisable()
    {
        StopClient();
    }


    /*
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
                using (stream = socketConnection.GetStream())
                {
                    int length;

                    while (running && stream.CanRead)
                    {
                        length = stream.Read(bytes, 0, bytes.Length);
                        if (length != 0)
                        {
                            var incomingData = new byte[length];
                            Array.Copy(bytes, 0, incomingData, 0, length);
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
    */
}