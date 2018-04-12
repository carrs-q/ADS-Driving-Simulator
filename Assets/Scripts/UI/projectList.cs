using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class projectList : MonoBehaviour {

    public Dropdown loadVideoDropDown;
    public Text LogText;
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
        names.AddRange(list);
        loadVideoDropDown.ClearOptions();
        AttachList();
    }

    public void changeProject(int index)
    {
        Debug.Log(names[index]);
        controller.loadProject(names[index]);
    }
}
