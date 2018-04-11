using System;
using System.Collections;
using System.IO;
using UnityEngine;
using System.Net;

public class MyFile: MonoBehaviour{
    private string directory;
    private string filesource;

    private string filename;
    private string filetype;

    private int filesize;
    private float progress;

    private bool ready;
    private bool download;
    private bool downloaded;


    public MyFile(string filesource, string directory)
    {
        this.filesource = filesource;
        this.directory = directory;
        this.filename = Path.GetFileName(filesource);
        this.filetype = Path.GetExtension(filesource);

        this.ready = false;
        this.download = false;
        this.downloaded = false;
    }
    public IEnumerator downloadFile()
    {
        if (File.Exists(this.directory + this.filename))
        {
            Debug.Log("File " + filename + " exist.");
            downloaded = true;
            ready = true;
        }
        else
        {
            Debug.Log("File " + filename + " does not exist. Start download.");
            //ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            download = true;
            WWW www = new WWW(filesource);
            while(!www.isDone)
            {
                progress = www.progress;
                yield return null;
            }
            if (string.IsNullOrEmpty(www.error))
            {
                File.WriteAllBytes(directory + filename, www.bytes);
                progress = 1.0f;
                download = false;
                downloaded = true;
                ready = true;
                Debug.Log("Download finished for file " + filename + "");
            }
            else
            {
                Debug.Log("Download not possible for file: " + filename + "\n" + www.error);
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
    public string getFilePath()
    {
        return this.directory + this.filename;
    }
    public string getFileNameWithoutExt()
    {
        return Path.GetFileNameWithoutExtension(this.getFilePath());
    }

}
