using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class WSDPosition : MonoBehaviour
{
    public Dropdown loadVideoDropDown;
    private Controller controller;

    private List<string> defaultList = new List<string>(){
        "Load Setup",
        "Complete WSD",
        "Before Car" };

    public void Start()
    {
        controller = Controller.getController();
        AttachList();
    }
    private void AttachList()
    {
        loadVideoDropDown.AddOptions(defaultList);
    }

    public void changeState(int index)
    {
        controller.changeWSDDefault(index);
    }
}