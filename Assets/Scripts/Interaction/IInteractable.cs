using UnityEngine;

/// <summary>플레이어가 다가가서 상호작용 키로 사용할 수 있는 대상.</summary>
public interface IInteractable
{
    /// <summary>안내 문구. 예: "말 걸기", "조사하기"</summary>
    string Prompt { get; }

    /// <summary>안내 UI를 띄울 월드 좌표 (보통 머리 위).</summary>
    Vector3 PromptPosition { get; }

    void Interact(PlayerInteractor interactor);
}
