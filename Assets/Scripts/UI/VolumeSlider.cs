using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour {
    public Text valueText;
    private Controller controller;

    void Start()
    {
        controller = Controller.getController();
        valueText.text = ((int)this.GetComponent<Slider>().value).ToString();
    }

    public void onValueChange(float value)
    {
        valueText.text = ((int)value).ToString();
    }

    public void onMouseRelease()
    {
        controller.changeVolume(this.name, (int)this.GetComponent<Slider>().value);
    }
}
