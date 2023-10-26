using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcController : MonoBehaviour
{
    // have list of possible trajectory CSVs here

    public NpcPathData path;
    public float speed = 5.0f;
    public LayerMask collisionLayerMask;
    float ellapsedInterp = 0.0f;
    float nextInterpTime;

    public bool drawDebugGizmos = false;
    CarController carController;
    bool foundCarController = false;


    // Start is called before the first frame update
    void Start()
    {
        
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
    }

    void FixedUpdate()
    {
       
    }

    void TickMotion()
    {
        //transform.position = GetNextPosition();
        //transform.rotation = GetNextRotation();

        carController.throttleCmd = 0.1f;
        carController.brakeCmd = 0.0f;
        //carController.steerAngleCmd = 0.3f;

        SetSteerAngleCmdForError(GetHeadingError());

        UpdatePathIndex();
    }

    void UpdatePathIndex()
    {
        path.UpdateIndex(transform.position);
    }

    void CheckOverlaps()
    {
        // Collider[] hitColliders = Physics.OverlapBox(gameObject.transform.position, 
        //     transform.localScale / 2, Quaternion.identity, collisionLayerMask);
        // int i = 0;
        // //Check when there is a new collider coming into contact with the box
        // while (i < hitColliders.Length)
        // {
        //     //Output all of the collider names
        //     Debug.Log("Hit : " + hitColliders[i].name + i);
        //     //Increase the number of Colliders in the array
        //     i++;
        // }
    }

    void CheckDespawn()
    {
        // despawn if beyond thresh behind player
    }

    Vector3 GetNextPosition()
    {
        return path.GetNextPoint();
    }

    Quaternion GetNextRotation()
    {
        Vector3 prevDir = path.GetThisPoint() - path.GetPrevPoint();
        Vector3 dir = path.GetNextPoint() - path.GetThisPoint();

        Quaternion prevRot = Quaternion.LookRotation(prevDir, Vector3.up);
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        Quaternion nextRot = Quaternion.Lerp(transform.rotation, targetRot, ellapsedInterp / (0.2f / speed));

        return nextRot;
    }

    void SetSteerAngleCmdForError(float angleError)
    {
        float cmd = -angleError;
        carController.steerAngleCmd = cmd / carController.vehicleParams.steeringRatio;
        float maxAngleAtwheel = Mathf.Abs(carController.vehicleParams.maxSteeringAngle / carController.vehicleParams.steeringRatio);
        carController.steerAngleCmd = Mathf.Clamp(carController.steerAngleCmd, -maxAngleAtwheel, maxAngleAtwheel);
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

    void OnDrawGizmos()
    {
        if(drawDebugGizmos && path != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(path.GetNextPoint(), 1.2f);
            Gizmos.DrawRay(transform.position, path.GetNextPoint());
        }
    }
}
