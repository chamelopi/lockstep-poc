using System.Collections;
using System.Collections.Generic;
using Server;
using Simulation;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.UIElements;

public class InputHandler : MonoBehaviour
{

    public GameObject groundPlane;

    private Simulation.Simulation sim;


    bool isSelecting = false;
    Vector3 selectionBeginPos;


    // Start is called before the first frame update
    void Start()
    {
        sim = SimulationManager.sim;

        groundPlane = GameObject.Find("GroundPlane");
    }

    void AddCommand(Command cmd)
    {
        cmd.TargetTurn = sim.currentTurn + 2;
        sim.AddCommand(cmd);
        MenuUi.networkManager.QueuePacket(new CommandPacket()
        {
            Command = cmd,
            PkgType = PacketType.Command,
            PlayerId = MenuUi.networkManager.GetLocalPlayer(),
        });
    }

    // Update is called once per frame
    void Update()
    {
        // Spawn entity on key press
        if (Input.GetKeyDown(KeyCode.E))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (groundPlane.GetComponent<MeshCollider>().Raycast(ray, out RaycastHit hitPoint, 500f))
            {
                var cmd = new Command()
                {
                    CommandType = CommandType.Spawn,
                    PlayerId = MenuUi.networkManager!.GetLocalPlayer(),
                    TargetX = FixedPointUtil.ToFixed(hitPoint.point.x),
                    TargetY = FixedPointUtil.ToFixed(hitPoint.point.z),
                };
                AddCommand(cmd);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            isSelecting = true;
            selectionBeginPos = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(0))
        {
            isSelecting = false;
            var bounds = UiUtils.GetViewportBounds(selectionBeginPos, Input.mousePosition);

            Debug.Log("selection bounds: " + bounds);

            var cmd = new Command()
            {
                CommandType = CommandType.BoxSelect,
                PlayerId = MenuUi.networkManager!.GetLocalPlayer(),
                // FIXME: Might need all three coordinates
                TargetX = FixedPointUtil.ToFixed(bounds.center.x),
                TargetY = FixedPointUtil.ToFixed(bounds.center.y),
                BoxX = FixedPointUtil.ToFixed(bounds.size.x),
                BoxY = FixedPointUtil.ToFixed(bounds.size.y),
            };
            AddCommand(cmd);
        }

        // TODO: Handle selection (box select/click select)
        // TODO: Handle move command (right click)
    }

    void OnGUI()
    {
        if (isSelecting)
        {
            var rect = UiUtils.GetScreenRect(selectionBeginPos, Input.mousePosition);
            UiUtils.DrawSelectionRect(rect);
        }
    }

    void OnDrawGizmos()
    {
        if (isSelecting)
        {
            var bounds = UiUtils.GetViewportBounds(selectionBeginPos, Input.mousePosition);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(bounds.center, bounds.extents * 2);
            Gizmos.color = Color.white;
        }
    }


}
