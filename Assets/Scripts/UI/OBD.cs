using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using SFB;


public class OBD : MonoBehaviour {
    private List<string> names = new List<string>(){
        "Load OBD Data",
        "Break Pedal",
        "Is Break Pedal",
        "Is Gas Pedal",
        "Speed",
        "Steering Wheel"};

    private Controller controller;
    public Dropdown loadOBDDropdown;

    private int obdDataCount;

    // Use this for initialization
    public void Start()
    {
        controller = Controller.GetController();
        AttachList();
    }
    private void AttachList()
    {
        loadOBDDropdown.AddOptions(names);
    }
    public void loadOBDData(int index)
    {
        if(index!=0 && index < 7)
        {
            var extensions = new[] {
                new ExtensionFilter("OBD Data", "csv"),
            };
            var path = StandaloneFileBrowser.OpenFilePanel("OBD Data", "", extensions, true);
            if (path.Length > 0)
            {
                string[] lines;
                string fileData;
                string newPath = WWW.UnEscapeURL(path[0].Replace("file://", ""));

                fileData = System.IO.File.ReadAllText(newPath);
                lines = fileData.Split("\n"[0]);
                obdDataCount = lines.Length;
                controller.LoadOBDData(index, bigintOBD(lines), obdDataCount);
                --index;
                switch (index)
                {
                    case 0:
                    case 2:
                        {
                            controller.LoadOBDData(index, floatOBD(lines));
                        }
                        break;
                    case 1:
                        {
                            controller.LoadOBDData(index, boolOBD(lines));
                        }
                        break;
                    case 3:
                    case 4:
                        {
                            controller.LoadOBDData(index, intOBD(lines));
                        }
                        break;
                    case 5:
                        {
                            controller.LoadOBDData(index, intOBD(lines));
                        }
                        break;
                    default:
                        {
                            controller.WriteError("Index out of bound at OBD");
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
                controller.WriteError("Error: #CSVS-1");
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
                    controller.WriteError("Error: #CSVS-1");
                }
            }
            else
            {
                controller.WriteError("Error: #CSVS-2");
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
                controller.WriteError("Error: #CSVS-2");
            }
        }
        return obdData;
    }
    private Int64[] bigintOBD(string[] lines)
    {
        Int64[] obdData = new Int64[obdDataCount];
        string temp;
        Int64 beginnTime=0;
        for (int i = 0; i < obdDataCount - 1; ++i)
        {
            temp = lines[i].Trim().Split(";"[0])[0];
            if (i == 0)
            {
                beginnTime = Int64.Parse(temp);
                obdData[0] = 0;
            }
            else
            {
                try
                {
                    obdData[i] = ((Int64.Parse(temp)) - beginnTime)/1000;
                }
                catch
                {
                    obdData[i] = 0;
                    controller.WriteError("Error: #CSVS-1");
                }
            }
            
        }
        return obdData;
    }
}
