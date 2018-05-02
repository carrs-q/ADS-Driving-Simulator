using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using SFB;


public class Audio : MonoBehaviour {

    private List<string> names = new List<string>() { "Load Audio", "Right Mirror", "Left Mirror" };
    private Controller controller;
    public Dropdown audiDrowpdown;

    // Use this for initialization
    void Start () {
        controller = Controller.getController();
        AttachList();

    }

    private void AttachList()
    {
        audiDrowpdown.AddOptions(names);
    }

    public void loadAudioData(int index)
    {
        if (index != 0 && index < 6)
        {
            var extensions = new[] {
                new ExtensionFilter("Load Simulation Audio", "ogg", "mp3"),
            };
            var path = StandaloneFileBrowser.OpenFilePanel("Load Simulation Audio", "", extensions, true);
            if (path.Length > 0)
            {
                string newPath = WWW.UnEscapeURL(path[0].Replace("file://", ""));
                switch (index)
                {
                    case 1:
                    case 2:
                        {
                            controller.loadAudioSource(index, newPath);
                        };  break;
                    default:
                        {
                            controller.writeError("Index out of Bound - Audio");
                        }
                        break;
                }
            }
        }
        audiDrowpdown.value = 0;
    }
}
