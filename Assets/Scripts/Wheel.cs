using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wheel : MonoBehaviour
{
    private float _rotation_x = 0.0f;
    private float _car_speed = 0.0f;

    [SerializeField]
    private GameObject _ego_car;
    
    [SerializeField]
    private float _rotation_factor = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        //GameObject _ego_car = GameObject.FindWithTag("ego_car");
        
                
        if (_ego_car == null)
        {
            Debug.Log("Error: No ego car in scene to get speed for wheels");
        }
    }

    // Update is called once per frame
    void Update()
    {        
        _rotation_x += _ego_car.GetComponent<egoCar>().GetSpeed() * _rotation_factor;
        Mathf.Clamp(_rotation_x, -360, 360);        
        transform.eulerAngles = new Vector3(_rotation_x, transform.eulerAngles.y, transform.eulerAngles.z);
    }
    void ForwardCarSpeed(float speed)
    {
        _car_speed = speed;
    }

}
