using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HueCurveData", menuName = "Custom/Hue Curve Data")]
public class HueCurveData : ScriptableObject
{
    public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
    public Texture2D backgroundTexture;
}
