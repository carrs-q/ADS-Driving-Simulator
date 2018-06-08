using System.Collections;
using System.IO;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Networking;

public class MyFile {
    public static int BUFFERSIZE = 8192;

    private string directory;
    private string filesource;

    private string filename;
    private string filetype;

    private int filesize;
    private float progress;

    private bool ready;
    private bool fileNotExist;
    private bool download;
    private bool downloaded;
    private bool checkPending;

    private byte[] data;

    byte[] bytes = new byte[BUFFERSIZE];
    UnityWebRequest webRequest;


    public MyFile(string filesource, string directory)
    {
        this.filesource = filesource;
        this.directory = directory;
        this.filename = Path.GetFileName(filesource);
        this.filetype = Path.GetExtension(filesource);
        this.checkPending = true;
        this.ready = false;
        this.download = false;
        this.downloaded = false;
        this.fileNotExist = true;
    }

    //Non Blocking Download
    public IEnumerator downloadFile()
    {
        
        if (File.Exists(this.directory + this.filename))
        {
            downloaded = true;
            ready = true;
            fileNotExist = false;
        }
        else
        {

            download = true;
            webRequest = new UnityWebRequest(filesource);
            webRequest.downloadHandler = new CustomWebRequest(bytes, this.directory , this.filename);
            webRequest.SendWebRequest();
            while (!webRequest.isDone)
            {
                progress = webRequest.downloadProgress;
                yield return null;
            }
            if (!webRequest.isNetworkError)
            {
                downloaded = true;
                download = false;
                checkPending = false;
                ready = true;
            }
            else
            {

                download = false;
                checkPending = false;
                if (webRequest.error == "404 Not Found")
                    Debug.Log(webRequest.error);
            }
        }
    }

    public float getProgress()
    {
        return this.progress;
    }
    public bool isReady()
    {
        return this.ready;
    }
    public bool doesFileExist()
    {
        return download || downloaded || !fileNotExist;
    }
    public bool isCheckPending()
    {
        return this.checkPending;
    }
    //Check is Pending if file exists

    public string getFilePath()
    {
        return this.directory + this.filename;
    }
    public string getFileName()
    {
        return this.filename;
    }
    public string getFileNameWithoutExt()
    {
        return Path.GetFileNameWithoutExtension(this.getFilePath());
    }

}
