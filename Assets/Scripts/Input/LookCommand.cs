using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        throw new System.NotImplementedException();
    }
}
