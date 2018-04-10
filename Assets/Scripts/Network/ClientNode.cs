public class ClientNode{
    private int connectionID;
    private int displayID;

    public ClientNode(int connectionID, int displayID)
    {
        this.connectionID = connectionID;
        this.displayID = displayID;
    }

    public void setdisplazID(int displayID)
    {
        this.displayID = displayID;
    }

    public int getConnectionID()
    {
        return this.connectionID;
    }
    public int getDisplayID()
    {
        return this.displayID;
    }

}
