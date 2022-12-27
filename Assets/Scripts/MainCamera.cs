using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamera : MonoBehaviour
{
    [SerializeField]
    private GameObject _ego_car;

    private Vector3 relative_cam_position;
    // Start is called before the first frame update
    void Start()
    {
        relative_cam_position = transform.position;

        transform.position = new Vector3(-2, 8, -17);
        //transform.eulerAngles = new Vector3(20, 20, 0);
        _ego_car = GameObject.FindWithTag("ego_car");
        if (_ego_car == null)
        {
            Debug.Log("Error: No ego car in scene to track with camera");
        }

    }


    // Update is called once per frame
    void Update()
    {        
        //Follow with camera, so that we have always fixed position behind car
        Vector3 new_pos;
        new_pos = _ego_car.transform.position;
        new_pos = new_pos + relative_cam_position;
        transform.position = new_pos;
    }      
 
}
