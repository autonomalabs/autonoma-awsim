using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ROS2;
using AWSIM;
using autonoma_msgs.msg;
//using std_msgs.msg;
using rosgraph_msgs.msg;
//using builtin_interfaces.msg;

namespace Autonoma
{

public class AtmaClockPublisher : Publisher<rosgraph_msgs.msg.Clock>
{
    public String modifiedRosNamespace = "";
    public String modifiedTopicName = "/clock";
    public float modifiedFrequency = 100f;
    public String modifiedFrameId = "";

    protected override void Start()
    {
        this.rosNamespace = modifiedRosNamespace;
        this.topicName = modifiedTopicName;
        this.frequency = modifiedFrequency;
        this.frameId = modifiedFrameId;
        base.Start();
    }

    void Update()
    {
        
    }

    public override void fillMsg()
    {
       //TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
        //msg.Clock_.Sec = (int)t.TotalSeconds;
        //msg.Clock_.Nanosec = (uint)((t.TotalSeconds - (uint)t.TotalSeconds) / 1.0e-9);

        float t = Time.time;
        msg.Clock_.Sec = (int)t;
        msg.Clock_.Nanosec = (uint)((t - (uint)t) / 1.0e-9);

    }
}

}