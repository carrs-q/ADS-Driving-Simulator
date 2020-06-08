using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class WSDPosition : MonoBehaviour
{
    public Dropdown loadVideoDropDown;
    private Controller controller;

    private List<string> defaultList = new List<string>(){
        "Load Setup",
        "ARC Linkage" };

    public void Start()
    {
        controller = Controller.getController();
        AttachList();
    }
    private void AttachList()
    {
        this.GetComponent<Dropdown>().AddOptions(defaultList);
    }

    public void changeState(int index)
    {
        if (index != 0)
        {
            controller.ChangeWSDDefault(index);
            this.GetComponent<Dropdown>().value = 0;
        }
    }
}