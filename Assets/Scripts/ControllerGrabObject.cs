using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using PDollarGestureRecognizer;

public class ControllerGrabObject : MonoBehaviour {

    private SteamVR_TrackedObject trackedObj;
    private GameObject collidingObject;
    private GameObject objectInHand;

    public Camera headCamera;
    public Transform gestureOnScreenPrefab;

    private List<LineRenderer> gestureLinesRenderer = new List<LineRenderer>();
    private LineRenderer currentGestureLineRenderer;
    private Vector3 lineStart, lineEnd;
    private int vertexCount = 0;

    private Vector2 screenPos = Vector2.zero;
    private List<Point> points = new List<Point>();
    private List<Gesture> trainingSet = new List<Gesture>();
    private int strokeId = -1;


    private SteamVR_Controller.Device Controller
    {
        get { return SteamVR_Controller.Input((int)trackedObj.index); }
    }

    void Awake()
    {
        trackedObj = GetComponent<SteamVR_TrackedObject>();

        //Load pre-made gestures
        TextAsset[] gesturesXml = Resources.LoadAll<TextAsset>("GestureSet/10-stylus-MEDIUM/");
        foreach (TextAsset gestureXml in gesturesXml)
            trainingSet.Add(GestureIO.ReadGestureFromXML(gestureXml.text));
    }

    private void SetCollidingObject(Collider col)
    {
        /*if (collidingObject || !col.GetComponent<Rigidbody>())
        {
            return;
        }*/
        collidingObject = col.gameObject;
    }

    // Update is called once per frame
    void Update () {
        if (Controller.GetHairTriggerDown())
        {
            if (collidingObject)
            {
                GrabObject();
            }
        }

        if (Controller.GetHairTriggerUp())
        {
            if (objectInHand)
            {
                ReleaseObject();
            }
        }

        if (Controller.GetPressDown(SteamVR_Controller.ButtonMask.Grip))
        {
            ++strokeId;

            lineStart = transform.position;
            BeginLine();
        }

        if (Controller.GetPressUp(SteamVR_Controller.ButtonMask.Grip))
        {
            lineEnd = transform.position;
        }

        if (Controller.GetPress(SteamVR_Controller.ButtonMask.Grip))
        {
            //Debug.Log(gameObject.name + " Grip Press");
            screenPos = headCamera.WorldToScreenPoint(Controller.transform.pos);
            Debug.Log("Screen Pos:" + screenPos);

            points.Add(new Point(screenPos.x, -screenPos.y, strokeId));

            DrawLine();
        }

        if (Controller.GetPressDown(SteamVR_Controller.ButtonMask.ApplicationMenu))
        {
            Result gesture = RecognizeGesture();

            if (gesture.GestureClass == "line")
            {
                currentGestureLineRenderer.material.color = Color.blue;
                AddCollider();
            }
            else
            {
                strokeId = -1;
                points.Clear();

                foreach (LineRenderer lineRenderer in gestureLinesRenderer)
                {
                    lineRenderer.SetVertexCount(0);
                    Destroy(lineRenderer.gameObject);
                }
            }

        }
        /*
        if (GameObject.Find("GestureOnScreen(Clone)")  && !Controller.GetPress(SteamVR_Controller.ButtonMask.Grip))
        {
            GameObject.Find("GestureOnScreen(Clone)").transform.position = GameObject.Find("GestureOnScreen(Clone)").GetComponentInChildren<Rigidbody>().transform.position - GameObject.Find("GestureOnScreen(Clone)").GetComponentInChildren<Rigidbody>().transform.localPosition;
        }
        */
    }

    public void OnTriggerEnter(Collider other)
    {
        SetCollidingObject(other);
    }

    public void OnTriggerStay(Collider other)
    {
        SetCollidingObject(other);
    }

    public void OnTriggerExit(Collider other)
    {
        if (!collidingObject)
        {
            return;
        }

        collidingObject = null;
    }

    private void GrabObject()
    {
        objectInHand = collidingObject;
        collidingObject = null;
        var joint = AddFixedJoint();
        joint.connectedBody = objectInHand.GetComponentInParent<Rigidbody>();
    }

    private FixedJoint AddFixedJoint()
    {
        FixedJoint fx = gameObject.AddComponent<FixedJoint>();
        fx.breakForce = 20000;
        fx.breakTorque = 20000;
        return fx;
    }

    private void ReleaseObject()
    {
        if (GetComponent<FixedJoint>())
        {
            GetComponent<FixedJoint>().connectedBody = null;
            Destroy(GetComponent<FixedJoint>());
            objectInHand.GetComponentInParent<Rigidbody>().velocity = Controller.velocity;
            objectInHand.GetComponentInParent<Rigidbody>().angularVelocity = Controller.angularVelocity;
        }
        objectInHand = null;
    }

    private Result RecognizeGesture()
    {
        foreach (Point pnt in points)
        {
            Debug.Log("Points x:" + pnt.X + "Points y:" + pnt.Y);
        }
        Gesture candidate = new Gesture(points.ToArray());
        Result gestureResult = PointCloudRecognizer.Classify(candidate, trainingSet.ToArray());
        return gestureResult;

        //Debug.Log(gestureResult.GestureClass + " " + gestureResult.Score);
    }

    private void AddCollider()
    {
        GameObject line;
        BoxCollider col = new GameObject("Collider").AddComponent<BoxCollider>();
        line = GameObject.Find("GestureOnScreen(Clone)");
        if (line != null)
        {
            //Rigid body for the line
            Rigidbody lineRigidBody = line.AddComponent<Rigidbody>();
            lineRigidBody.mass = 1;
            lineRigidBody.useGravity = false;
            lineRigidBody.interpolation = RigidbodyInterpolation.Interpolate;
            lineRigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            col.transform.parent = line.transform;
            //line.AddComponent<BoxCollider>();
            //BoxCollider col = line.GetComponent<BoxCollider>();
            //Width of the line renderer
            float lineWidth = currentGestureLineRenderer.endWidth;
            //length of line
            float lineLength = Vector3.Distance(lineStart, lineEnd);
            //Height of line
            float lineHeight = Mathf.Abs(lineStart.y - lineEnd.y);
            /*if (lineHeight < 1f)
            {
                lineHeight = 1f;
            }*/
            //Set size of collider
            col.size = new Vector3(lineLength, lineHeight, lineWidth);
            //mid point
            Vector3 midPoint = (lineStart + lineEnd) / 2;
            //Move collider to mid point
            col.transform.position = midPoint;
            //Get angle
            float angle = Mathf.Atan2((lineEnd.z - lineStart.z), (lineEnd.x - lineStart.x));
            //Convert to degrees
            angle *= Mathf.Rad2Deg;
            //Get inverse
            angle *= -1;
            //Rotato potato
            col.transform.Rotate(0, angle, 0);

            
        }
        else Debug.Log("Didn't find object");
        
    }

    private void BeginLine()
    {
        Transform tmpGesture = Instantiate(gestureOnScreenPrefab, transform.position, transform.rotation) as Transform;
        currentGestureLineRenderer = tmpGesture.GetComponent<LineRenderer>();
        
        gestureLinesRenderer.Add(currentGestureLineRenderer);

        vertexCount = 0;
    }

    private void DrawLine()
    {
        currentGestureLineRenderer.SetVertexCount(++vertexCount);
        currentGestureLineRenderer.SetPosition(vertexCount - 1, transform.position);
        currentGestureLineRenderer.SetPosition(vertexCount - 1, GameObject.Find("GestureOnScreen(Clone)").transform.InverseTransformPoint(transform.position));
    }
}
