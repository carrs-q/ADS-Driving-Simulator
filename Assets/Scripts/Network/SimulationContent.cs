using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class SimulationContent{
    private string rootFolder;
    private string assetFolder;
    private string completePath;
    private bool ready;
    private List<MyFile> fileList;

    public SimulationContent(string assetFolder, string rootFolder)
    {
        this.fileList = new List<MyFile>();
        this.rootFolder = rootFolder;
        this.assetFolder = assetFolder;
        this.completePath = this.rootFolder+"/"+ this.assetFolder;
        if (!Directory.Exists(completePath))
        {
            Directory.CreateDirectory(completePath);
        }
        ready = false;
    }
    public MyFile addFile(string resource)
    {
        ready = false;
        MyFile newFile = new MyFile(resource, this.completePath+'/');
        fileList.Add(newFile);
        return newFile;
    }
    public string getFilePath(string index)
    {
        foreach(MyFile file in fileList)
        {
            if (file.doesFileExist())
            {
                if (index == file.getFileName())
                {
                    return file.getFilePath();
                }
            }
           
        }
        return null;
    }

    //Important ignores 404 files!
    public bool areFilesReady()
    {
        if (!ready)
        {
            int temp = 0;
            bool allFilesLoaded = true;
            foreach (MyFile f in fileList)
            {
                ++temp;
                if (!f.isReady())
                {
                    if (f.isCheckPending())
                        allFilesLoaded = false;
                    else if (f.doesFileExist())
                        allFilesLoaded = false;
                }
            }
            if (temp == 0)
                return false;
            else
            {
                this.ready = allFilesLoaded;
                return allFilesLoaded;
            }
        }
        else
        {
            return this.ready;
        }
    }
    public float getProgress()
    {
        if (this.areFilesReady())
        {
            return 1;
        }
        else
        {
            float progress = 0;
            int temp = 0;
            foreach(MyFile f in fileList)
            {
                ++temp;
                progress += f.getProgress();
            }
            return progress / temp;
        }
    }
}
