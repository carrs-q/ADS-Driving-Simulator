using System;
using UnityEngine;
using UnityEngine.UI;

public class overTake : MonoBehaviour
{
    private Controller controller;

    public void Start()
    {
        controller = Controller.GetController();
    }
    public void userTakeOver()
    {
        controller.TakeOverRequest(DateTime.Now);
    }
}
