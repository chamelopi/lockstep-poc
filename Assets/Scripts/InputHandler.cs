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
        if (Input.GetKeyDown(KeyCode.F))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (groundPlane.GetComponent<MeshCollider>().Raycast(ray, out RaycastHit hitPoint, 500f))
            {
                var cmd = new Command()
                {
                    CommandType = CommandType.MassSpawn,
                    PlayerId = MenuUi.networkManager!.GetLocalPlayer(),
                    TargetX = FixedPointUtil.ToFixed(hitPoint.point.x),
                    TargetY = FixedPointUtil.ToFixed(hitPoint.point.z),
                };
                AddCommand(cmd);
            }
        }
        if (Input.GetKeyDown(KeyCode.H)) {
            Debug.Log("Checking simulation determinism....");
            sim.CheckFullDeterminism();
        }
        if (Input.GetKeyDown(KeyCode.P)) {
            Debug.Log("Toggle pause");
            sim.TogglePause();
        }

        if (Input.GetMouseButtonUp(1))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (groundPlane.GetComponent<MeshCollider>().Raycast(ray, out RaycastHit hitPoint, 500f))
            {
                var cmd = new Command
                {
                    PlayerId = MenuUi.networkManager!.GetLocalPlayer(),
                    CommandType = CommandType.Move,
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
            

            // Left click select vs. box select
            if (Vector3.Distance(selectionBeginPos, Input.mousePosition) < 1.0f)
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (groundPlane.GetComponent<MeshCollider>().Raycast(ray, out RaycastHit hitPoint, 500f))
                {
                    HandleLeftClickSelection(hitPoint.point);
                }
            }
            else
            {
                var minPos = Vector3.Min(selectionBeginPos, Input.mousePosition);
                var maxPos = Vector3.Max(selectionBeginPos, Input.mousePosition);

                var cmd = new Command()
                {
                    CommandType = CommandType.BoxSelect,
                    PlayerId = MenuUi.networkManager!.GetLocalPlayer(),
                    TargetX = (long)minPos.x,
                    TargetY = (long)minPos.y,
                    BoxX = (long)maxPos.x,
                    BoxY = (long)maxPos.y,
                };
                AddCommand(cmd);
            }
        }
    }

    void HandleLeftClickSelection(Vector3 hitPoint)
    {
        // Check for entities in the close proximity
        bool hit = false;
        foreach (var entity in sim.currentState.Entities.Values)
        {
            // Guard against selecting other player's entities.
            if (entity.OwningPlayer != MenuUi.networkManager!.GetLocalPlayer())
            {
                continue;
            }

            var dist = FixedPointUtil.Distance(entity.X, entity.Y, hitPoint.x, hitPoint.z);
            if (dist < FixedPointUtil.One * 2)
            {
                var cmd = new Command
                {
                    EntityId = entity.EntityId,
                    PlayerId = MenuUi.networkManager!.GetLocalPlayer(),
                    CommandType = CommandType.Select,
                };
                AddCommand(cmd);
                hit = true;

                // Select only one entity this way
                break;
            }
        }

        // If we hit nothing, deselect
        if (!hit)
        {
            var cmd = new Command
            {
                PlayerId = MenuUi.networkManager!.GetLocalPlayer(),
                CommandType = CommandType.Deselect,
            };
            AddCommand(cmd);
        }
    }

    void OnGUI()
    {
        if (isSelecting)
        {
            var rect = UiUtils.GetScreenRect(selectionBeginPos, Input.mousePosition);
            UiUtils.DrawSelectionRect(rect);
        }
    }


}
