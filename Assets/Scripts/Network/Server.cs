﻿using System;
using System.Net;
using System.Text;
using UnityEngine;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;

public class ServerClient{
    //ID Creator 
    public static int CON_ID;

    //ServerClient
    public Socket      socket;      // Client Socket
    private DateTime   connectedSince; // Session Time
    public int         ID;             // Connection ID
    public int         type;           // Display Type

    //Constructor
    public ServerClient(Socket socket) {
        connectedSince = new DateTime();
        this.socket = socket;
        ID = ++ServerClient.CON_ID; 
        type = 0;
    }
    public void SetType(int type){
        this.type = type;
    }
    public bool IsConnected(){
        try{
            return socket != null && socket.Connected;
        }
        catch{
            return false;
        }
    }
    public TimeSpan ConnectionTime(){
        return connectedSince - new DateTime();
    }
}

[Serializable]
public class ServerMessage{
    public string message;
    public int type;
    public int ID;

    public ServerMessage(string message, int type, int ID){
        this.message = message;
        this.type = type;
        this.ID = ID;
    }
}

public class StateObject{
    public ServerClient serverClient;
    public byte[] buffer = new byte[Controller.BUFFERSIZE];
    public StringBuilder sb = new StringBuilder();
}

public class Server :  MonoBehaviour
{
    //Delegate Events
    public Action<ServerMessage>    OnClientMessage = delegate { };
    public Action<ServerClient>     OnClientConnect = delegate { };
    public Action<ServerClient>     OnClientDisconnect = delegate { };

    private string IPAddress = "127.0.0.1";
    private int Port = 8052;
    private byte[] serverBuffer = new byte[Controller.BUFFERSIZE];
    private Socket  serverSocket;
    private static List<ServerClient> connectedClients = new List<ServerClient>();
    private static List<ServerClient> disconnectedClients = new List<ServerClient>();

    private bool    serverStarted = false;
    private bool    loopKiller = true;

    public bool IsConnected(){
        try{
            return serverSocket != null && serverSocket.Connected;
        }
        catch{
            return false;
        }
    }
    public bool IsConnected(Socket c){
        try{
            return c != null && c.Connected;
        }
        catch{
            return false;
        }
    }

    //Startup Server
    public bool CreateServer(string IPAddress, int Port)
    {
        this.IPAddress = IPAddress;
        this.Port = Port;

        try{
            //Define Socket, create EndPoint and set Max_Client
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(System.Net.IPAddress.Parse(this.IPAddress), this.Port));
            serverSocket.Listen(Controller.MAX_CONNECTION);
            ThreadPool.QueueUserWorkItem(StartListening, serverSocket);
            Debug.Log("Server has been created");
            serverStarted = true;
        }
        catch(Exception e){
            serverStarted = false;
            Debug.Log("Error at CreateServer \n" + e);
        }
        return serverStarted;
    }
    private void StartListening(object token){
        try{
            while (loopKiller){
                //allDone.Reset();

                // Waiting for Connection
                serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), serverSocket);

                //TODO: Check if Async doesn't work
                //listener.BeginAccept(SocketListener.AcceptCallback, listener);

                //allDone.WaitOne();
            }
        }
        catch(Exception e){
            Debug.Log("AcceptConnection failed " + e);
        }
        Debug.Log("Connection closed");
    }
    private void AcceptCallback(IAsyncResult ar){
        Socket server = (Socket)ar.AsyncState;

        //Thats ma client
        ServerClient handler = new ServerClient(server.EndAccept(ar));
        connectedClients.Add(handler);
        StateObject clientState = new StateObject();
        clientState.serverClient = handler;

        handler.socket.BeginReceive(clientState.buffer, 0, clientState.buffer.Length, 0, ReceiveCallback, clientState);
    }

    //Server Update
    public void ServerUpdate(){
        if (!serverStarted)
        {
            return;
        }
        foreach (ServerClient sc in connectedClients){
            if (!sc.IsConnected()){
                sc.socket.Close();
                OnClientDisconnect(sc);
                disconnectedClients.Add(sc);

            }
            else{
                CheckForData(sc);
            }
        }

        //clear up Disconnected Clients
        foreach(ServerClient dc in disconnectedClients){
            connectedClients.Remove(dc);
        }
        disconnectedClients.Clear();
    }
    void CheckForData(ServerClient sc){
        Byte[] bytes = new Byte[Controller.BUFFERSIZE];
        sc.socket.BeginReceive(bytes, 0, Controller.BUFFERSIZE, 0, ReceiveCallback, sc);
    }
    void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            StateObject clientState = (StateObject)ar.AsyncState;
            Socket c = clientState.serverClient.socket;
            int bR = c.EndReceive(ar);
            if (bR > 0)
            {
                clientState.sb.Append(Encoding.ASCII.GetString(clientState.buffer, 0, bR));

                c.BeginReceive(clientState.buffer, 0, Controller.BUFFERSIZE, 0,
                    new AsyncCallback(ReceiveCallback), clientState);
            }
            else
            {
                if (clientState.sb.Length > 1)
                {
                    OnClientMessage(
                        new ServerMessage(
                            clientState.sb.ToString(),
                            clientState.serverClient.type, 
                            clientState.serverClient.ID));
                }

                // Signal that all bytes have been received.  
                //receiveDone.Set();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }


        /*

        Byte[] bytes = new Byte[Controller.BUFFERSIZE];
        Socket handler = (Socket)ar.AsyncState;
        ServerClient client = new ServerClient(handler);


        

        Debug.Log("function to read data from client");
        
        // Read data from the client socket  
        int bytesRead = client.tcpSocket.EndReceive(ar);

        Debug.Log("receiving from client....");
        
        if (bytesRead == 0)
        {
            //no data to read
            Debug.Log("no data to receive");
            return;
        }


        //////////////not sure here
        var data = new byte[bytesRead];
        Array.Copy(serverBuffer, data, bytesRead);

        // Get the data
        client.tcpSocket.BeginReceive(serverBuffer, 0, serverBuffer.Length, 0,
                                      new AsyncCallback(ReadCallback), client.tcpSocket);
        /////////////////

        //store the data received
        content = Encoding.ASCII.GetString(serverBuffer);


        //send data to teh client
        //Send(client.tcpSocket, "hello from the server");

        //OnIncommingData(client, content);

        Debug.Log("client sent: ");

        */

    }

    //Server Events
    public  void BroadCastAll(string data){
        if (!serverStarted){
            return;
        }
        foreach (ServerClient sc in connectedClients){
            if (!sc.IsConnected()){
                sc.socket.Close();
                OnClientDisconnect(sc);
                disconnectedClients.Add(sc);

            }
            else{
                Send(sc, data);
            }
        }

        //clear up Disconnected Clients
        foreach (ServerClient dc in disconnectedClients)
        {
            connectedClients.Remove(dc);
        }
        disconnectedClients.Clear();
    }
    public  void BroatCastType(int type, string data){
        if (!serverStarted){
            return;
        }
        foreach (ServerClient sc in connectedClients){
            if (!sc.IsConnected()){
                sc.socket.Close();
                OnClientDisconnect(sc);
                disconnectedClients.Add(sc);

            }
            else{
                if (sc.type == type){
                    Send(sc, data);
                }
            }
        }

        //clear up Disconnected Clients
        foreach (ServerClient dc in disconnectedClients)
        {
            connectedClients.Remove(dc);
        }
        disconnectedClients.Clear();
    }
    public  void Send(int ID, string data){
        foreach(ServerClient sc in connectedClients)
        {
            if (sc.ID == ID){
                Send(sc, data);
                break; //no need to iterate the rest
            }
        }
        //TODO: Create Send
    }
    private void Send(ServerClient sc, string message){
        Send(sc, new ServerMessage(message, sc.type, sc.ID));
    }
    private void Send(ServerClient sc, ServerMessage message){
        byte[] byteMessage = Encoding.ASCII.GetBytes(JsonUtility.ToJson(message));
        sc.socket.BeginSend(byteMessage, 0, byteMessage.Length, 0, new AsyncCallback(SendCallback), sc.socket);
    }
    private void SendCallback(IAsyncResult ar){
        try {
            Socket handler = (Socket)ar.AsyncState;
            int bytesSent = handler.EndSend(ar);
            Debug.Log("Message successfull sent with " + bytesSent + " bytes");

        }
        catch (Exception e) {
            Debug.Log("Message hasn't been sent: " + e.Message);
        }
    }
    
    // Server Stop
    private void StopServer()
    {
        loopKiller = false;
        serverStarted = false;
        
        // Disconnect all clients
        foreach (ServerClient sc in connectedClients)
        {
            sc.socket.Close();
            disconnectedClients.Add(sc);
        }
        foreach (ServerClient dc in disconnectedClients)
        {
            connectedClients.Remove(dc);
        }
        disconnectedClients.Clear();

        // Clear Server
        serverSocket.Close();
    }
    private void OnApplicationQuit()
    {
        StopServer();
    }
    private void OnDestroy()
    {
        StopServer();
    }
    private void OnDisable()
    {
        StopServer();
    }

    //Specific functions
    public void setClientType(int ID, int type){
        foreach (ServerClient sc in connectedClients){
            if(sc.ID == ID){
                sc.type = type;
            }
        }
    }

}


/*

   public bool IsConnected()
   {
       //get { return tcpListenerThread != null && tcpListenerThread.IsAlive};
        return tcpListener != null && tcpListener.Server.IsBound;
   }


   private TcpListener tcpListener;

   private List<ConnectedClient> connectedClients = new List<ConnectedClient>();

   public void StartServer(string IPAddress, int Port)
   {
       this.IPAddress = IPAddress;
       this.Port = Port;
       try
       {
           tcpListener = new TcpListener(System.Net.IPAddress.Any, Port);
           tcpListener.Start();

           ThreadPool.QueueUserWorkItem(ListenerWorker, tcpListener);

           Debug.Log("Server started at: " + this.IPAddress + ":" + this.Port);
       }
       catch (SocketException socketException)
       {
           Debug.Log("SocketException " + socketException);
       }
   }
   private void ListenerWorker(object token)
   {
       while (tcpListener != null)
       {
           TcpClient tcpClient = tcpListener.AcceptTcpClient();


           ThreadPool.QueueUserWorkItem(HandleClientWorker, tcpClient);
       }
       Debug.Log("tcpListener went out");
   }
   private void HandleClientWorker(object token)
   {
       Byte[] bytes = new Byte[Controller.BUFFERSIZE];
       using (TcpClient client = token as TcpClient)
       {
           SimClient simclient = new SimClient();
           simclient.ID = ++SimClient.MAX_ID;
           simclient.name = "TMP" + simclient.ID;



           ConnectedClient connectedClient = new ConnectedClient(simclient, client);
           connectedClients.Add(connectedClient);

           //Just send a random message
           SendMessageToClient(simclient.ID, "Welcome Stranger, who are you?");

           try
           {
               using (NetworkStream stream = client.GetStream())
               {
                   int length;
                   while (stream.CanRead && (length = stream.Read(bytes, 0, bytes.Length)) != 0)
                   {
                       var incomingData = new byte[length];
                       Array.Copy(bytes, 0, incomingData, 0, length);
                       string clientMessage = Encoding.ASCII.GetString(incomingData);

                       / *if (clientMessage == "!disconnect")
                       { 
                           stream.Close();
                           client.Close();
                       }* /

                       ProcessMessage(connectedClient, clientMessage);
                   }
                   Debug.Log("ich bin ausgelaufen");
               }
           }
           catch (SocketException e)
           {
               Debug.Log(e.ToString());
           }
       }
       Debug.Log("Ich bin ausgelaufen 2");
   }
   private void ProcessMessage(ConnectedClient connectedClient, string command)
   {
       string[] split = command.Split('|');
       string response = string.Empty;

       switch (split[0])
       {
           case Controller.RESDISPLAY: //Resond Display
               connectedClient.simClient.name = split[1]; //Assign correct name
               OnLog(split[0]+"|"+connectedClient.simClient.ID +"|"+ split[1]);
               break;
           case "!disconnect":
               response = (string.Format("{0} has Disconnected", connectedClient.simClient.name));
               Debug.Log(response);
               DisconnectClient(connectedClient);
               break;
           default:
               OnLog(command);
               break;
       }

   }

   private void CheckForData(TcpClient c)
   {
       Debug.Log("still connected");
       Byte[] bytes = new Byte[Controller.BUFFERSIZE];
       //begin receiving data from the client
       c.Client.BeginReceive(bytes, 0, Controller.BUFFERSIZE, 0, ReadCallback, c);

   }
   void ReadCallback(IAsyncResult ar)
   {

       Debug.Log("test");

   }

   public void serverUpdate()
   {
       connectedClients.ForEach(delegate (ConnectedClient c)
       {

           if (c.tcpClient != null && c.tcpClient.Connected)
           {
               CheckForData(c.tcpClient);
           }
           else
           {
               c.tcpClient.Close(); //close the socket
               connectedClients.Remove(c);
               Debug.Log("Client disconnected");
               //disconnectList.Add(sc);
               //continue;
           }
       });
   }

   private void DispatchMessage(ServerMessage serverMessage)
   {
       for (int i = 0; i < connectedClients.Count; i++)
       {
           ConnectedClient connection = connectedClients[i];
           TcpClient client = connection.tcpClient;
           if (!SendMessage(client, serverMessage))
           {
               Debug.Log(string.Format("Lost connection with {0}", connection.simClient.name));
               DisconnectClient(connection);
               i--;
           }
       }
   }
   private void DisconnectClient(ConnectedClient connection)
   {
       OnClientDisconnect(connection);
       connectedClients.Remove(connection);
   }
   public void SendMessageToClient(int clientID, string message)
   {
       ServerMessage tmp;
       Debug.Log("Start sending Message to " + clientID);

       connectedClients.ForEach(delegate (ConnectedClient c)
       {
           if (c.simClient.ID == clientID)
           {
               tmp = new ServerMessage(c.simClient, message);
               if(!SendMessage(c.tcpClient, tmp)){
                   Debug.Log("Error sending Message" + message);
               }
           }
       });
   }
   public void SendMessageToAllClients(string message)
   {
       ServerMessage tmp;
       try
       {
           connectedClients.ForEach(delegate (ConnectedClient c)
           {
               Debug.Log(message + " to " + c.simClient.name);
               tmp = new ServerMessage(c.simClient, message);
               TcpClient tempClient = c.tcpClient;
               if (!SendMessage(tempClient, tmp))
               {
                   Debug.Log(string.Format("Lost connection with {0}", c.simClient.name));
                   DisconnectClient(c);
               }
           });
       }
       catch(Exception e)
       {
          // TODO: This exception will be thrown always when a client got removed, not nice
          //Debug.Log("Client got disconnected: " +e);
       }
   }
   private bool SendMessage(TcpClient client, ServerMessage serverMessage)
   {
       if (client != null && client.Connected)
       {
           try
           {
               NetworkStream stream = client.GetStream();
               if (stream.CanWrite)
               {
                   //Create Json Byte Array from Serializable Object 
                   byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(JsonUtility.ToJson(serverMessage));
                   stream.WriteAsync(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
                   Debug.Log("Message sent:" + serverMessage.message);
                   return true;
               }
           }
           catch (SocketException socketException)
           {
               Debug.Log("Socket exception: " + socketException);
           }
       }
       else
       {
           // The client is null or not connected anymore
           // In this case the connection got dropped
           Debug.Log("It kicks me out here");
       }
       return false;
   }

   */
