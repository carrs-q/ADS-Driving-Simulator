using UnityEngine.UI;

public class Log {
    public Text log;

    public Log(Text textField)
    {
        log = textField;
    }

    private string generateString(string text)
    {
        int m = System.DateTime.Now.Minute;
        int h = System.DateTime.Now.Hour;
        int s = System.DateTime.Now.Second;
        string ss, mm, hh;

        if (m < 10)
        {
            mm = "0" + m;
        }
        else
        {
            mm = m.ToString();
        }

        if (h < 10)
        {
            hh = "0" + h;
        }
        else
        {
            hh = h.ToString();
        }
        if (s < 10)
        {
            ss = "0" + s;
        }
        else
        {
            ss = s.ToString();
        }
        return "\t\t" + hh + ":" + mm + ":" + ss + "\t-\t " + text + "\n";
    }

    public void write(string text)
    {
        log.text =  this.generateString(text) + log.text;
    }
    public void writeWarning(string text)
    {
        log.text = "<color=#ffff00ff>" + this.generateString(text) + "</color>" + log.text;
    }

    public void writeError(string text)
    {
        log.text = "<color=#FF4D45FF><b>" + this.generateString(text) + "</b></color>" + log.text;
    }

}
