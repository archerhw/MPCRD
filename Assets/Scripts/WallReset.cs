﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallReset : MonoBehaviour {
    public GameObject realRoom;
    private Transform player;
    public float resetAngle;

    private void Awake()
    {
        player = GameObject.Find("Player").GetComponent<Transform>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Player")
        { 
            realRoom.GetComponent<Transform>().position = GeneralVector3.RotateCounterClockwise(GeneralVector3.Vector3NoHeight(player.position), realRoom.GetComponent<Transform>().position, resetAngle);
        } 
    }

   
}
