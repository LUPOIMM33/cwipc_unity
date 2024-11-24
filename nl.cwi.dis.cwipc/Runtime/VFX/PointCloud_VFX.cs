using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class PointCloud_VFX : MonoBehaviour
{
    public VisualEffect vfxGraph;
    private string positionParameterName = "Positions";
    private string colorParameterName = "Colors";
    private string pointCountName = "PointCount";

    public GraphicsBuffer positionBuffer;
    public GraphicsBuffer colorBuffer;

    public void PassToVFX(GraphicsBuffer pointBuffer, int nPoint, float PointSize)
    {
#if xxxjack_old
        if (positions == null || colors == null || positions.Length == 0 || colors.Length == 0)
        {
            Debug.LogWarning("Positions or colors array is empty. Skipping PassToVFX.");
            return;
        }
        // Check if buffers exist and if they need resizing
        if (positionBuffer == null || positionBuffer.count != positions.Length)
        {
            positionBuffer?.Release();
            positionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, positions.Length, sizeof(float) * 4);
            vfxGraph.SetGraphicsBuffer(positionParameterName, positionBuffer);
        }

        if (colorBuffer == null || colorBuffer.count != colors.Length)
        {
            colorBuffer?.Release();
            colorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, colors.Length, sizeof(float) * 4);
            vfxGraph.SetGraphicsBuffer(colorParameterName, colorBuffer);
        }

        vfxGraph.SetInt(pointCountName, positions.Length);
        // Set data in buffers
        positionBuffer.SetData(positions);
        colorBuffer.SetData(colors);
#else
    vfxGraph.SetGraphicsBuffer(positionParameterName, pointBuffer);
    vfxGraph.SetInt(pointCountName, nPoint);
//    vfxGraph.SetInt(pointSizeName, pointSize);
#endif
    }

    public void PassToVFX(GraphicsBuffer positions_GraphicBuffer, GraphicsBuffer colors_GraphicBuffer)
    {
        if (positions_GraphicBuffer == null || colors_GraphicBuffer == null)
        {
            Debug.LogError("One or both GraphicsBuffers are null.");
            return;
        }

        // Set the GraphicsBuffers to the VFX Graph
        vfxGraph.SetGraphicsBuffer(positionParameterName, positions_GraphicBuffer);
        vfxGraph.SetGraphicsBuffer(colorParameterName, colors_GraphicBuffer);

        Debug.Log("GraphicsBuffers passed to VFX Graph successfully.");
    }

    void OnDestroy()
    {
        // Release the buffers when they�re no longer needed
        positionBuffer?.Release();
        colorBuffer?.Release();
    }
}
