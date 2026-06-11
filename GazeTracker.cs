using System.IO;
using System.Text;
using UnityEngine;

public class GazeTracker : MonoBehaviour
{
    public Transform targetSphere;
    public Material defaultMaterial;
    public Material hitMaterial;
    public float dotThreshold = 0.95f;
    public float confidenceThreshold = 0.5f;

    private string csvPath;
    private StreamWriter csvWriter;
    private Transform eyeAnchor;

    void Start()
    {
        var rig = FindObjectOfType<OVRCameraRig>();
        if (rig != null) eyeAnchor = rig.centerEyeAnchor;
        else Debug.LogError("No OVRCameraRig");

        if (targetSphere == null) Debug.LogError("No target sphere");

        string dir = Application.persistentDataPath;
        Directory.CreateDirectory(dir);
        string fileName = $"GazeData_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
        csvPath = Path.Combine(dir, fileName);
        Debug.Log("CSV: " + csvPath);

        csvWriter = new StreamWriter(csvPath, false, Encoding.UTF8);
        csvWriter.WriteLine("Timestamp,SphereX,SphereY,SphereZ,GazeOriginX,GazeOriginY,GazeOriginZ,GazeDirX,GazeDirY,GazeDirZ,Hit,LeftConf,RightConf");
        csvWriter.Flush();
    }

    void Update()
    {
        if (eyeAnchor == null || targetSphere == null) return;

        Vector3 gazeOrigin = eyeAnchor.position;
        Vector3 gazeDirection = eyeAnchor.forward;
        float leftConf = 0f, rightConf = 0f;

        OVRPlugin.EyeGazesState state = new OVRPlugin.EyeGazesState();
        if (OVRPlugin.GetEyeGazesState(OVRPlugin.Step.Render, -1, ref state))
        {
            if (state.EyeGazes.Length >= 2)
            {
                leftConf = state.EyeGazes[0].Confidence;
                rightConf = state.EyeGazes[1].Confidence;

                if (leftConf > 0.3f && rightConf > 0.3f)
                {
                    Vector3 leftPos = new Vector3(state.EyeGazes[0].Pose.Position.x, state.EyeGazes[0].Pose.Position.y, state.EyeGazes[0].Pose.Position.z);
                    Vector3 rightPos = new Vector3(state.EyeGazes[1].Pose.Position.x, state.EyeGazes[1].Pose.Position.y, state.EyeGazes[1].Pose.Position.z);
                    gazeOrigin = (leftPos + rightPos) * 0.5f;

                    Quaternion leftRot = new Quaternion(state.EyeGazes[0].Pose.Orientation.x, state.EyeGazes[0].Pose.Orientation.y, state.EyeGazes[0].Pose.Orientation.z, state.EyeGazes[0].Pose.Orientation.w);
                    Quaternion rightRot = new Quaternion(state.EyeGazes[1].Pose.Orientation.x, state.EyeGazes[1].Pose.Orientation.y, state.EyeGazes[1].Pose.Orientation.z, state.EyeGazes[1].Pose.Orientation.w);
                    Vector3 leftDir = leftRot * Vector3.forward;
                    Vector3 rightDir = rightRot * Vector3.forward;
                    gazeDirection = (leftDir + rightDir).normalized;
                }
                else if (leftConf > 0.3f)
                {
                    gazeOrigin = new Vector3(state.EyeGazes[0].Pose.Position.x, state.EyeGazes[0].Pose.Position.y, state.EyeGazes[0].Pose.Position.z);
                    Quaternion leftRot = new Quaternion(state.EyeGazes[0].Pose.Orientation.x, state.EyeGazes[0].Pose.Orientation.y, state.EyeGazes[0].Pose.Orientation.z, state.EyeGazes[0].Pose.Orientation.w);
                    gazeDirection = leftRot * Vector3.forward;
                }
                else if (rightConf > 0.3f)
                {
                    gazeOrigin = new Vector3(state.EyeGazes[1].Pose.Position.x, state.EyeGazes[1].Pose.Position.y, state.EyeGazes[1].Pose.Position.z);
                    Quaternion rightRot = new Quaternion(state.EyeGazes[1].Pose.Orientation.x, state.EyeGazes[1].Pose.Orientation.y, state.EyeGazes[1].Pose.Orientation.z, state.EyeGazes[1].Pose.Orientation.w);
                    gazeDirection = rightRot * Vector3.forward;
                }
            }
        }

        Vector3 toSphere = targetSphere.position - gazeOrigin;
        float dot = Vector3.Dot(gazeDirection.normalized, toSphere.normalized);
        float maxConf = Mathf.Max(leftConf, rightConf);
        bool isHit = (dot > dotThreshold) && (maxConf >= confidenceThreshold);

        Renderer rend = targetSphere.GetComponent<Renderer>();
        if (rend != null) rend.material = isHit ? hitMaterial : defaultMaterial;

        if (csvWriter != null)
        {
            string line = $"{Time.time:F6},{targetSphere.position.x:F6},{targetSphere.position.y:F6},{targetSphere.position.z:F6}," +
                          $"{gazeOrigin.x:F6},{gazeOrigin.y:F6},{gazeOrigin.z:F6}," +
                          $"{gazeDirection.x:F6},{gazeDirection.y:F6},{gazeDirection.z:F6}," +
                          $"{(isHit ? 1 : 0)},{leftConf:F6},{rightConf:F6}";
            csvWriter.WriteLine(line);
            csvWriter.Flush();
        }
    }

    void OnApplicationQuit()
    {
        csvWriter?.Close();
        Debug.Log("CSV saved: " + csvPath);
    }
}