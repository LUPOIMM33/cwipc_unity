using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class VFX_ELemRenderer : MonoBehaviour
{
    public VisualEffect vfxGraph;
    private string DataBufferParameterName = "DataBuffer";
    private string totalPointCountName = "TotalPointCount";
    private string chunkIndexName = "ChunkIndex";
    private string chunkCountName = "ChunkCount";
    private string pointSizeName = "PointSize";

    [SerializeField] private bool _passPointSize = true;

    public void PassToVFX(GraphicsBuffer DataBuffer, int totalPointCount, int chunkIndex, int chunkCount, float pointSize)
    {
        vfxGraph.SetGraphicsBuffer(DataBufferParameterName, DataBuffer);
        vfxGraph.SetInt(chunkIndexName, chunkIndex);
        vfxGraph.SetInt(chunkCountName, chunkCount);
        vfxGraph.SetInt(totalPointCountName, totalPointCount);

        if (_passPointSize)
        {
            vfxGraph.SetFloat(pointSizeName, pointSize);
        }
    }

    public void Enable_VFX(bool enable)
    {
        vfxGraph.enabled = enable;
    }

    public void SetPointSize(float PointSize)
    {
        vfxGraph.SetFloat(pointSizeName, PointSize);
    }

}
