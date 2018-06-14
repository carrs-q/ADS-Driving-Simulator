using System.Collections;
using System.IO;
using UnityEngine;
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
        bool downLoadPending = false;
        if (File.Exists(this.directory + this.filename))
        {
            UnityWebRequest wrq = UnityWebRequest.Head(this.filesource);
            wrq.chunkedTransfer = false;
            wrq.SendWebRequest();

            while (!wrq.isDone)
            {
                yield return null;
            }

            if (wrq.responseCode==404)
            {
                Debug.Log("Resource does not exist on CDN, I delete it :) ");
                File.Delete(Path.Combine(this.directory, this.filename));
                downLoadPending = true;
            }
            else if (wrq.responseCode == 200)
            {
                long localFileSize = new FileInfo(Path.Combine(this.directory, this.filename)).Length;
                long remoteFileSize = long.Parse(wrq.GetResponseHeader("content-length"));
                if (localFileSize != remoteFileSize)
                {
                    Debug.Log("File Update: " + filename);
                    File.Delete(Path.Combine(this.directory, this.filename));
                    downLoadPending = true;
                }
                else
                {
                    downloaded = true;
                    ready = true;
                    fileNotExist = false;
                }
            }
            else
            {
                Debug.Log("Some other code");
                downloaded = true;
                ready = true;
                fileNotExist = false;
            }
        }
        else
        {
            downLoadPending = true;
        }

        if(downLoadPending)
        {
            download = true;
            if (!Directory.Exists(this.directory))
            {
                Directory.CreateDirectory(this.directory);
            }
            var uwr = new UnityWebRequest(this.filesource);
            uwr.method = UnityWebRequest.kHttpVerbGET;
            var dh = new DownloadHandlerFile(Path.Combine(this.directory, this.filename));
            dh.removeFileOnAbort = true;
            uwr.downloadHandler = dh;
            yield return uwr.SendWebRequest();


            if(uwr.isNetworkError || uwr.isHttpError)
            {
                download = false;
                checkPending = false;
                if (File.Exists(Path.Combine(this.directory, this.filename)))
                {
                    File.Delete(Path.Combine(this.directory, this.filename));
                }
                Debug.Log(uwr.error);
            }
            else
            {
                downloaded = true;
                download = false;
                checkPending = false;
                ready = true;
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
