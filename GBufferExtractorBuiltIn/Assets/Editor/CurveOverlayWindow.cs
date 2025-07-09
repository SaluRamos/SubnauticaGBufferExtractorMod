using UnityEditor;
using UnityEngine;
using System.Globalization;

public class CurveOverlayWindow : EditorWindow
{

    [SerializeField] private HueCurveData curveData;
    private Vector2 textureSize = new Vector2(512, 256);

    [MenuItem("Tools/Curve Overlay Editor")]
    public static void ShowWindow()
    {
        GetWindow<CurveOverlayWindow>("Curve Overlay");
    }

    private void OnGUI()
    {
        curveData = (HueCurveData) EditorGUILayout.ObjectField("Curve Data", curveData, typeof(HueCurveData), false);
        if (curveData == null) return;

        //salva os dados da curva
        curveData.curve = EditorGUILayout.CurveField("Curve", curveData.curve);
        curveData.backgroundTexture = (Texture2D)EditorGUILayout.ObjectField("Background Texture", curveData.backgroundTexture, typeof(Texture2D), false);

        if (curveData.backgroundTexture != null)
        {
            Rect textureRect = GUILayoutUtility.GetRect(textureSize.x, textureSize.y);
            EditorGUI.DrawPreviewTexture(textureRect, curveData.backgroundTexture);
            DrawCurveOverTexture(textureRect);
        }

        if (GUILayout.Button("copy ShaderLab array"))
        {
            CopyShaderLabArrayFromCurve(curveData.curve);
        }
    }

    private void DrawCurveOverTexture(Rect rect)
    {
        Handles.color = Color.black;
        const int steps = 256;

        Vector3 prevPoint = Vector3.zero;
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float value = curveData.curve.Evaluate(t);

            float x = Mathf.Lerp(rect.x, rect.xMax, t);
            float y = Mathf.Lerp(rect.yMax, rect.y, value); // eixo invertido na GUI

            Vector3 currentPoint = new Vector3(x, y, 0);

            if (i > 0)
                Handles.DrawLine(prevPoint, currentPoint);

            prevPoint = currentPoint;
        }
    }

    private void CopyShaderLabArrayFromCurve(AnimationCurve curve, int resolution = 256)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendFormat("float _HueThresholds[{0}] = {{", resolution);

        for (int i = 0; i < resolution; i++)
        {
            float t = i / (float)(resolution - 1);
            float value = Mathf.Clamp01(curve.Evaluate(t));
            sb.AppendFormat(" {0}{1}", value.ToString("0.######", CultureInfo.InvariantCulture), i < resolution - 1 ? "," : "");
        }

        sb.Append(" };");

        string result = sb.ToString();
        EditorGUIUtility.systemCopyBuffer = result;
        Debug.Log("copied array");
    }

}
