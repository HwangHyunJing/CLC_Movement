using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetailedSpawner : MonoBehaviour
{

    public GameObject detailed;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 39; i < 68; i++)
            Instantiate(detailed, new Vector3(i, 27, 60), detailed.transform.rotation);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
