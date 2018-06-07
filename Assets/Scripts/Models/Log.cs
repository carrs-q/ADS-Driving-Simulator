using UnityEngine;
using UnityEngine.UI;

public class Log {
    private bool storeFile;
    private bool participantIsSet;
    private bool participantIsBelted;
    private bool newEntry=false;
    private string scenario;
    private string unstoredLog;
    private string participantcode;

    public Text log;

    public Log(Text textField)
    {
        log = textField;
    }
    private string exactTime()
    {
        int month = System.DateTime.Now.Month;
        int day = System.DateTime.Now.Day;
        

        int year = System.DateTime.Now.Year;
        int m = System.DateTime.Now.Minute;
        int h = System.DateTime.Now.Hour;
        int s = System.DateTime.Now.Second;
        int ms = System.DateTime.Now.Millisecond;

        string ss, mm, hh, msms, mmonth, mday;

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

        if (month < 10)
        {
            mmonth = "0" + month;
        }
        else
        {
            mmonth = month.ToString();
        }
        if (day < 10)
        {
            mday = "0" + day;
        }
        else
        {
            mday = day.ToString();
        }

        return   "Date: "+ mday + "."+ mmonth + "."+year
            +"\nTime: "+hh + ":" + mm + ":" + ss +":" + msms;
    }
    public void recordingStatus(bool recStat)
    {
        this.newEntry = false;
        this.storeFile = recStat;
    }
    public void safetyRequirements(bool safetyRequireMents)
    {
        this.participantIsBelted = safetyRequireMents;
    }
    public void setParticipantCode(string parCode)
    {
        this.participantIsSet = true;
        this.participantcode = parCode;
    }
    public void setScenario(string scenario)
    {
        this.scenario = scenario;
    }

    public bool isRecording()
    {
        return this.storeFile;
    }
    public bool isSafety()
    {
        return this.participantIsBelted;
    }
    public bool isParticipantSet()
    {
        return this.participantIsSet;
    }

    public void init()
    {
        newEntry = false;
        storeFile = false;
        participantIsSet = false;
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
    public bool isNewLogEntry()
    {
        return this.newEntry;
    }

    public void customRecord(string text)
    {
        unstoredLog =
            "---------------------------\n"
            + "Label: " + this.participantcode + "\n"
            + exactTime() + "\n"
            + text
            + "\n---------------------------"; ;
        newEntry = true;
    }

    public void recordedStart(string text)
    {

        unstoredLog =
            "---------------------------\n"
            + "Status: " + text + "\n"
            + exactTime() 
            + "\nScenario: " + this.scenario
            + "\nParticipant: " + this.participantcode+"\n"
            + "---------------------------\n";
        newEntry = true;

    }
    public string getUnstoredLog()
    {
        this.newEntry = false;
        return unstoredLog;
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
