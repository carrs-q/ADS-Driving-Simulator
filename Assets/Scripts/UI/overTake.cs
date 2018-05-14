using UnityEngine;
using UnityEngine.UI;

public class overTake : MonoBehaviour
{
    public Toggle wsdToogle;
    public Toggle xMoveToogle;
    public Toggle Chroma;
    public Toggle Tinting;
    private Controller controller;

    public void Start()
    {
        controller = Controller.getController();
    }
    public void userTakeOver()
    {
        controller.serverTakeOverRequest();
        if (wsdToogle.isOn)
        {
            wsdToogle.isOn = false;
        }
        if (xMoveToogle.isOn)
        {
            xMoveToogle.isOn = false;
        }
        if (Chroma.isOn)
        {
            Chroma.isOn = false;
        }
        if (Tinting.isOn)
        {
            Tinting.isOn = false;
        }

    }
}
