using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Command to handle player camera look using input delta from MouseLookWithTouch.
/// Executes only on the owning client to prevent multiplayer null reference issues.
/// </summary>
public class LookCommand : ICommand
{
    private CameraLook cameraLook;
    private MouseLookWithTouch mouseLookWithTouch;

    public LookCommand(CameraLook cameraLook, MouseLookWithTouch mouseLookWithTouch)
    {
        this.cameraLook = cameraLook;
        this.mouseLookWithTouch = mouseLookWithTouch;
    }

    public void Execute()
    {
        cameraLook.Look(mouseLookWithTouch.GetLookDelta());
    }

    public void Undo()
    {
        // No implementation needed for look undo in this context
    }
}
