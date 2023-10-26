using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;


public class NpcPathData
{
    public List<Vector3> positions = new List<Vector3>();
    int currentIndex = 0;

    public NpcPathData(String CsvFilePath)
    {
        using (var Reader = new StreamReader(CsvFilePath))
        {
            while(!Reader.EndOfStream)
            {
                var Line = Reader.ReadLine();
                var Values = Line.Split(',');
                Vector3 pos = new Vector3();

                // May need coordinate transform here


                pos[0] = float.Parse(Values[1]);
                pos[1] = -float.Parse(Values[2]);
                pos[2] = float.Parse(Values[0]);

                //pos[0] = float.Parse(Values[0]);
                //pos[1] = float.Parse(Values[1]);
                //pos[2] = float.Parse(Values[2]);
                positions.Add(pos);

                //Debug.Log($"Loaded position {pos}", null);
            }
            Debug.Log($"Loaded {positions.Count} path points for npc path", null);
        }
    }

    public void SnapPathHeight()
    {
        for(int i = 0; i < positions.Count; i++)
        {
            Vector3 start = new Vector3(positions[i][0], 1000, positions[i][2]);
            Vector3 end = new Vector3(positions[i][0], -1000, positions[i][2]);
            RaycastHit hit;
            Vector3 backoff = new Vector3(0, 0.1f, 0);
            if(Physics.Linecast(start, end, out hit))
            {
                positions[i] = hit.point + backoff;
            }
        }
    }

    public bool UpdateIndex(Vector3 pos)
    {
        int minDistI = 0;
        float minDist = Vector3.Distance(pos, positions[0]);
        for(int i = 1; i < positions.Count; i++)
        {
            float dist = Vector3.Distance(pos, positions[i]);
            if(dist < minDist)
            {
                minDist = dist;
                minDistI = i;
            }
        }
        bool changed = currentIndex != minDistI;
        currentIndex = minDistI;
        return changed;
    }

    public Vector3 GetSegmentDirection()
    {
        return (GetNextPoint() - GetThisPoint()).normalized;
    }

    public float GetSegmentDistance()
    {
        return Vector3.Distance(GetNextPoint(), GetThisPoint());
    }

    public Vector3 GetThisPoint()
    {
        return positions[currentIndex];
    }

    public Vector3 GetPoint(int offset)
    {
        return positions[(currentIndex + offset + positions.Count) % positions.Count];
    }

    public Vector3 GetNextPoint()
    {
        int nextIndex = (currentIndex + 1) % positions.Count;
        return positions[nextIndex];
    }

    public Vector3 GetPrevPoint()
    {
        int prevIndex = (currentIndex - 1 + positions.Count) % positions.Count;
        return positions[prevIndex];
    }
}

public class NpcSpawnManager : MonoBehaviour
{
    public bool ShouldSpawnNPCs = false;
    public GameObject NpcPrefab;
    public List<String> npcPathFiles = new List<String>();
    public List<NpcPathData> npcPaths = new List<NpcPathData>();
    public float nextNpcMaxSpeed = 20.0f;
    public float npcSpeedIncrement = 20.0f;
    bool pathsAreReady = false;
    NpcController currentNpc;

    public float collisionTimePenalty = 5.0f;
    public float collisionDebounceTime = 0.1f;

    public float teleportNpcDistanceThreshold = -50.0f;
    public float postTeleportNpcLeadDistance = 200.0f;

    public String PlayerVehicleObjectName;
    GameObject PlayerVehicle;
    TrackPositionFinder trackPosition;
    LapTimer lapTimer;
    bool hasFoundRequiredObjects = false;
    public bool drawDebugGizmos = true;

    void Awake()
    {
        foreach(var s in npcPathFiles)
        {
            NpcPathData pd = new NpcPathData(Path.Combine(Application.streamingAssetsPath, s));
            pd.SnapPathHeight();
            npcPaths.Add(pd);
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(DelayedSnapPaths());
    }

    // Start is called before the first frame update
    void Start()
    {
        

        //StartCoroutine(DelayedSnapPaths());
    }

    private IEnumerator DelayedSnapPaths()
    {
        yield return new WaitForSeconds(1.0f);
        foreach(var p in npcPaths)
        {
            p.SnapPathHeight();
        }
        pathsAreReady = true;
    }

    // Update is called once per frame
    void Update()
    {
        if(!hasFoundRequiredObjects)
        {
            FindRequiredObjects();
        }
        else
        {
            if(!currentNpc && pathsAreReady)
            {
                SpawnNpc();
            }
            else if(currentNpc)
            {
                float distance = trackPosition.GetCarRelativeTrackDistance(currentNpc.transform.position);
                //Debug.Log($"relative npc distance: {distance}");

                if(distance < teleportNpcDistanceThreshold)
                {
                    nextNpcMaxSpeed += npcSpeedIncrement;
                    currentNpc.transform.position = GetSpawnPosition();
                    currentNpc.maxSpeed = nextNpcMaxSpeed;
                }
            }
        }
    }

    void SpawnNpc()
    {
        var prefab = Instantiate(NpcPrefab, GetSpawnPosition(), GetSpawnRotation());
        var prefabChildren = prefab.GetComponentsInChildren<NpcController>();
        currentNpc = prefabChildren[0];
        currentNpc.path = npcPaths[0];
        currentNpc.trackPosition = trackPosition;
        currentNpc.maxSpeed = nextNpcMaxSpeed;
        currentNpc.lapTimer = lapTimer;
        currentNpc.collisionTimePenalty = collisionTimePenalty;
        currentNpc.collisionDebounceTime = collisionDebounceTime;
    }

    Vector3 GetSpawnPosition()
    {
        return trackPosition.GetAheadPoint(postTeleportNpcLeadDistance);
    }

    Quaternion GetSpawnRotation()
    {
        return Quaternion.identity;
    }

    void FindRequiredObjects()
    {
        PlayerVehicle = GameObject.Find(PlayerVehicleObjectName);
        var TpfFindResults = FindObjectsOfType<TrackPositionFinder>();
        var lapTimers = FindObjectsOfType<LapTimer>();
        if(TpfFindResults.Length > 0)
        {
            trackPosition = TpfFindResults[0];
        }
        if(PlayerVehicle)
        {
            //Debug.Log($"Npc Spawn Manager found player vehicle", this);
        }
        if(lapTimers.Length > 0)
        {
            lapTimer = lapTimers[0];
        }
        if(trackPosition)
        {
            //Debug.Log($"Npc Spawn Manager found track position", this);
        }
        hasFoundRequiredObjects = PlayerVehicle != null 
            && trackPosition != null && lapTimer != null;
    }

    void OnDrawGizmos()
    {
        if(drawDebugGizmos && npcPaths.Count > 0)
        {
            Gizmos.color = Color.yellow;

            foreach(var path in npcPaths)
            {
                foreach(Vector3 point in path.positions)
                {
                    Gizmos.DrawSphere(point, 0.4f);
                }
            }
        }
    }
}
