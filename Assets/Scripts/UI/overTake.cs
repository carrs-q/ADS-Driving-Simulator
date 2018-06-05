using System;
using UnityEngine;
using UnityEngine.UI;

public class overTake : MonoBehaviour
{
    private Controller controller;

    public void Start()
    {
        controller = Controller.getController();
    }
    public void userTakeOver()
    {
        controller.takeOverRequest(DateTime.Now);
    }
}
