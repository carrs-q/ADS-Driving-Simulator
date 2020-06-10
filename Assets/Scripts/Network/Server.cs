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
    public ClientData clientData;
    public TcpClient client;

    public ConnectedClient(ClientData data, TcpClient client)
    {
        clientData = data;
        this.client = client;
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


public class Server :  MonoBehaviour
{
    public Action<string> OnLog = delegate { };
    public Action<ConnectedClient> OnClientDisconnect = delegate { };


    public bool IsConnected()
    {
        //get { return tcpListenerThread != null && tcpListenerThread.IsAlive};
         return tcpListener != null && tcpListener.Server.IsBound;
    }

    private string IPAddress = "127.0.0.1";
    private int Port = 8052;
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
        try
        {
            // Create listener on localhost port 8052. 			
            tcpListener = new TcpListener(System.Net.IPAddress.Any, Port);
            tcpListener.Start();

            ThreadPool.QueueUserWorkItem(ListenerWorker, tcpListener);
            Debug.Log("Server started at: " + this.IPAddress + ":" + this.Port);
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException);
        }
        // Start TcpServer background thread 		
        /*
        tcpListenerThread = new Thread(ListenForIncomingRequests);
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
        */
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
            ClientData data = new ClientData();
            data.ID = ++ClientData.MAX_ID;
            data.Name = "TMP" + data.ID;



            ConnectedClient connectedClient = new ConnectedClient(data, client);
            connectedClients.Add(connectedClient);
            //SendMessageToClient(data.ID, "Welcome Stranger, who are you?");

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

                        /*if (clientMessage == "!disconnect")
                        { 
                            stream.Close();
                            client.Close();
                        }*/

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
        ServerMessage serverMessage = null;

        switch (split[0])
        {
            case Controller.RESDISPLAY: //Resond Display
                connectedClient.clientData.Name = split[1]; //Assign correct name
                OnLog(split[0]+"|"+connectedClient.clientData.ID +"|"+ split[1]);
                break;
            case "!disconnect":
                response = (string.Format("{0} has Disconnected", connectedClient.clientData.Name));
                Debug.Log(response);
                DisconnectClient(connectedClient);
                break;
            default:
                OnLog(command);
                break;
        }

    }
    

    private void DispatchMessage(ServerMessage serverMessage)
    {
        for (int i = 0; i < connectedClients.Count; i++)
        {
            ConnectedClient connection = connectedClients[i];
            TcpClient client = connection.client;
            if (!SendMessage(client, serverMessage))
            {
                Debug.Log(string.Format("Lost connection with {0}", connection.clientData.Name));
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
            if (c.clientData.ID == clientID)
            {
                tmp = new ServerMessage(c.clientData, message);
                if(SendMessage(c.client, tmp))
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
        try
        {
            connectedClients.ForEach(delegate (ConnectedClient c)
            {
                Debug.Log(message + " to " + c.clientData.Name);
                tmp = new ServerMessage(c.clientData, message);
                TcpClient tempClient = c.client;
                if (!SendMessage(tempClient, tmp))
                {
                    Debug.Log(string.Format("Lost connection with {0}", c.clientData.Name));
                    DisconnectClient(c);
                }
            });
        }
        catch(Exception e)
        {
            //Debug.Log("Client got disconnected: " +e);
        }
    }
    
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
                    Debug.Log("Original Message" + serverMessage.Data);
                    // Convert string message to byte array.                 
                    byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(JsonUtility.ToJson(serverMessage));
                    Debug.Log("Byte Message" + serverMessageAsByteArray.ToString());
                    // Write byte array to socketConnection stream.               
                    stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
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
            Debug.Log("It kicks me out here");
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
    private void OnApplicationQuit()
    {
        StopServer();
    }
    private void OnDestroy()
    {
        StopServer();
    }
}