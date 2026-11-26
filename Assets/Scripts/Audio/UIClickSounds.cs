using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 이 오브젝트 아래의 모든 버튼에 "틱" 소리를 붙입니다. (로드맵 5-2) 타이틀·명찰 화면의 Canvas에 붙여 둡니다.
/// 화면 스크립트(TitleScreen 등)를 고치지 않고도 버튼마다 소리가 납니다.
/// </summary>
public class UIClickSounds : MonoBehaviour
{
    private void Start()
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
            button.onClick.AddListener(() => AudioManager.PlaySfx(Sfx.Click));
    }
}
