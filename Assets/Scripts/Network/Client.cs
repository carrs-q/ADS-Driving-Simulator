using System;
using System.Net;
using System.Text;
using UnityEngine;
using System.Net.Sockets;

public class myClient : MonoBehaviour
{
    //Delegate Events
    public Action OnConnected = delegate { };
    public Action OnDisconnected = delegate { };
    public Action<ServerMessage> OnMessage = delegate { };

    private int connectionID;
    private string IPAddress = "localhost";
    private int Port = 8052;
    private byte[] buffer = new byte[Controller.BUFFERSIZE];
    private bool autoReconnect = true;

    public Socket clientSocket;
    public IPEndPoint endPoint;
    private bool socketConnected = false;

    //Connect
    public void ConnectToServer(string ip, int port)
    {
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
    private void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            Socket client = (Socket)ar.AsyncState;
            client.EndConnect(ar);
            OnConnected();
            Debug.Log("Connected to " + client.RemoteEndPoint);
        }
        catch (Exception e)
        {
            Debug.Log("Error connecting: " + e);
        }
    }
    public bool IsConnected()
    {
        //TODO make check if connection is active
        return socketConnected;
    }
    public void Reconnect(){
        if (autoReconnect){
            //TODO: If lost connection,  
        }
    }

    //Send
    public void SendToServer(string message)
    {
        byte[] byteMessage = Encoding.ASCII.GetBytes(message);

        clientSocket.BeginSend(byteMessage, 0, byteMessage.Length, 0,
            new AsyncCallback(SendCallBack), clientSocket);

    }
    private void SendCallBack(IAsyncResult ar)
    {
        try
        {
            Socket client = (Socket)ar.AsyncState;
            int bytesSent = client.EndSend(ar);
            Debug.Log("Message sent with " + bytesSent + " bytes");
        }
        catch (Exception e)
        {
            Debug.Log("Error sending message");
        }
    }

    //Receive
    public void ClientUpdate()
    {
        try
        {
            Debug.Log("I listen");
            clientSocket.BeginReceive(buffer, 0, buffer.Length, 0,
                new AsyncCallback(ReceiveCallback), clientSocket);
        }
        catch (Exception e)
        {
            StopClient();
            Debug.Log("Error receiving data");
        }
    }
    private void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            Socket client = (Socket)ar.AsyncState;
            int bytesReceived = client.EndReceive(ar);


            if (bytesReceived == 0){
                return;
            }
            else if (bytesReceived > 0 ){
                var data = new byte[bytesReceived];
                Array.Copy(buffer, data, bytesReceived);

                string rawMessage = Encoding.Default.GetString(buffer);
                Debug.Log("Received " + rawMessage);
                if (!rawMessage.Contains("}{")) //Check if two or more messages have received at once
                {
                    //Unpack message according to ServerMessage class (in Server.cs)
                    ServerMessage serverMessage = JsonUtility.FromJson<ServerMessage>(rawMessage);
                    Debug.Log("Received " + serverMessage.message);
                    OnMessage(serverMessage);
                    
                }
                else
                {
                    string[] messages = rawMessage.Split('}');
                    string tmp;
                    foreach(string mMessage in messages)
                    {
                        tmp = mMessage + "}";
                        if (!String.IsNullOrEmpty(tmp)){ 
                            ServerMessage serverMessage = JsonUtility.FromJson<ServerMessage>(tmp);
                            OnMessage(serverMessage);
                            Debug.Log("multi messages");
                        }
                    }
                }
                Array.Clear(buffer, 0, buffer.Length);


                //client.BeginReceive(buffer, 0, buffer.Length, 0,  new AsyncCallback(ReceiveCallback), client);
            }
        }
        catch (Exception e)
        {
            Array.Clear(buffer, 0, buffer.Length);
            Debug.Log("Receiving failed" + e);
        }
    }


    //Shutdown
    public void StopClient()
    {
        if (socketConnected)
        {
            clientSocket.Close();
            socketConnected = false;
            OnDisconnected();
        }
        autoReconnect = false;
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
}