using UnityEditor;
using UnityEngine;

public class AnimationReverseTool
{
    [MenuItem("Tools/Reverse Animation Clip")]
    public static void ReverseAnimationClip()
    {
        AnimationClip originalClip = Selection.activeObject as AnimationClip;

        if (originalClip == null)
        {
            Debug.LogError("선택한 것이 AnimationClip이 아님");
            return;
        }

        string path = AssetDatabase.GetAssetPath(originalClip);
        AnimationClip reversedClip = new AnimationClip();
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(originalClip);

        foreach (var binding in bindings)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(originalClip, binding);
            Keyframe[] keys = curve.keys;
            float clipLength = keys[keys.Length - 1].time;

            // 키 순서를 역으로 바꾸고 시간도 반전
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i].time = clipLength - keys[i].time;
            }

            // 다시 정렬 (시간 오름차순)
            System.Array.Sort(keys, (a, b) => a.time.CompareTo(b.time));

            curve.keys = keys;
            reversedClip.SetCurve(binding.path, binding.type, binding.propertyName, curve);
        }

        reversedClip.frameRate = originalClip.frameRate;
        AssetDatabase.CreateAsset(reversedClip, path.Replace(".anim", "_Reversed.anim"));
        AssetDatabase.SaveAssets();

        Debug.Log("애니메이션 역재생 클립 생성 완료!");
    }
}