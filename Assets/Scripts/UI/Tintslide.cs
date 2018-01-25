using UnityEngine;
using System;

public class Tintslide : MonoBehaviour {
    private Controller controller;
    
    public void sliderMoved(Single state)
    {
        controller = Controller.getController();
        controller.setTintState(state);
    }
    public void setSlider(Single state)
    {
        this.setSlider(state);
    }
}
