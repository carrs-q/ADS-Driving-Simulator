using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using SFB;


public class OBD : MonoBehaviour {


    private List<string> names = new List<string>() { "Load OBD Data", "Break Pedal", "Break Boolean", "Gas", "Speed", "Steering Wheel" };
    private Controller controller;
    public Dropdown loadOBDDropdown;
    public Text LogText;

    private int obdDataCount;
    private bool countSet;

    // Use this for initialization
    public void Start()
    {
        controller = Controller.getController();
        countSet = false;
        AttachList();
    }
    private void AttachList()
    {
        loadOBDDropdown.AddOptions(names);
    }
    public void loadOBDData(int index)
    {
        if(index!=0 && index < 6)
        {
            var extensions = new[] {
                new ExtensionFilter("OBD Data", "csv"),
            };
            var path = StandaloneFileBrowser.OpenFilePanel ("OBD Data", "", extensions, true);
            if (path.Length > 0)
            {
                string newPath = WWW.UnEscapeURL(path[0].Replace("file://", ""));
                string fileData;
                string[] lines;
                fileData = System.IO.File.ReadAllText(newPath);
                lines = fileData.Split("\n"[0]);
                obdDataCount = lines.Length;
                if (!countSet)
                {
                    controller.loadOBDData(index, bigintOBD(lines), obdDataCount);
                    countSet = true;
                }
                --index;
                switch (index)
                {
                    case 0:
                    case 2:
                        {
                            controller.loadOBDData(index, floatOBD(lines));
                        }break;
                    case 1:
                        {
                            controller.loadOBDData(index, boolOBD(lines));
                        }
                        break;
                    case 3:
                    case 4:
                        {
                            controller.loadOBDData(index, intOBD(lines));
                        }
                        break;
                    default:{
                            LogText.text = "Index out of bound";
                        }
                        break;
                }
                
            }
        }
        loadOBDDropdown.value = 0;
    }
    private float[] floatOBD(string[] lines)
    {
        float[] obdData = new float[obdDataCount];
        string temp;
        for (int i = 0; i < obdDataCount - 1; ++i)
        {
            temp = lines[i].Trim().Split(";"[0])[1];
           
            try
            {
                obdData[i] = float.Parse(temp);
                //string.Format("{0:N0}", obdData[i])
            }
            catch
            {
                obdData[i] = 0;
                LogText.text += "\n" + "Error: #CSVS-1";
            }
            
            //LogText.text += "\n" + string.Format("{0:N0}", obdData[i]);
        }
        return obdData;
    }
    private int[] intOBD(string[] lines)
    {
        int[] obdData = new int[obdDataCount];
        string temp;
        for (int i = 0; i < obdDataCount - 1; ++i)
        {
            temp = lines[i].Trim().Split(";"[0])[1];
            if (temp.Length < 6)
            {
                try
                {
                    obdData[i] = int.Parse(temp);
                    //string.Format("{0:N0}", obdData[i])
                }
                catch
                {
                    obdData[i] = 0;
                    LogText.text += "\n" + "Error: #CSVS-1";
                }
            }
            else
            {
                LogText.text += "\n" + "Error: #CSVS-2";
            }
            //LogText.text += "\n" + string.Format("{0:N0}", obdData[i]);
        }
        return obdData;
    }
    private bool[] boolOBD(string[] lines)
    {
        bool[] obdData = new bool[obdDataCount];
        string temp;
        for (int i = 0; i < obdDataCount - 1; ++i)
        {
            temp = lines[i].Trim().Split(";"[0])[1];
            try
            {
                if (int.Parse(temp) == 1)
                {
                    obdData[i] = true;
                }
                else
                {
                    obdData[i] = false;
                }
                
            }
            catch
            {
                obdData[i] = false;
                LogText.text += "\n" + "Error: #CSVS-1";
            }
        }
        return obdData;
    }
    private Int64[] bigintOBD(string[] lines)
    {
        Int64[] obdData = new Int64[obdDataCount];
        string temp;
        for (int i = 0; i < obdDataCount - 1; ++i)
        {
            temp = lines[i].Trim().Split(";"[0])[0];
            try
            {
                obdData[i] = Int64.Parse(temp);
            }
            catch
            {
                obdData[i] = 0;
                LogText.text += "\n" + "Error: #CSVS-1";
            }
        }
        return obdData;
    }
}
