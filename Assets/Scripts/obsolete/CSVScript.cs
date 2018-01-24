using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SFB;

public class CSVScript : MonoBehaviour {
	public Text loadCSV;
	public Transform arDisplay;

    private float[] obdData;
    private int obdDataCount;

    private bool isLoaded;
    private bool obdRunning;
    private int actualFrame;
    public Text LogText;

    private float initHMD;
    
    public void csvClicked() {

		var extensions = new [] {
			new ExtensionFilter("OBD Data", "csv"),
			new ExtensionFilter("All Files", "*" ),
		};

		var path = StandaloneFileBrowser.OpenFilePanel ("Load Steeringwheel Data", "", extensions, true);
		string newPath = WWW.UnEscapeURL (path [0].Replace ("file://", ""));

        if (path[0] != "") {
            string fileData;
            string[] lines;
            fileData = System.IO.File.ReadAllText (newPath);
			lines = fileData.Split ("\n" [0]);
            obdDataCount = lines.Length;
            obdData = new float[obdDataCount];
            string temp;
            for (int i=0; i< obdDataCount - 1; ++i)
            {
                temp = lines[i].Split(";"[0])[1].Trim();
                if (temp.Length < 6)
                {
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
                } 
                else
                {
                    LogText.text += "\n" + "Error: #CSVS-2";
                }
                //LogText.text += "\n" + string.Format("{0:N0}", obdData[i]);

            }
            LogText.text += "\n" + "CSV has been loaded successfully";
            isLoaded = true;
		}
	}
     
    public void runOBD()
    {
        this.obdRunning = true;
    }

    public void stopOBD()
    {
        this.obdRunning = false;
    }
    public void Reset()
    {
        this.actualFrame = 0;
    }

    void Start()
    {
        isLoaded = false;
        obdRunning = true; // just for testing true
        initHMD = 0f;
        actualFrame = 0;

    }
     
    // Update is called once per frame
    void Update()
    {
        if (isLoaded && obdRunning)
        {
            if (actualFrame < obdDataCount)
            {
                arDisplay.transform.position = new Vector3(initHMD + (float)(0.002 * obdData[actualFrame]), 5.5f, 7f);
                actualFrame++;
            }
            else {
                actualFrame = 0;
                obdRunning = false;
            }
        }
    }
}
