using System.Collections;
using System.IO;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;

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
            WWW www = new WWW(filesource);
            while(!www.isDone)
            {
                progress = www.progress;
                yield return null;
            }
            if (string.IsNullOrEmpty(www.error))
            {
                progress = 1.0f;
                downloaded = true;
                download = false;
                checkPending = false;
                data = www.bytes;
                Thread thread = new Thread(saveData);
                thread.Start();
               // Debug.Log("Download finished for file " + filename + "");
            }
            else
            {
                File.WriteAllBytes(getFilePath(), data);
                download = false;
                checkPending = false;
                if (www.error == "404 Not Found")
                    fileNotExist = true;
                else
                    Debug.Log(www.error);
            }
        }
    }

    //Non Blocking Storeing
    private void saveData()
    {
        File.WriteAllBytes(getFilePath(), data);
        ready = true;
        fileNotExist = false;
        data=null;
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
