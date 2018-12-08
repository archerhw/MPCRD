﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataRecord : MonoBehaviour {

    //exponentially smoothed orientation
    private int count;
    private List<float> orientationSequence;
    [SerializeField]
    private float exponentialFactorMove;
    [SerializeField]
    private float exponentialFactorStatic;
    private float exponentialFactorUse;
    private int timerOrientation;
    [SerializeField]
    private float orientationInterval;
    private float finalOrientation;
    private int carryBit;

   //velocity data record
    private int timerVelocity;
    [SerializeField]
    private float velocityInterval;
    private Vector3 presentPosition;
    private Vector3 lastPosition;
    private float presentDirection;
    private float lastDirection;
    private float deltaDistance;
    private float deltaDiv;
    private float distance;
    private float velocity;

    public struct Data
    {
        public float virtualOrientation;
        public float velocity;
        public Vector3 virtualPosition;
        public float realOrientation;
        public Vector3 realPosition;
    };

    public struct WayPointsReal
    {
        public Vector3 realPosition;
        public WayPoint.turnType turnType;
    };

    public float timeHorizon; //timaeHorizon应该为velocityInterval与orientation的公倍数
    public Transform playerTransform;
    public Data data;
    [HideInInspector]
    public float timer;
    private int timerDataRecord;

    public Transform Corner1;
    public Transform Corner2;
    public Transform Corner3;
    public Transform Corner4;
    public Transform Center;

    public Redirector redirector;
    private WayPoint wayPoint;
    [SerializeField]
    private Player player;

    private Redirector.ActionTaken action;

    private void Awake()
    {
        count = 0;
        orientationSequence = new List<float>();
        playerTransform = GameObject.Find("Player").GetComponent<Transform>();
        timer = 0;
        timerOrientation = 0;
        timerVelocity = 0;
        carryBit = 0;
        timerDataRecord = 0;
        presentPosition=lastPosition = GeneralVector3.Vector3NoHeight(playerTransform.position);
        presentDirection = lastDirection = playerTransform.rotation.eulerAngles.y;
        Corner1 = GameObject.Find("Corner1").GetComponent<Transform>();
        Corner2 = GameObject.Find("Corner2").GetComponent<Transform>();
        Corner3 = GameObject.Find("Corner3").GetComponent<Transform>();
        Corner4 = GameObject.Find("Corner4").GetComponent<Transform>();
        Center= GameObject.Find("Center").GetComponent<Transform>();
        wayPoint = gameObject.GetComponent<WayPoint>();
        action = Redirector.ActionTaken.Zero;
    }
	
	// Update is called once per frame
	void Update () {
        TimerIncrease();
        CalculateDeltaPar();
        MovePlayer(player);
        //Apply Redirection
        switch(action)
        {
            case Redirector.ActionTaken.Zero:break;
            case Redirector.ActionTaken.PositiveRotation:
                redirector.ApplyRedirection(Redirector.RedirectorType.Rotation,player.transform,deltaDistance, deltaDiv,gain:redirector.rotateGainEnlarge);
                break;
            case Redirector.ActionTaken.NegativeRotation:
                redirector.ApplyRedirection(Redirector.RedirectorType.Rotation, player.transform, deltaDistance, deltaDiv, gain: redirector.rotateGainDecrease);
                break;
            case Redirector.ActionTaken.ClockwiseCurvature:
                redirector.ApplyRedirection(Redirector.RedirectorType.Curvature, player.transform, deltaDistance, deltaDiv, clockwise: true, radius: redirector.curvatureRadius);
                break;
            case Redirector.ActionTaken.CounterClockwiseCurvature:
                redirector.ApplyRedirection(Redirector.RedirectorType.Curvature, player.transform, deltaDistance, deltaDiv, clockwise: false, radius: redirector.curvatureRadius);
                break;
            default:break;

        }

        //velocity data record
        if((timer-timerVelocity*velocityInterval)>=velocityInterval)
        {
            velocity=distance/velocityInterval;
            timerVelocity++;
            distance = 0;
        }

        //exponentially smoothed orientation
        if((timerOrientation-timerOrientation*orientationInterval)>=orientationInterval)
        {
            //ExponentiallySmoothedSample(true);
        }
        else
        {
            //ExponentiallySmoothedSample(false);
        }

        //data record
        if ((timer-timeHorizon*timerDataRecord)>=timeHorizon)
        {
            data.velocity = velocity;
            data.virtualOrientation = player.transform.eulerAngles.y;//没有使用指数平滑
            data.virtualPosition = GeneralVector3.Vector3NoHeight(playerTransform.position);
            Vector3 originPoint = GeneralVector3.Vector3NoHeight(Corner4.position);
            //Debug.Log(originPoint);
            Vector3 xAxis = GeneralVector3.Vector3NoHeight(Corner3.position);
            //Debug.Log(xAxis);
            Vector3 zAxis = GeneralVector3.Vector3NoHeight(Corner1.position);
            //Debug.Log(zAxis);
            data.realPosition = GeneralVector3.GetRealPoint(originPoint, xAxis, zAxis, data.virtualPosition);
            //Debug.Log(data.realPosition);
            data.realOrientation = player.transform.eulerAngles.y - Center.rotation.eulerAngles.y;
            timerDataRecord++;
            WayPointsReal[] wayPointsReals = CalWayPoint2DCoor(wayPoint.wayPoints, playerTransform, wayPoint.wayPoints.Length, data.realOrientation, data.velocity * redirector.timeDepth * timeHorizon*2, originPoint, xAxis, zAxis);
            //Debug.Log(data.realPosition);
            //Debug.Log(wayPointsReals[0].realPosition);
            action = redirector.MPCRedirect(data, wayPointsReals, redirector.timeDepth).action;
            Debug.Log(action);
        }

	}

    private void ResetData()
    {
        data.virtualOrientation =0;
        data.velocity = 0;
        data.virtualPosition = new Vector3(0, 0, 0);
        data.realOrientation = 0;
        data.realPosition = new Vector3(0, 0, 0);
    }

    private void CalculateDeltaPar()//Calculate deltaDiv and deltaPos
    {
        //distance
        presentPosition = GeneralVector3.Vector3NoHeight(playerTransform.position);
        deltaDistance = Vector3.Distance(presentPosition, lastPosition);
        distance += deltaDistance;
        lastPosition = presentPosition;
        //rotation
        presentDirection = playerTransform.rotation.eulerAngles.y;
        if (presentDirection - lastDirection > 300) deltaDiv = -(360 - (presentDirection - lastDirection));
        else if (presentDirection - lastDirection < -300) deltaDiv = 360 + presentDirection - lastDirection;
        else deltaDiv = presentDirection - lastDirection;
        lastDirection = presentDirection;     
    }

    #region ExponentiallySmoothedOrientation
    //St=a*yt+(1-a)*St-1
    private void ExponentiallySmoothedSample(bool closing)
    {
        float tempY = 0;
        if (!closing)
        {
            switch (count)
            {
                case 0:
                    if (velocity <= 0.2) exponentialFactorUse = exponentialFactorStatic;
                    else exponentialFactorUse = exponentialFactorMove;
                    orientationSequence.Insert(count, playerTransform.GetComponent<Transform>().rotation.eulerAngles.y);
                    ++count;
                    break;
                case 1:
                    tempY = playerTransform.GetComponent<Transform>().rotation.eulerAngles.y - orientationSequence[0];
                    if (tempY > 300) --carryBit;
                    else if (tempY < -300) ++carryBit;
                    tempY = playerTransform.GetComponent<Transform>().rotation.eulerAngles.y + 360 * carryBit;
                    orientationSequence.Insert(2 * count, tempY);
                    ++count;
                    break;
                case 2:
                    tempY = playerTransform.GetComponent<Transform>().rotation.eulerAngles.y - orientationSequence[2];
                    if (tempY > 300) --carryBit;
                    else if (tempY < -300) ++carryBit;
                    tempY = playerTransform.GetComponent<Transform>().rotation.eulerAngles.y + 360 * carryBit;
                    orientationSequence.Insert(2 * count, tempY);
                    orientationSequence.Insert(1, (orientationSequence[0] + orientationSequence[2] + orientationSequence[4]) / 3);
                    orientationSequence.Insert(3, exponentialFactorUse * orientationSequence[2] + (1 - exponentialFactorUse) * orientationSequence[1]);
                    orientationSequence.Insert(5, exponentialFactorUse * orientationSequence[4] + (1 - exponentialFactorUse) * orientationSequence[3]);
                    ++count;
                    break;
                default:
                    tempY = playerTransform.GetComponent<Transform>().rotation.eulerAngles.y - orientationSequence[2*count-2];
                    if (tempY > 300) --carryBit;
                    else if (tempY < -300) ++carryBit;
                    tempY = playerTransform.GetComponent<Transform>().rotation.eulerAngles.y + 360 * carryBit;
                    orientationSequence.Insert(2 * count, tempY);
                    orientationSequence.Insert(2 * count + 1, exponentialFactorUse * orientationSequence[2 * count ] + (1 - exponentialFactorUse) * orientationSequence[2 * count - 1]);
                    ++count;
                    break;
            }
        }
        else
        {
            finalOrientation = (exponentialFactorUse * orientationSequence[2 * (count-1)] + (1 - exponentialFactorUse) * orientationSequence[2 *( count-1) + 1])%360;
            count = 0;
            timerOrientation++;
            carryBit = 0;
        }
    }
    #endregion ExponentiallySmoothedOrientation

    private void TimerIncrease()
    {
        timer += Time.deltaTime;
    }

    //TODO
    //private void MovePlayer()
    //{
    //    int num = NearestMovenableWayPoint();
    //    if (wayPoint.wayPoints[num].turnType == WayPoint.turnType.straight) playerTransform.position = MoveStraight(playerTransform, wayPoint.wayPoints[num].transform);
    //    else if(wayPoint.wayPoints[num].turnType == WayPoint.turnType.ninetyLeft)
    //}

    //Update player position
    private void MovePlayer(Player player)
    {
        int moveIndex = NearestMoveableWayPoint();
        Ray direction = new Ray(GeneralVector3.Vector3NoHeight(player.transform.position), wayPoint.wayPoints[moveIndex].transform.position- GeneralVector3.Vector3NoHeight(player.transform.position));
        //Debug.Log(direction);
        player.transform.position = direction.GetPoint(player.velocity * Time.deltaTime)+new Vector3(0,0.5f,0);
    }
    
    private int NearestMoveableWayPoint()
    {
        float minDistance = 50000;
        int num = 0;
        Vector3 playerTransformOH = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
        Vector3 wayPointsOH = new Vector3();
        for (int i = 0; i < wayPoint.wayPoints.Length; ++i)
        {
            wayPointsOH = new Vector3(wayPoint.wayPoints[i].transform.position.x, 0, wayPoint.wayPoints[i].transform.position.z);
            if (Vector3.Distance(playerTransformOH, wayPointsOH) < minDistance)
            {
                minDistance = Vector3.Distance(playerTransformOH, wayPointsOH);
                num = i;
            }
        }
        //Debug.Log(num + 1);
        return num + 1;
    }

    private WayPointsReal[] CalWayPoint2DCoor(WayPoint.WayPointSequence[] wayPoints,Transform playerPos,int length, float orientation, float routineDistance, Vector3 origin, Vector3 xAxis, Vector3 zAxis)
    {
        int nearestnum = NearestMoveableWayPoint();
        int largerNum, smallerNum;
        if (nearestnum == length-1) largerNum = 0;
        else largerNum = nearestnum + 1;
        if (nearestnum == 0) smallerNum = length-1;
        else smallerNum = nearestnum - 1;
        int assumedNum = Mathf.CeilToInt(routineDistance)+1;
        WayPointsReal[] resultWayPoints = new WayPointsReal[assumedNum];
        float anglelarger = Vector3.Angle(GeneralVector3.Vector3NoHeight(playerPos.forward), GeneralVector3.Vector3NoHeight(wayPoints[largerNum].transform.position) - GeneralVector3.Vector3NoHeight(playerPos.position));
        float anglesmaller= Vector3.Angle(GeneralVector3.Vector3NoHeight(playerPos.forward), GeneralVector3.Vector3NoHeight(wayPoints[smallerNum].transform.position) - GeneralVector3.Vector3NoHeight(playerPos.position));
        if(anglelarger>=anglesmaller)
        {
            int index=smallerNum;
            for(int i=0; i<assumedNum;++i)
            {
                resultWayPoints[i].realPosition = GeneralVector3.GetRealPoint(origin, xAxis, zAxis, GeneralVector3.Vector3NoHeight(wayPoints[index].transform.position));
                resultWayPoints[i].turnType = wayPoints[index].turnType;
                if (index == 0) index = length-1;
                else --index;
            }
        }
        else
        {
            int index = largerNum;
            for(int i=0;i<assumedNum;++i)
            {
                resultWayPoints[i].realPosition = GeneralVector3.GetRealPoint(origin, xAxis, zAxis, GeneralVector3.Vector3NoHeight(wayPoints[index].transform.position));
                resultWayPoints[i].turnType = wayPoints[index].turnType;
                if (index == length-1) index = 0;
                else ++index;
            }
        }
        return resultWayPoints;
    }

}
