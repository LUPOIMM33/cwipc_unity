using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class PointCloud_VFX : MonoBehaviour
{
    public VisualEffect vfxGraph;
    private string positionParameterName = "Points";
    private string pointCountName = "PointCount";

    public void PassToVFX(GraphicsBuffer pointBuffer, int nPoint, float PointSize)
    {

    vfxGraph.SetGraphicsBuffer(positionParameterName, pointBuffer);
    vfxGraph.SetInt(pointCountName, nPoint);
#if xxxjack_notyet
    // it seems pointSize isn't passed to the graphics pipeline yet. Lucas?
    vfxGraph.SetInt(pointSizeName, pointSize);
#endif
    }

}
