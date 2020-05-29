using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class Server : MonoBehaviour
{
    public static string data = null;

    #region Network Variables
        private IPAddress ipAddress;
        private int port;
        private ProtocolType protocolType;
        private SocketType socketType;
        private IPHostEntry ipHostInfo;
        private IPEndPoint localEndPoint;
        private static Socket socket;
        private static bool listen = false;
        private Thread socketListener;
    #endregion

    #region Delegate Variables
    protected Action OnServerStarted    = null;  //Delegate triggered when server start
        protected Action OnServerClosed     = null;  //Delegate triggered when server close
        protected Action OnClientConnected  = null;  //Delegate triggered when the server stablish connection with client
        public Action<string> OnLog = delegate { };
    #endregion

    public static ManualResetEvent allDone = new ManualResetEvent(false);

    public Server()
    {
        ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());       // Set Default Hostname
        ipAddress = ipHostInfo.AddressList[0];                  // Set Default IP
        port = 25000;                                           // Set Default Port
        protocolType = ProtocolType.Tcp;                        // Set default to TCP
        socketType = SocketType.Stream;                         // Set default to Stream Socket  
        Debug.Log("Default Network settings.\nHost name" + ipHostInfo.HostName.ToString());

    }

    public void CreateServer()
    {
        StartServer();
    }

    public void CreateServer(IPAddress ipAddress, int port)
    {
        this.ipAddress = ipAddress;
        this.port = port;
        StartServer();
    }

    public void CreateServer(IPAddress ipAddress, int port, ProtocolType protocolType, SocketType socketType)
    {
        this.ipAddress = ipAddress;
        this.port = port;
        this.protocolType = protocolType;
        this.socketType = socketType;
        StartServer();
    }

    private void StartServer()
    {
        //Define IP and Port for Server and bind
        localEndPoint = new IPEndPoint(ipAddress, port);            
        socket = new Socket(ipAddress.AddressFamily, socketType, protocolType);

        try
        {
            socket.Bind(localEndPoint);
            socket.Listen(100);
            listen = true;
            socketListener = new Thread(new ThreadStart(Listen));
            socketListener.Start();

            OnServerStarted?.Invoke();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    private void ServerStop()
    {
        //Shutdown Socket
        Debug.Log("Halt Stopp");
        try
        {
            socket.Shutdown(SocketShutdown.Both);
            listen = false;
        }
        finally
        {
            socket.Close();
            OnServerClosed?.Invoke();
        }
    }

    public static void Listen()
    {
        Debug.Log("Server is now listening");
        try
        {
            while (listen)
            {
                // Set the event to nonsignaled state.  
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.  
                socket.BeginAccept(new AsyncCallback(AcceptCallback), socket);

                // Wait until a connection is made before continuing.  
                allDone.WaitOne();
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    public static void AcceptCallback(IAsyncResult ar)
    {
        // Signal the main thread to continue.  
        allDone.Set();

        // Get the socket that handles the client request.  
        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);

        // Create the state object.  
        StateObject state = new StateObject();
        state.workSocket = handler;
        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
    }  

    public static void ReadCallback(IAsyncResult ar)
    {
        String content = String.Empty;

        // Retrieve the state object and the handler socket  
        // from the asynchronous state object.  
        StateObject state = (StateObject)ar.AsyncState;
        Socket handler = state.workSocket;

        // Read data from the client socket.
        int bytesRead = handler.EndReceive(ar);

        if (bytesRead > 0)
        {
            // There  might be more data, so store the data received so far.  
            state.sb.Append(Encoding.ASCII.GetString(
                state.buffer, 0, bytesRead));

            // Check for end-of-file tag. If it is not there, read
            // more data.  
            content = state.sb.ToString();
            if (content.IndexOf("<EOF>") > -1)
            {
                // All the data has been read from the
                // client. Display it on the console.  
                Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                    content.Length, content);
                // Echo the data back to the client.  
                Send(handler, content);
            }
            else
            {
                // Not all data received. Get more.  
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
            }
        }
    }

    private static void Send(Socket handler, String data)
    {
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        // Begin sending the data to the remote device.  
        handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
    }

    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket handler = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = handler.EndSend(ar);
            Console.WriteLine("Sent {0} bytes to client.", bytesSent);

            handler.Shutdown(SocketShutdown.Both);
            handler.Close();

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }


    void OnApplicationQuit()
    {
        ServerStop();
    }
    void OnDestroy()
    {
        ServerStop();
    }
}