using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using SFB;

public class LoadVideoButton : MonoBehaviour
{
    public Dropdown loadVideoDropDown;
    private Controller controller;

    private List<string> names = new List<string>() { "Load Video", "Front", "Left", "Right", "HMD/Nav", "Mirror Back", "Mirror Left", "Mirror Right" };

    public void Start()
    {
        controller = Controller.GetController();
        AttachList();
    }
    private void AttachList()
    {
        loadVideoDropDown.AddOptions(names);
    }

    public void LoadVideo(int index){
        if(index!=0 && index < 8)
        {
            var extensions = new[] {
                new ExtensionFilter("Video Files", "mp4" ),
                new ExtensionFilter("All Files", "*" ),
            };
            var path = StandaloneFileBrowser.OpenFilePanel("Open Video", "", extensions, true);
            if (path.Length > 0)
            {
                string newPath = WWW.UnEscapeURL(path[0].Replace("file://", ""));
               

                controller.LoadVideotoPlayer(index - 1, newPath);
            }
        }
        if (index >=5)
        {
            controller.WriteLog("Display is currently not available");
        }
        loadVideoDropDown.value = 0;
    }
}