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
        }

    }

    // TODO: Handle selection (box select/click select)
    // TODO: Handle move command (right click)

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

                Debug.Log($"New command: selected entity {entity.EntityId}");
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

            Debug.Log($"New command: deselected everything");
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
