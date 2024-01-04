using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (!ENet.Library.Initialize()) {
            Debug.LogError("Cannot initialize ENet!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
