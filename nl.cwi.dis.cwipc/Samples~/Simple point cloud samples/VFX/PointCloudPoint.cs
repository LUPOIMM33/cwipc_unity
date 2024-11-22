using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;


[VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
public struct PointCloudPoint
{
    public Vector3 position;
    public uint rgbmask;
};

