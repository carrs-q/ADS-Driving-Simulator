using System.Net.Sockets;
using System.Text;

public class StateObject
{
    public Socket workSocket = null;                    // Client  socket.  
    public const int BufferSize = 1024;                 // Size of receive buffer.  
    public byte[] buffer = new byte[BufferSize];        // Receive buffer. 
    public StringBuilder sb = new StringBuilder();      // Received data string.  
}