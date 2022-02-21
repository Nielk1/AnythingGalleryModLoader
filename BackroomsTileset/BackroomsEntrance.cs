using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackroomsEntrance : MonoBehaviour
{
    public Jigsaw.Connector DoorConnector;
    public Light DoorLight;

    private bool Done = false;

    // Start is called before the first frame update
    void Start()
    {
        if (DoorConnector.connected)
        {
            Done = true;
        }
        else
        {
            //DoorLight.enabled = false;
            DoorLight.intensity = 0;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Done) return;
        if(DoorConnector.connected && !DoorConnector.fallbackConnected)
        {
            //DoorLight.enabled = true;
            DoorLight.intensity = 1;
            Done = true;
        }
    }
}
