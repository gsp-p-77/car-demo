using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
using SocketInterfaceNameSpace;

public class egoCar : MonoBehaviour
{
    private float _speed = 0.0f;

    private float _input_x_axis = 0.0f;
    private float _input_y_axis = 0.0f;
    private float _input_acceleration_pedal = 0.0f;
    private float _input_steering_wheel_enable = 0.0f;
    private float _ego_speed_meter_per_s = 0.0f;
    private float _acceleration = 0.0f;
    private const float _factor_km_per_h = 3.6f;
    private float _wheel_rotation_speed = 0.0f;
    private float _rotation_y = 0.0f;
    [SerializeField]
    private float _max_speed_km_per_hour = 50.0f;

    [SerializeField]
    private float _max_speed__rev_km_per_hour = 10.0f;

    [SerializeField]
    private float _acceleration_factor = 0.1f;

    [SerializeField]
    private float _steering_factor = 10.0f;

    [SerializeField]
    private float _max_acceleration = 1.0f;

    [SerializeField]
    private GameObject _wheel_fl;

    [SerializeField]
    private GameObject _wheel_fr;

    [SerializeField]
    private GameObject _wheel_rl;

    [SerializeField]
    private GameObject _wheel_rr;

    [SerializeField]
    private GameObject _terrain;

    [SerializeField]
    private GameObject _SocketInterfaceGameObject;

    [SerializeField]
    private GameObject _CanBusInterfaceGameObject;

    private float _nextSendMessage;
    private float _nextReceiveMessage;

    // Start is called before the first frame update
    void Start()
    {
        transform.position = new Vector3(0, 0, 0);
        _nextSendMessage = Time.time + 1.0f ;              
    }

    // Update is called once per frame
    void Update()
    {
      SocketInterface _SocketInterfaceComponent = _SocketInterfaceGameObject.GetComponent<SocketInterface>();
      CanBusInterfaceClass _CanBusInterfaceClass = _CanBusInterfaceGameObject.GetComponent<CanBusInterfaceClass>();
        
        if (_SocketInterfaceComponent == null)
        {
            Debug.Log("_virtualComInterfaceComponent == null");
        }
        else
        {
            if (Time.time > _nextSendMessage)
            {
                _SocketInterfaceComponent.SendMessage( Encoding.ASCII.GetBytes("Speed: 5.0 km/h"));
            }
        }
        if (_CanBusInterfaceClass == null)
        {
            Debug.Log("No CAN bus interface connected");
        }
        else
        {
            //SendSpeed to CAN ID 0x700 (serialize float to an integer value with msb first)
            int ego_speed_int = (int)Math.Round(_ego_speed_meter_per_s * 10);
            byte upper = (byte)(ego_speed_int >> 8);
            byte lower = (byte)(ego_speed_int & 0xff);            
            _CanBusInterfaceClass.SendCanMessage(0x700, new byte[] { upper, lower}, 2);
        }

        if (Time.time > _nextSendMessage)
        {            
            _nextSendMessage = Time.time + 1.0f;
        }

        //_virtualComInterfaceComponent.DebugOut();
        GetInputs();
        UpdatePosition();
        UpdateEgoAngle();
        LimitTerrain();
    }
    public float GetSpeed()
    {
        return _ego_speed_meter_per_s;
    }

    private void GetInputs()
    {
        _input_y_axis = Input.GetAxis("Mouse Y");
        _input_acceleration_pedal = Input.GetAxis("Fire2");
        _input_steering_wheel_enable = Input.GetAxis("Fire1");
        _input_x_axis = Input.GetAxis("Mouse X");
    }

    private void UpdatePosition()
    {
        if (_input_acceleration_pedal == 1)
        {
            _acceleration = _input_y_axis * _acceleration_factor;
        }
        else
        {
            _acceleration = 0.0f;
        }

        _acceleration = Mathf.Clamp(_acceleration, -_max_acceleration, _max_acceleration);

        _ego_speed_meter_per_s += _acceleration;

        _ego_speed_meter_per_s = Mathf.Clamp(_ego_speed_meter_per_s, -_max_speed__rev_km_per_hour / _factor_km_per_h, _max_speed_km_per_hour / _factor_km_per_h);
        
        transform.Translate(0, 0, _ego_speed_meter_per_s * Time.deltaTime);

    }
    private void UpdateEgoAngle()
    {
        if (_input_steering_wheel_enable == 1)
        {
            _rotation_y += _input_x_axis * _steering_factor * _ego_speed_meter_per_s;
        }
        
        transform.eulerAngles = new Vector3(0, _rotation_y, 0);
        //_wheel_fl.transform.eulerAngles = new Vector3(_wheel_fl.transform.eulerAngles.x, _rotation_y, 0);
        //_wheel_fr.transform.eulerAngles = new Vector3(_wheel_fr.transform.eulerAngles.x, _rotation_y, 0);
        //_wheel_rl.transform.eulerAngles = new Vector3(_wheel_rl.transform.eulerAngles.x, _rotation_y, 0);
        //_wheel_rr.transform.eulerAngles = new Vector3(_wheel_rr.transform.eulerAngles.x, _rotation_y, 0);
    }
    private void LimitTerrain()
    {
        Terrain _terrain;
        _terrain = Terrain.activeTerrain;

        //Debug.Log("Terrain size z:= " + _terrain.terrainData.size.z + ", position z := " + transform.position.z);

        if (transform.position.x + 20 > _terrain.terrainData.size.x / 2)
        {
            _ego_speed_meter_per_s = 0.0f;
            transform.position = new Vector3(transform.position.x - 1, 0, transform.position.z);

        }
        else if (transform.position.x - 20 < -_terrain.terrainData.size.x / 2)
        {
            _ego_speed_meter_per_s = 0.0f;
            transform.position = new Vector3(transform.position.x + 1, 0, transform.position.z);
        }


        if (transform.position.z + 20 > _terrain.terrainData.size.z / 2)
        {
            _ego_speed_meter_per_s = 0.0f;
            transform.position = new Vector3(transform.position.x, 0, transform.position.z - 1);

        }
        else if (transform.position.z - 20 < -_terrain.terrainData.size.z / 2)
        {
            _ego_speed_meter_per_s = 0.0f;
            transform.position = new Vector3(transform.position.x, 0, transform.position.z + 1);
        }
    }
}
