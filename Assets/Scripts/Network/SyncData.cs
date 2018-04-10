using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;

public class SyncData : NetworkBehaviour
{

    private void Start()
    {
        if (!isServer) // Does Client
        {
            //CmdchangeState(0);
        }
        else
        {

        }
    }

    [SyncVar]
    public int 
        simulationState = 0,
        currentSpeed =0,
        currentSteeringWheelRotation = 0;

    [Command]
    void CmdchangeState(int state)
    {

    }

    [ClientRpc]
    void RpcUpdateSimState(int state)
    {
        this.simulationState = state;
        Debug.Log("Client listen");
    }


    
    //On Send
    public override bool OnSerialize(NetworkWriter writer, bool forceAll)
    {
        Debug.Log("I Changed something " + simulationState);
        RpcUpdateSimState(simulationState);
        /*
         *   if (forceAll)
        {
            writer.WritePackedUInt32((uint)this.simulationState);
            writer.WritePackedUInt32((uint)this.currentSpeed);
            writer.WritePackedUInt32((uint)this.currentSteeringWheelRotation);
            Debug.Log("Initial Data written");
            return true;
        }
        bool wroteSyncVar = false;
        if((this.get_syncVarDirtyBits & 1u) != 0u){
            if (!wroteSyncVar)
            {
                writer.WritePackedUInt32(value: this.get_syncVarDirtyBits);
                wroteSyncVar = true;
            }
            writer.WritePackedUInt32((uint)this.simulationState);
        }

        if (!wroteSyncVar)
        {
            writer.WritePackedUInt32(0);
        }
        else
        {
            Debug.Log("Write Data");
        }
        */

        return true;
    }

    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {

        //On Recieve
        /*
        if (initialState)
        {
            this.simulationState = (int)reader.ReadPackedUInt32();
            this.currentSpeed = (int)reader.ReadPackedUInt32();
            this.currentSpeed = (int)reader.ReadPackedUInt32();
            Debug.Log("Read Initial");

        }
        this.simulationState = (int)reader.ReadPackedUInt32();
        */
    }
}
