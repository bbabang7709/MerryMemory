using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 짝꿍 타일도 "같은 타일"로 보는 Rule Tile.
/// 규칙의 This / NotThis를 판정할 때 자기 자신뿐 아니라 Siblings 목록에 든 타일도 This로 칩니다.
///
/// 예: 눈(Terrain_SnowPatch)의 Siblings에 흙길(Terrain_Path)을 넣으면
/// 눈은 흙길 쪽으로 가장자리를 그리지 않고 끊김 없는 눈 장판으로 이어지고,
/// 눈과 흙의 경계는 흙길 타일이 자기 가장자리(눈 테두리 포함)로 그립니다.
/// 관계는 한쪽 방향이라, 흙길 쪽 규칙은 그대로 눈을 NotThis로 봅니다.
/// </summary>
[CreateAssetMenu(fileName = "SiblingRuleTile", menuName = "2D/Tiles/Sibling Rule Tile")]
public class SiblingRuleTile : RuleTile
{
    [Tooltip("이 타일과 이어져 보여야 하는 타일들. 이웃에 있으면 This로 판정합니다.")]
    public List<TileBase> m_Siblings = new List<TileBase>();

    public override bool RuleMatch(int neighbor, TileBase other)
    {
        if (other is RuleOverrideTile overrideTile)
            other = overrideTile.m_InstanceTile;

        switch (neighbor)
        {
            case TilingRuleOutput.Neighbor.This: return IsSame(other);
            case TilingRuleOutput.Neighbor.NotThis: return !IsSame(other);
        }
        return base.RuleMatch(neighbor, other);
    }

    private bool IsSame(TileBase other)
    {
        return other == this || (other != null && m_Siblings.Contains(other));
    }
}
