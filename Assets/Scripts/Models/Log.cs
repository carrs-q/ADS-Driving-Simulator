using UnityEngine.UI;

public class Log {

    private bool storeFile;
    private bool paticipantIsSet;
    private string scenario;
    private string participantcode;

    public Text log;
    public Log(Text textField)
    {
        log = textField;
    }
    private string exactTime()
    {
        int m = System.DateTime.Now.Minute;
        int h = System.DateTime.Now.Hour;
        int s = System.DateTime.Now.Second;
        int ms = System.DateTime.Now.Millisecond;

        string ss, mm, hh, msms;

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
        if (ms < 10)
        {
            msms = "0" + s;
        }
        else
        {
            msms = ms.ToString();
        }
        return hh + ":" + mm + ":" + ss +":" + msms;
    }

    public void init()
    {
        storeFile = false;
        paticipantIsSet = false;
        participantcode = "";
    }
    private string generateString(string text)
    {
        int m = System.DateTime.Now.Minute;
        int h = System.DateTime.Now.Hour;
        int s = System.DateTime.Now.Second;
        int ms = System.DateTime.Now.Millisecond;

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
        if (storeFile)
        {
            generateLogFileString(text);
        }
        log.text =  this.generateString(text) + log.text;
    }
    public void writeWarning(string text)
    {
        if (storeFile)
        {
            generateLogFileString(text);
        }
        log.text = "<color=#ffff00ff>" + this.generateString(text) + "</color>" + log.text;
    }
    public void writeError(string text)
    {
        if (storeFile)
        {
            generateLogFileString(text);
        }
        log.text = "<color=#FF4D45FF><b>" + this.generateString(text) + "</b></color>" + log.text;
    }

    private void generateLogFileString(string text)
    {

    }
}
