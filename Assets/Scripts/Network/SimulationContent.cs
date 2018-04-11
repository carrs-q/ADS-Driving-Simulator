using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class SimulationContent: MonoBehaviour{
    private static string FRONTWALL = "wf";
    private static string LEFTWALL = "wl";
    private static string RIGHTWALL = "wr";
    private static string MIRRORBACK = "mb";
    private static string MIRRORLEFT = "ml";
    private static string MIRRORRIGHT = "mr";
    private static string NAVIGATION = "nav";

    private string rootFolder;
    private string assetFolder;
    private string completePath;
    private bool ready;
    private List<MyFile> fileList;

    public SimulationContent(string assetFolder)
    {
        this.fileList = new List<MyFile>();
        this.rootFolder = Application.persistentDataPath;
        this.assetFolder = assetFolder;
        this.completePath = this.rootFolder+"/"+ this.assetFolder;
        if (!Directory.Exists(completePath))
        {
            Debug.Log("Directory created");
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
                    allFilesLoaded = true;
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
    
}
