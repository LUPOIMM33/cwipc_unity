using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class PointCloud_VFX : MonoBehaviour
{
    public VisualEffect vfxGraph;
    private string DataBufferParameterName = "DataBuffer";
    private string pointCountName = "PointCount";
    private string pointSizeName = "PointSize";

    [SerializeField] private bool _passPointSize = true;

    public void PassToVFX(GraphicsBuffer DataBuffer, int nPoint, float PointSize)
    {
        vfxGraph.SetGraphicsBuffer(DataBufferParameterName, DataBuffer);
        vfxGraph.SetInt(pointCountName, nPoint);
        if (_passPointSize)
        {
            vfxGraph.SetFloat(pointSizeName, PointSize);
        }
    }

}
