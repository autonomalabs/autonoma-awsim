using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcController : MonoBehaviour
{
    // have list of possible trajectory CSVs here

    public NpcPathData path;
    public float maxSpeed = 50.0f;
    private float speed = 0.0f;
    private float accel = 10.0f;
    public LayerMask collisionLayerMask;
    float ellapsedInterp = 0.0f;
    float nextInterpTime;
    float turnLeft = 0.0f;
    public float collisionTimePenalty = 0.0f;

    public bool drawDebugGizmos = false;
    CarController carController;
    bool foundCarController = false;

    public GameObject frontLeftWheel;
    public GameObject frontRightWheel;
    public GameObject rearLeftWheel;
    public GameObject rearRightWheel;
    public float wheelRadius;

    public float collisionDebounceTime = 0.1f;
    bool collisionDebounce = false;

    public TrackPositionFinder trackPosition;
    public LapTimer lapTimer;


    // Start is called before the first frame update
    void Start()
    {
        //transform.position = path.GetThisPoint();
    }

    // Update is called once per frame
    void Update()
    {
        if(!foundCarController)
        {
            InitCarController();
        }
        else
        {
            TickMotion();
            CheckDespawn();
        }
        SnapWheelsToGround();
        CheckOverlaps();
    }

    void FixedUpdate()
    {
       
    }

    void TickMotion()
    {
        //Debug.Log($"desired speed {GetDesiredSpeed()}");
        float speedError = GetDesiredSpeed() - speed;
        if(Mathf.Abs(speedError) <= accel * Time.deltaTime)
        {
            speed = GetDesiredSpeed();
        }
        speed += Mathf.Sign(speedError) * accel * Time.deltaTime;
        speed = Mathf.Clamp(speed, 0.0f, maxSpeed);

        Vector3 newPos = transform.position + GetVelocity() * Time.deltaTime;
        //newPos[1] = transform.position[1];
        transform.position = newPos;
        TickRotation();
        path.UpdateIndex(transform.position);

        //transform.GetComponent<Rigidbody>().velocity = new Vector3(0f,0f,0f);
        //transform.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }

    void CheckOverlaps()
    {
        Collider[] hitColliders = Physics.OverlapBox(gameObject.transform.position + new Vector3(0, 1, 0), 
            new Vector3(2, 1, 3), transform.rotation);
        int i = 0;
        //Check when there is a new collider coming into contact with the box
        while (i < hitColliders.Length)
        {
            //Output all of the collider names
            //Debug.Log("Hit : " + hitColliders[i].name + i);
            //Increase the number of Colliders in the array

            if(hitColliders[i].transform.root.gameObject.name == "DallaraAV21v2(Clone)"
                && !collisionDebounce)
            {
                collisionDebounce = true;
                Debug.Log("PLAYER COLLISION", this);
                lapTimer.currLaptime += collisionTimePenalty;
                StartCoroutine(ResetCollisionDebounce());
            }
            i++;
        }
    }

    private IEnumerator ResetCollisionDebounce()
    {
        collisionDebounce = true;
        yield return new WaitForSeconds(collisionDebounceTime);
        collisionDebounce = false;
    }

    Vector3 GetVelocity()
    {
        return (path.GetNextPoint() - transform.position).normalized * speed;
    }

    float GetDesiredSpeed()
    {
        return (maxSpeed - (maxSpeed * GetTurnBrakeIndex())) - (maxSpeed * GetDistFromPlayerBrakeIndex());
    }

    void CheckDespawn()
    {
        // despawn if beyond thresh behind player
    }

    void TickRotation()
    {
        Vector3 prevDir = path.GetThisPoint() - path.GetPoint(-2);
        Vector3 dir = (path.GetNextPoint() - path.GetPrevPoint()).normalized;

        Quaternion prevRot = Quaternion.LookRotation(prevDir, Vector3.up);
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);

        float nextPointDist = Vector3.Distance(path.GetNextPoint(), transform.position);
        float segmentDist = path.GetSegmentDistance();

        turnLeft = Mathf.Abs(Vector3.SignedAngle(prevDir, dir, Vector3.up));

        float ratio = (segmentDist - (nextPointDist - segmentDist / 2.0f)) / segmentDist;

        transform.rotation = Quaternion.Lerp(prevRot, targetRot, ratio);
        //transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 150.0f * Time.deltaTime);
        //transform.rotation = targetRot;
    }

    float GetHeadingError()
    {
        return Vector3.SignedAngle(transform.position, path.GetNextPoint(), Vector3.up);
    }

    void InitCarController()
    {
        carController = transform.root.GetComponentInChildren<CarController>();
        foundCarController = carController != null;
    }

    void SnapWheelsToGround()
    {
        SnapObjectToGround(transform.root.gameObject, 0);
        SnapObjectToGround(frontRightWheel, wheelRadius);
        SnapObjectToGround(frontLeftWheel, wheelRadius);
        SnapObjectToGround(rearRightWheel, wheelRadius);
        SnapObjectToGround(rearLeftWheel, wheelRadius);
    }

    void SnapObjectToGround(GameObject o, float offset)
    {
        Transform t = o.transform;
        t.position -= new Vector3(0, offset / 2.0f, 0);
        Vector3 start = t.position;
        Vector3 end = start + new Vector3(0, -10, 0);
        RaycastHit hit;
        Vector3 backoff = new Vector3(0, 0.1f, 0);
        if(Physics.Linecast(start, end, out hit))
        {
            o.transform.position = new Vector3(t.position[0], hit.point[1] + offset, t.position[2]);
        }
    }

    float GetTurnBrakeIndex()
    {
        float totalTurn = 0.0f;

        Vector3 d0 = (path.GetPoint(1) - path.GetPoint(-1)).normalized;
        Vector3 d1 = (path.GetPoint(3) - path.GetPoint(1)).normalized;
        Vector3 d2 = (path.GetPoint(10) - path.GetPoint(7)).normalized;

        totalTurn += Mathf.Abs(Vector3.SignedAngle(d0, d1, Vector3.up));
        totalTurn += Mathf.Abs(Vector3.SignedAngle(d1, d2, Vector3.up));
        //totalTurn += Mathf.Abs(Vector3.SignedAngle(path.GetPoint(1), path.GetPoint(3), Vector3.up));
        //totalTurn += Mathf.Abs(Vector3.SignedAngle(path.GetPoint(3), path.GetPoint(4), Vector3.up));
        float brakeIndex = totalTurn / 90.0f;
        return Mathf.Clamp(brakeIndex, 0.0f, 0.9f);
    }

    float GetDistFromPlayerBrakeIndex()
    {
        float distToPlayer = trackPosition.GetCarRelativeTrackDistance(transform.position);
        float index = (distToPlayer - 100.0f) / 400.0f;
        return Mathf.Clamp(index, 0.0f, 0.9f);
    }

    void OnDrawGizmos()
    {
        if(drawDebugGizmos && path != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(path.GetNextPoint(), 1.2f);
            Gizmos.DrawRay(transform.position, path.GetNextPoint());

            //Gizmos.DrawCube(transform.position + new Vector3(0, 1, 0), new Vector3(2, 1, 3));
        }
    }
}
