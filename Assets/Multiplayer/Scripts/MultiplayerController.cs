﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ModelViewer;
using Multiplayer;

public class MultiplayerController : MonoBehaviour
{
    public MultiuserMPOInterface MPO;

    [SerializeField]
    private float _speed = 5.0f;
    public float Speed
    {
        get { return _speed; }
        set { _speed = value; }
    }

    // Update is called once per frame
    void Update()
    {
        // non XR input
        if (Input.GetKey(KeyCode.UpArrow))
            this.transform.position += this.transform.up * Time.deltaTime * Speed;
        if (Input.GetKey(KeyCode.RightArrow))
            this.transform.position += this.transform.right * Time.deltaTime * Speed;
        if (Input.GetKey(KeyCode.DownArrow))
            this.transform.position -= this.transform.up * Time.deltaTime * Speed;
        if (Input.GetKey(KeyCode.LeftArrow))
            this.transform.position -= this.transform.right * Time.deltaTime * Speed;
        if (Input.GetKey(KeyCode.W))
            this.transform.position += this.transform.forward * Time.deltaTime * Speed;
        if (Input.GetKey(KeyCode.S))
            this.transform.position -= this.transform.forward * Time.deltaTime * Speed;

        if (Input.GetKeyUp(KeyCode.Z))
            MPO.ToggleSelect();
        if (Input.GetKeyUp(KeyCode.X))
            MPO.Grab();
        if (Input.GetKeyUp(KeyCode.C))
            MPO.Release();

        // XR input
        if (Input.GetKeyUp(KeyCode.JoystickButton9))
            MPO.ToggleSelect();
        if (Input.GetKeyDown(KeyCode.JoystickButton15))
            MPO.Grab();
        if (Input.GetKeyUp(KeyCode.JoystickButton15))
            MPO.Release();



    }
}
