using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class projectList : MonoBehaviour {

    public Dropdown loadVideoDropDown;
    private Controller controller;
    public static string chooseProject = "Choose Project";
    public static string loadProject = "Reload Projects";

    private List<string> names = new List<string>() { chooseProject, loadProject };

    public void Start()
    {
        controller = Controller.getController();
        AttachList();
    }
    private void AttachList()
    {
        loadVideoDropDown.ClearOptions();
        loadVideoDropDown.AddOptions(names);
    }

    public void addList(string[] list)
    {
        names.Clear();
        names.Add(chooseProject);
        names.AddRange(list);
        names.Add(loadProject);
        AttachList();
    }

    public void changeProject(int index)
    {
        if (index != 0 && index != names.Count-1)
        {
            controller.loadProject(names[index]);
        }
        else if (names[index] == loadProject)
        {
            loadVideoDropDown.value = 0;
            controller.loadProjectList();
        }
    }
}
