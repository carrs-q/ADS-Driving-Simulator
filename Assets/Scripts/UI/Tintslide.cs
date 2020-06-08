using UnityEngine;
using System;
using UnityEngine.UI;

public class Tintslide : MonoBehaviour {
    private Controller controller;
    public Slider slider;
    
    public void sliderMoved(Single state)
    {
        controller = Controller.getController();
        controller.SetTintState(state);
    }
    public void setSlider(Single state)
    {
        this.setSlider(state);
    }

    public void Update()
    {
        float wheel = Input.GetAxis("Mouse ScrollWheel")*20;
        slider.value += wheel;
    }

}
