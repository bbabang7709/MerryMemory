using System.Collections.Generic;
using UnityEngine;

/// <summary>대사 묶음. Project 창 우클릭 → Create → Merry Memory → Dialogue</summary>
[CreateAssetMenu(menuName = "Merry Memory/Dialogue", fileName = "New Dialogue")]
public class DialogueData : ScriptableObject
{
    public List<DialogueLine> lines = new();

    public bool IsEmpty => lines == null || lines.Count == 0;
}
