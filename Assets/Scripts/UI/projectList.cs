using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class projectList : MonoBehaviour {

    public Dropdown loadVideoDropDown;
    private Controller controller;

    private List<string> names = new List<string>() { "projectList is Loading" };

    public void Start()
    {
        controller = Controller.getController();
        AttachList();
    }
    private void AttachList()
    {
        loadVideoDropDown.AddOptions(names);
    }
    public void addList(string[] list)
    {
        names.Clear();
        names.Add("Choose Project");
        names.AddRange(list);
        loadVideoDropDown.ClearOptions();
#if (!UNITY_EDITOR)
        AttachList();
#endif
    }

    public void changeProject(int index)
    {
        if (index != 0)
        {
            controller.loadProject(names[index]);
        }
    }
}
