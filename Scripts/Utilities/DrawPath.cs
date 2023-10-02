using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ROS2;
using autonoma_msgs.msg;
using ai_msgs.msg;
using AWSIM;
using System.Threading;

public class DrawPath : MonoBehaviour
{
    public string pathTopic = "/planning/raceline";
    public string stateTopic = "/estimation/state_estimate";
    public QoSSettings qosSettings = new QoSSettings();
    ISubscription<Trajectory> pathSubscriber;
    ISubscription<StateEstimate> stateSubscriber;

    bool draw_path = false;
    bool draw_state_position = false;
    Vector3 state_position;
    double state_heading;

    public List<Vector3> waypoints;

    // Start is called before the first frame update
    void Start()
    {
        var qos = qosSettings.GetQoSProfile();
        pathSubscriber = SimulatorROS2Node.CreateSubscription<Trajectory>(pathTopic, OnPathMsg, qos);
        stateSubscriber = SimulatorROS2Node.CreateSubscription<StateEstimate>(stateTopic, OnStateMsg, qos);

        waypoints = new List<Vector3>();
    }

    void OnPathMsg(Trajectory msg)
    {
        double[] x = msg.X_m;
        double[] y = msg.Y_m;

        waypoints.Clear();
        for(int i = 0; i < x.Length; i++)
        {
            // looks like x fwd, y left
            waypoints.Add(new Vector3((float)y[i], 0, (float)x[i]));
        }
    }

    void OnStateMsg(StateEstimate msg)
    {
        double x_cg = msg.X_cg_m;
        double y_cg = msg.Y_cg_m;
        double z_cg = msg.Z_cg_m;
        state_heading = msg.Psi_cg_rad;
        state_position = new Vector3((float)y_cg, (float)-z_cg, (float)x_cg);
    }

    void OnDrawGizmos()
    {
        if(draw_state_position)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(state_position, 0.2f);

            Quaternion q = Quaternion.AngleAxis((float)state_heading * Mathf.Rad2Deg, new Vector3(0, 1, 0));
            Vector3 forward = new Vector3(0, 0, 100);
            Vector3 direction = q * forward;
            //Gizmos.DrawRay(state_position, direction);

            //Gizmos.DrawRay()
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log($"waypoints size {waypoints.Count}", this);
        //Debug.Log($"draw tsf {transform.root.position}", this);
        Color color = new Color(0, 0, 1.0f);

        if(draw_state_position)
        {
            Debug.Log($"draw state {state_position}", this);
            Debug.DrawLine(state_position, state_position + new Vector3(0, 5000, 0), color, 0.0f, true);
        }

        if(draw_path)
        {
            string path_string = "";
            List<Vector3> wpts = waypoints;
            for(int i = 1; i < wpts.Count - 1; i++)
            {
                Transform t = transform;
                transform.localScale = new Vector3(1, 1, 1);
                Vector3 wpi = wpts[i];
                wpi = t.TransformPoint(wpi);
                Vector3 wpn = wpts[i + 1];
                wpn = t.TransformPoint(wpn);

                path_string += $"i: {i}, {wpts[i]}";

                //Debug.DrawLine(wpi, wpi + new Vector3(0, 5000, 0), color, 0.0f, true);
                //Debug.DrawLine(wpi, wpn, Color.green, 0.0f, false);
            }
            Debug.Log($"pos {path_string}", this);
        }
    }
}
