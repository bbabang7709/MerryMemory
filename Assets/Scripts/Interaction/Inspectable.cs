using UnityEngine;

/// <summary>조사하면 대사가 나오는 사물 (우편함, 표지판, 트리 등).</summary>
public class Inspectable : DialogueInteractable
{
    [SerializeField] private DialogueData dialogue;

    protected override string DefaultPrompt => "조사하기";
    protected override DialogueData GetDialogue() => dialogue;
}
