using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimulatorDropdown : MonoBehaviour {

    public Dropdown loadVideoDropDown;
    private Controller controller;

    private List<string> modes = new List<string>() { "Choose Destination", "Main Simulator", "Virtual Reality"};

    public void Start()
    {
        controller = Controller.getController();
        AttachList();
    }
    private void AttachList()
    {
        loadVideoDropDown.AddOptions(modes);
    }

    public void changeSimulatorMode(int index)
    {
        if (index != 0 && index < 3)
        {
            controller.ChangeMode(index);
            controller.WriteLog("Destination " + modes[index]);
        }
    }
}
