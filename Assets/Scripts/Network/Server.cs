using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

[Serializable]
public class ClientData
{
    public static int MAX_ID;

    public int ID;
    public string Name;
}

public class ConnectedClient
{
    public ClientData ClientData;
    public TcpClient Client;

    public ConnectedClient(ClientData data, TcpClient client)
    {
        ClientData = data;
        Client = client;
    }

}

[Serializable]
public class ServerMessage
{
    public ClientData SenderData;
    public string Data;

    public ServerMessage(ClientData client, string message)
    {
        SenderData = client;
        Data = message;
    }
}


public class Server : MonoBehaviour
{
    public Action<string> OnLog = delegate { };

    public bool IsConnected
    {
        get { return tcpListenerThread != null && tcpListenerThread.IsAlive; }
    }

    public string IPAddress = "127.0.0.1";


    public int Port = 8052;

    /// <summary> 	
    /// TCPListener to listen for incoming TCP connection 	
    /// requests. 	
    /// </summary> 	
    private TcpListener tcpListener;

    /// <summary> 
    /// Background thread for TcpServer workload. 	
    /// </summary> 	
    private Thread tcpListenerThread;

    private List<ConnectedClient> connectedClients = new List<ConnectedClient>();

    // Use this for initialization
    public void StartServer(string IPAddress, int Port)
    {
        this.IPAddress = IPAddress;
        this.Port = Port;

        // Start TcpServer background thread 		
        tcpListenerThread = new Thread(ListenForIncomingRequests);
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
        Debug.Log("Server started at: " + this.IPAddress + ":" + this.Port);

    }

    /// <summary> 	
    /// Runs in background TcpServerThread; Handles incoming TcpClient requests 	
    /// </summary> 	
    private void ListenForIncomingRequests()
    {
        try
        {
            // Create listener on localhost port 8052. 			
            tcpListener = new TcpListener(System.Net.IPAddress.Any, Port);
            tcpListener.Start();

            ThreadPool.QueueUserWorkItem(ListenerWorker, null);
            Debug.Log("Server is listening");
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
            var client = tcpListener.AcceptTcpClient();
            ThreadPool.QueueUserWorkItem(HandleClientWorker, client);
        }
        Debug.Log("hmm something went wrong");
    }

    private void HandleClientWorker(object token)
    {
        Byte[] bytes = new Byte[Controller.BUFFERSIZE];
        using (TcpClient client = token as TcpClient)
        {
            ClientData data = new ClientData();
            data.ID = ++ClientData.MAX_ID;
            data.Name = "TMP" + data.ID; 

            ConnectedClient connectedClient = new ConnectedClient(data, client);
            connectedClients.Add(connectedClient);
            
            SendMessageToClient(data.ID, "Welcome Stranger, who are you?");

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

                        if (clientMessage == "!disconnect")
                        { 
                            stream.Close();
                            client.Close();
                        }
        
                        
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
    }

    private void ProcessMessage(ConnectedClient connectedClient, string command)
    {
        string[] split = command.Split('|');
        string response = string.Empty;
        ServerMessage serverMessage = null;

        switch (split[0])
        {
            case Controller.RESDISPLAY: //Resond Display
                connectedClient.ClientData.Name = split[1]; //Assign correct name
                OnLog(split[0]+"|"+connectedClient.ClientData.ID +"|"+ split[1]);
                break;
            case "!disconnect":
                response = (string.Format("{0} has Disconnected", connectedClient.ClientData.Name));
                Debug.Log(response);
                DisconnectClient(connectedClient);
                break;
            case "!ping":
                response = String.Join(" ", split) + " " + (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
                serverMessage = new ServerMessage(connectedClient.ClientData, response);
                SendMessage(connectedClient.Client, serverMessage);
                break;
            default:
                response = "Unknown Command '" + command + "'";
                serverMessage = new ServerMessage(connectedClient.ClientData, response);
                SendMessage(connectedClient.Client, serverMessage);
                break;
        }
    }
    

    private void DispatchMessage(ServerMessage serverMessage)
    {
        for (int i = 0; i < connectedClients.Count; i++)
        {
            ConnectedClient connection = connectedClients[i];
            TcpClient client = connection.Client;
            if (!SendMessage(client, serverMessage))
            {
                Debug.Log("Client Disconnected");
                //Debug.Log(string.Format("Lost connection with {0}", connection.ClientData.Name));
                DisconnectClient(connection);
                i--;
            }
        }
    }

    private void DisconnectClient(ConnectedClient connection)
    {
        connectedClients.Remove(connection);
    }

    public void SendMessageToClient(int clientID, string message)
    {
        ServerMessage tmp;
        connectedClients.ForEach(delegate (ConnectedClient c)
        {
            if (c.ClientData.ID == clientID)
            {
                tmp = new ServerMessage(c.ClientData, message);
                if(SendMessage(c.Client, tmp))
                {
                    Debug.Log("Message sent: " + message);
                }
                else
                {
                    Debug.Log("Error sending Message" + message);
                }
            }
        });
    }

    public void SendMessageToAllClients(string message)
    {
        ServerMessage tmp;
        Debug.Log("Connected Clients: " +connectedClients.Count);
        try
        {
            connectedClients.ForEach(delegate (ConnectedClient c)
            {
                Debug.Log(message + " to " + c.ClientData.Name);
                tmp = new ServerMessage(c.ClientData, message);
                if (!SendMessage(c.Client, tmp))
                {
                    Debug.Log("Client Disconnected");
                    //Debug.Log(string.Format("Lost connection with {0}", connection.ClientData.Name));
                    DisconnectClient(c);
                }
            });
        }
        catch(Exception e)
        {
            Debug.Log("Client got disconnected: " +e);
        }
    }
    
    /// <summary> 	
    /// Send message to client using socket connection. 	
    /// </summary> 	
    private bool SendMessage(TcpClient client, ServerMessage serverMessage)
    {
        if (client != null && client.Connected)
        {
            try
            {
                // Get a stream object for writing. 			
                NetworkStream stream = client.GetStream();
                if (stream.CanWrite)
                {
                    // Convert string message to byte array.                 
                    byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(JsonUtility.ToJson(serverMessage));
                    // Write byte array to socketConnection stream.               
                    stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
                    return true;
                }
                else
                {
                    Debug.Log("I can't write");
                }
            }
            catch (SocketException socketException)
            {
                Debug.Log("Socket exception: " + socketException);
            }
        }
        return false;
    }

    private void StopServer()
    {
        tcpListener.Stop();

        //Disconnect all clients
        connectedClients.ForEach(delegate(ConnectedClient c){
            DisconnectClient(c);
        });

        tcpListenerThread.Abort();
    }

    void OnApplicationQuit()
    {
        StopServer();
    }
    void OnDestroy()
    {
        StopServer();
    }
}