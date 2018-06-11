using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;


//source: https://stackoverflow.com/questions/50689859/www-bytes-overflows-when-downloading-over-2gb
public class CustomWebRequest : DownloadHandlerScript
{
    // Standard scripted download handler - will allocate memory on each ReceiveData callback
    public CustomWebRequest()
        : base()
    {
    }

    // Pre-allocated scripted download handler
    // Will reuse the supplied byte array to deliver data.
    // Eliminates memory allocation.
    public CustomWebRequest(byte[] buffer, string path, string filename)
        : base(buffer)
    {

        Init(path, filename);
    }

    // Required by DownloadHandler base class. Called when you address the 'bytes' property.
    protected override byte[] GetData() { return null; }

    // Called once per frame when data has been received from the network.
    protected override bool ReceiveData(byte[] byteFromServer, int dataLength)
    {
        
        if (byteFromServer == null || byteFromServer.Length < 1)
        {
            return false;
        }
        if(dataLength == 14)
        {
            if(System.Text.Encoding.Default.GetString(byteFromServer).Contains("File not found"))
            {
                //NetworkError.BadMessage
                success = false;
                return false;
            }
            Debug.Log(System.Text.Encoding.Default.GetString(byteFromServer));
        }

        //Write the current data chunk to file
        AppendFile(byteFromServer, dataLength);

        return true;
    }

    //Where to save the video file
    string vidSavePath;
    //The FileStream to save the file
    FileStream fileStream = null;
    bool filestreamOpen = false;

    //Used to determine if there was an error while opening or saving the file
    bool success;
    bool finished;

    void Init(string path, string filename)
    {
        this.finished = false;
        vidSavePath = Path.Combine(Application.persistentDataPath, path);
        vidSavePath = Path.Combine(vidSavePath, filename);

        //Create Directory if it does not exist
        if (!Directory.Exists(Path.GetDirectoryName(vidSavePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(vidSavePath));
        }
    }

    void AppendFile(byte[] buffer, int length)
    {
        if (!filestreamOpen)
        {
            try
            {
                //Open the current file to write to
                filestreamOpen = true;
                fileStream = new FileStream(vidSavePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                success = true;
            }
            catch (Exception e)
            {
                Debug.Log(e);
                filestreamOpen = false;
                success = false;
            }
        }
        if (success)
        {
            if (success)
            {
                try
                {
                    //Write the current data to the file
                    fileStream.Write(buffer, 0, length);
                }
                catch (Exception e)
                {
                    success = false;
                }
            }
        }
    }

    // Called when all data has been received from the server and delivered via ReceiveData
    protected override void CompleteContent()
    {
        if (success)
        {
            finished = true;
        }
        //Close filestream
        if (filestreamOpen)
        {
            fileStream.Close();
            filestreamOpen = false;
        }
        
    }

    // Called when a Content-Length header is received from the server.
    protected override void ReceiveContentLength(int contentLength)
    {
        //Debug.Log(string.Format("CustomWebRequest :: ReceiveContentLength - length {0}", contentLength));
    }
    public bool isFinished()
    {
        return finished;
    }

}