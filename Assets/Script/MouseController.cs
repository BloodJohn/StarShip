using UnityEngine;


public class MouseController : MonoBehaviour, IShip
{
    public Camera mainCamera;
    public Collider ground;
    public GameObject marker;
    public GameObject ship;
    public float smooth;
    public Animator animator;

    private Vector3 destination;

    #region unity

    void Awake()
    {
        Speed = 10;
        ShiftSpeed = 1;

        shipCommand = new StarShipCommand(this);
    }
    // Use this for initialization
    void Start()
    {
        destination = transform.position;
        shipCommand.SetPos(new Vector2D(transform.position));
        animator = ship.GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Mouse0))
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hitInfo;
            if (ground.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out hitInfo, 20f))
            {
                marker.transform.position = hitInfo.point;// + hitInfo.normal;

                destination = marker.transform.position;

                var localHit = ground.transform.InverseTransformPoint(marker.transform.position);

                Debug.LogFormat("trasnform {0} local {1}", marker.transform.position, localHit);

                shipCommand.MoveCommand(new Vector2D(destination));
            }
        }
        else
        {
            CompleteUpdating();
            transform.position = shipCommand.UpdateCurPosition(LastUpdate).Vector3;
            ship.transform.localRotation = Quaternion.LookRotation(shipCommand.curHead.Vector3);

            animator.SetBool("FWD", shipCommand.isFlyMode);
            animator.SetBool("LH", shipCommand.dirRotate == 1);
            animator.SetBool("RH", shipCommand.dirRotate == -1);
        }
    }
    #endregion

    #region IShip

    private StarShipCommand shipCommand;
    public int Speed { get; private set; }
    public int ShiftSpeed { get; private set; }
    public void CompleteUpdating()
    {
        LastUpdate = JSTime.Now;
    }

    public long LastUpdate { get; private set; }
    public Vector2D GetCurrentPosition(long timeNow)
    {
        return new Vector2D(transform.position);
    }

    public bool IsVisible(long timeNow)
    {
        return true;
    }

    private Vector3 From(Vector3 source)
    {
        return source/10f;
    }
    #endregion
}