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
        if (collidingObject || !col.GetComponent<Rigidbody>())
        {
            return;
        }
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

            BeginLine();
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
            RecognizeGesture();
            strokeId = -1;
            foreach (LineRenderer lineRenderer in gestureLinesRenderer)
            {
                
                lineRenderer.SetVertexCount(0);
                Destroy(lineRenderer.gameObject);
            }


        }
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
        joint.connectedBody = objectInHand.GetComponent<Rigidbody>();
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
            objectInHand.GetComponent<Rigidbody>().velocity = Controller.velocity;
            objectInHand.GetComponent<Rigidbody>().angularVelocity = Controller.angularVelocity;
        }
        objectInHand = null;
    }

    private void RecognizeGesture()
    {
        foreach (Point pnt in points)
        {
            Debug.Log("Points x:" + pnt.X + "Points y:" + pnt.Y);
        }
        Gesture candidate = new Gesture(points.ToArray());
        Result gestureResult = PointCloudRecognizer.Classify(candidate, trainingSet.ToArray());

        Debug.Log(gestureResult.GestureClass + " " + gestureResult.Score);
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
        currentGestureLineRenderer.SetPosition(vertexCount - 1, headCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 1)));
    }
}
