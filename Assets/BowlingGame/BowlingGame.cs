using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A small bowling game built from Unity primitives and basic C#.
public class BowlingGame : MonoBehaviour
{
    readonly List<GameObject> pins = new List<GameObject>();
    GameObject ball;
    Rigidbody ballBody;
    Material laneMat, pinMat, redMat, ballMat, floorMat, trimMat;
    int frame = 1, rollInFrame, totalScore, framePins, firstRollPins;
    bool rolling, resolving;
    float aim = 0f, power = 18f;
    string status = "Aim with A/D, set power with W/S, then click ROLL.";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartGame()
    {
        if (FindFirstObjectByType<BowlingGame>() == null)
            new GameObject("Bowling Game Controller").AddComponent<BowlingGame>();
    }

    void Awake()
    {
        laneMat = MakeMat(new Color(.46f, .25f, .12f));
        pinMat = MakeMat(new Color(.94f, .92f, .83f));
        redMat = MakeMat(new Color(.75f, .12f, .10f));
        ballMat = MakeMat(new Color(.08f, .28f, .72f));
        floorMat = MakeMat(new Color(.14f, .18f, .21f));
        trimMat = MakeMat(new Color(.78f, .56f, .28f));
        BuildLane();
        SpawnBall();
        ResetPins();
        SetupCamera();
    }

    Material MakeMat(Color c)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material m = new Material(shader);
        m.color = c; return m;
    }

    GameObject Cube(string n, Vector3 p, Vector3 s, Material mat)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = n; g.transform.position = p; g.transform.localScale = s;
        g.GetComponent<Renderer>().material = mat; return g;
    }

    void BuildLane()
    {
        Cube("Bowling alley floor", new Vector3(0,-.25f,10), new Vector3(16,.5f,42), floorMat);
        Cube("Polished wooden lane", new Vector3(0,.02f,13), new Vector3(4,.15f,32), laneMat);
        Cube("Left gutter", new Vector3(-2.35f,-.08f,13), new Vector3(.65f,.22f,32), trimMat);
        Cube("Right gutter", new Vector3(2.35f,-.08f,13), new Vector3(.65f,.22f,32), trimMat);
        Cube("Left rail", new Vector3(-2.8f,.2f,13), new Vector3(.16f,.55f,32), trimMat);
        Cube("Right rail", new Vector3(2.8f,.2f,13), new Vector3(.16f,.55f,32), trimMat);
        for (int i=0;i<7;i++) Cube("Lane guide", new Vector3(-1.5f+i*.5f,.105f,3), new Vector3(.035f,.012f,1.2f), trimMat);
        Cube("Pin deck", new Vector3(0,.01f,27), new Vector3(5,.18f,5), laneMat);
        Cube("Back wall", new Vector3(0,2,31), new Vector3(16,4,.5f), floorMat);
        // Soft overhead light gives the simple primitive-built alley some depth.
        Light l = FindFirstObjectByType<Light>();
        if(l==null) l=new GameObject("Alley Light").AddComponent<Light>();
        l.type=LightType.Directional; l.intensity=1.3f; l.transform.rotation=Quaternion.Euler(48,-25,0);
    }

    void SetupCamera()
    {
        Camera c=Camera.main; if(c==null)c=new GameObject("Main Camera").AddComponent<Camera>();
        c.tag="MainCamera"; c.transform.position=new Vector3(0,9,-12); c.transform.rotation=Quaternion.Euler(20,0,0); c.fieldOfView=58;
        c.backgroundColor=new Color(.10f,.14f,.18f);
    }

    void SpawnBall()
    {
        ball=GameObject.CreatePrimitive(PrimitiveType.Sphere); ball.name="Bowling Ball";
        ball.transform.localScale=Vector3.one*.9f; ball.transform.position=new Vector3(aim,.55f,-3);
        ball.GetComponent<Renderer>().material=ballMat;
        ballBody=ball.AddComponent<Rigidbody>(); ballBody.mass=6; ballBody.drag=.18f; ballBody.angularDrag=.08f;
        ballBody.constraints=RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
        ballBody.isKinematic=true;
    }

    void ResetPins()
    {
        foreach(GameObject p in pins) if(p)Destroy(p); pins.Clear();
        int n=0;
        for(int row=0;row<4;row++) for(int col=0;col<=row;col++)
        {
            float x=(col-row*.5f)*.85f; float z=27.2f+row*.9f;
            GameObject pin=GameObject.CreatePrimitive(PrimitiveType.Capsule);
            pin.name="Bowling Pin "+(++n); pin.transform.position=new Vector3(x,.58f,z); pin.transform.localScale=new Vector3(.42f, .58f,.42f);
            pin.GetComponent<Renderer>().material=pinMat;
            pin.AddComponent<Rigidbody>().mass=1.1f;
            // Red neck band, also a simple primitive.
            GameObject band=GameObject.CreatePrimitive(PrimitiveType.Cylinder); band.name="Red pin stripe";
            band.transform.SetParent(pin.transform); band.transform.localPosition=new Vector3(0,.45f,0); band.transform.localScale=new Vector3(.48f,.045f,.48f); band.GetComponent<Renderer>().material=redMat;
            Collider colli=band.GetComponent<Collider>(); if(colli)Destroy(colli);
            pins.Add(pin);
        }
    }

    void Update()
    {
        if(rolling || resolving) return;
        if(Input.GetKeyDown(KeyCode.R)) RestartGame();
        if(Input.GetKeyDown(KeyCode.A)) aim=Mathf.Max(-1.25f,aim-.15f);
        if(Input.GetKeyDown(KeyCode.D)) aim=Mathf.Min(1.25f,aim+.15f);
        if(Input.GetKeyDown(KeyCode.W)) power=Mathf.Min(25,power+1);
        if(Input.GetKeyDown(KeyCode.S)) power=Mathf.Max(10,power-1);
        ball.transform.position=new Vector3(aim,.55f,-3);
        if(Input.GetKeyDown(KeyCode.Space)) Roll();
    }

    void RestartGame()
    {
        if(rolling || resolving) return;
        frame=1; rollInFrame=0; totalScore=0; framePins=0; firstRollPins=0;
        aim=0; power=18; status="Game restarted. Aim and roll when ready.";
        ResetPins();
        if(ball) Destroy(ball);
        SpawnBall();
    }

    void Roll()
    {
        if(rolling || resolving)return;
        rolling=true; ballBody.isKinematic=false;
        ballBody.AddForce(new Vector3(aim*.45f,0,power),ForceMode.Impulse);
        status="Ball rolling...";
        StartCoroutine(FinishRoll());
    }

    IEnumerator FinishRoll()
    {
        yield return new WaitForSeconds(3.2f);
        ballBody.velocity=Vector3.zero; ballBody.angularVelocity=Vector3.zero; ballBody.isKinematic=true;
        int knocked=0;
        foreach(GameObject p in pins) if(p && (p.transform.up.y<.65f || p.transform.position.y<.15f)) knocked++;
        knocked=Mathf.Clamp(knocked,0,10-framePins);
        framePins+=knocked; totalScore+=knocked; rollInFrame++;
        if(rollInFrame==1) firstRollPins=knocked;
        if(framePins>=10)
        {
            status=framePins==10 && rollInFrame==1 ? "STRIKE! Great roll!" : "SPARE! Nice pickup!";
            StartCoroutine(NextFrame(1.5f));
        }
        else if(rollInFrame>=2)
        {
            status="Frame complete: "+framePins+" pins."; StartCoroutine(NextFrame(1.5f));
        }
        else
        {
            status="You knocked down "+knocked+" pins. Roll again for the spare.";
            for(int i=pins.Count-1;i>=0;i--) { GameObject p=pins[i]; if(p && (p.transform.up.y<.65f || p.transform.position.y<.15f)) { pins.RemoveAt(i); Destroy(p); } }
        }
        rolling=false;
    }

    IEnumerator NextFrame(float wait)
    {
        resolving=true; yield return new WaitForSeconds(wait);
        frame++; if(frame>10){frame=1;totalScore=0;status="New game! Try for a high score.";}
        rollInFrame=0;framePins=0;aim=0; power=18;
        ResetPins(); Destroy(ball); SpawnBall(); resolving=false;
    }

    void OnGUI()
    {
        GUI.Box(new Rect(18,16,360,205),"BOWLING NIGHT");
        GUI.Label(new Rect(34,48,330,24),"Frame: "+frame+" / 10     Roll: "+(rollInFrame+1));
        GUI.Label(new Rect(34,75,330,24),"Score: "+totalScore+" pins");
        GUI.Label(new Rect(34,102,330,38),status);
        GUI.Label(new Rect(34,144,330,22),"Aim  "+aim.ToString("+0.0;-0.0;0.0")+"       Power  "+power.ToString("0"));
        GUI.enabled=!rolling&&!resolving;
        if(GUI.Button(new Rect(34,174,130,34),"◀ AIM")) aim=Mathf.Max(-1.25f,aim-.2f);
        if(GUI.Button(new Rect(174,174,130,34),"AIM ▶")) aim=Mathf.Min(1.25f,aim+.2f);
        if(GUI.Button(new Rect(34,216,270,42),"ROLL  •  SPACE")) Roll();
        GUI.enabled=true;
        GUI.Label(new Rect(18,Screen.height-36,600,28),"Keyboard: A / D aim   •   W / S power   •   SPACE roll");
    }
}
