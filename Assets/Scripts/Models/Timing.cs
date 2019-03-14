using System;

public class Timing {
    private int hours, minutes, seconds, milliseconds;
    private bool set;

    public Timing(string hours, string minutes, string seconds, string milliseconds)
    {
        this.hours = checkInt(hours);
        this.minutes = check60(minutes);
        this.seconds = check60(seconds);
        this.milliseconds = check1000(milliseconds);
        set = true;
    }
    public Timing(string hours, string minutes, string seconds )
    {
        this.hours = checkInt(hours);
        this.minutes = check60(minutes);
        this.seconds = check60(seconds);
        this.milliseconds = 0;
        set = true;

    }
    public Timing(Int64 millis)
    {
        this.hours = TimeSpan.FromMilliseconds(millis).Hours;
        this.minutes = TimeSpan.FromMilliseconds(millis).Minutes;
        this.seconds = TimeSpan.FromMilliseconds(millis).Seconds;
        this.milliseconds = TimeSpan.FromMilliseconds(millis).Milliseconds;

    }
    public Timing()
    {
        this.hours = 0;
        this.minutes = 0;
        this.seconds = 0;
        this.milliseconds = 0;
        set = false;
    }

    public int getHours()
    {
        return this.hours;
    }
    public int getMinutes()
    {
        return this.minutes;
    }
    public int getSeconds()
    {
        return this.seconds;
    }
    public int getMilliseconds()
    {
        return this.milliseconds;
    }

    public bool isBehind(Timing timing)
    {
        if (this.hours <= timing.getHours())
        {
            if(this.minutes <= timing.getMinutes())
            {
                if(this.seconds <= timing.getSeconds())
                {
                    if(this.milliseconds <= timing.getMilliseconds())
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
    public bool isSet()
    {
        return this.set;
    }

    public string getTiming()
    {
        string sh, sm, ss, sms;

        if (this.hours < 10)
        {
            sh = "0" + this.hours;
        }
        else
        {
            sh = this.hours.ToString();
        }

        if (this.minutes < 10)
        {
            sm = "0" + this.minutes;
        }
        else
        {
            sm = this.minutes.ToString();
        }

        if (this.seconds < 10)
        {
            ss = "0" + this.seconds;
        }
        else
        {
            ss = this.seconds.ToString();
        }

        if (this.milliseconds < 10)
        {
            sms = "00" + this.milliseconds;
        }
        else if (this.milliseconds < 100)
        {
            sms = "0" + this.milliseconds;
        }
        else
        {
            sms = this.milliseconds.ToString();
        }
        return sh + ":" + sm + ":" + ss + ":" + sms;
    }
    public string getDifference(Int64 diff)
    {
        if (set)
        {
            Int64 temp = this.getTotalMillis();
            if (diff >= temp)
            {
                return Labels.torFired;
            }
            Timing temp2 = new Timing((diff - temp) * -1);
            return temp2.getTiming();
        }
        return this.getTiming();
    }
    public int getTotalMillis()
    {
        int total = 0;
        total += (this.milliseconds);
        total += (this.seconds * 1000);
        total += (this.minutes * 60000);
        total += (this.hours * 3600000);
        return total;
    }
    public int getTotalSeconds()
    {
        return getTotalMillis()/1000;
    }
    private int check60(string val)
    {
        int value = checkInt(val);
        if(!(value>=0 && value <= 59))
        {
            value = 0;
        }
        return value;
    }
    private int check1000(string val)
    {
        int value = checkInt(val);
        if (!(value >= 0 && value <= 999))
        {
            value = 0;
        }
        return value;
    }
    private int checkInt(string val)
    {
        int value;
        if(int.TryParse(val, out value))
        {
            return value;
        }
        return 0;
    }
}
