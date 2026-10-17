using UnityEditor;
using UnityEngine;

/// <summary>개발용 메뉴: 저장 데이터 초기화.</summary>
public static class DevMenu
{
    /// <summary>광·기록·황금 광·튜토리얼·처음 안내까지 모두 지워 처음 실행한 상태로 (PlayerPrefs 전체 삭제, 설정·언어 포함).</summary>
    [MenuItem("Tools/Hwatu Solitaire/Reset Save (PlayerPrefs)")]
    public static void ResetSave()
    {
        if (!EditorUtility.DisplayDialog("저장 초기화", "진행·기록·튜토리얼·설정을 모두 지울까요?", "지우기", "취소")) return;
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[DevMenu] PlayerPrefs 를 모두 지웠습니다.");
    }
}
