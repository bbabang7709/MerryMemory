using UnityEngine;

/// <summary>
/// 성별에 맞는 Animator Controller를 캐릭터에 입힙니다. (로드맵 2-4)
///
/// 남/여 Override Controller는 상태(Idle, Walk)와 파라미터(DirX, DirY, IsMoving)가 같고
/// 클립(그림)만 다릅니다. 그래서 컨트롤러만 갈아 끼우면 PlayerAnimator는 그대로 동작합니다.
///
/// - 시작할 때(Start) GameSession의 성별로 한 번 입힙니다.
/// - 게임 도중 프로필이 바뀌면(GameSession.ProfileChanged) 그 자리에서 다시 입힙니다.
/// </summary>
[RequireComponent(typeof(Animator))]
public class CharacterSkin : MonoBehaviour
{
    [Tooltip("여자 주인공 컨트롤러 (Player_Female.overrideController)")]
    [SerializeField] private RuntimeAnimatorController femaleController;

    [Tooltip("남자 주인공 컨트롤러 (Player_Male.overrideController)")]
    [SerializeField] private RuntimeAnimatorController maleController;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // 다른 오브젝트가 모두 Awake를 마친 뒤이므로 GameSession 정보를 읽어 바로 입힙니다.
    private void Start()
    {
        Apply(GameSession.Profile.Gender);
    }

    // 구독(+=)과 해제(-=)는 반드시 짝으로. 해제하지 않으면 씬이 바뀐 뒤
    // 이미 사라진 이 오브젝트를 이벤트가 계속 부르다가 오류가 납니다.
    private void OnEnable() => GameSession.ProfileChanged += ChangeProfile;
    private void OnDisable() => GameSession.ProfileChanged -= ChangeProfile;

    /// <summary>주어진 성별의 컨트롤러를 Animator에 적용합니다. 비어 있으면 경고만 남깁니다.</summary>
    public void Apply(Gender gender)
    {
        var controller = ControllerFor(gender);

        if (controller != null)
        {
            animator.runtimeAnimatorController = controller;
        }
        else
        {
            // this를 넘기면 콘솔에서 이 경고를 클릭했을 때 해당 오브젝트가 선택됩니다.
            Debug.LogWarning($"{name}: {gender} 컨트롤러가 비어 있습니다. Inspector에서 넣어 주세요.", this);
        }
    }

    /// <summary>성별에 해당하는 컨트롤러. 모르는 값이면 null (→ Apply가 경고).</summary>
    public RuntimeAnimatorController ControllerFor(Gender gender)
    {
        return gender switch
        {
            Gender.Female => femaleController,
            Gender.Male   => maleController,
            _             => null,
        };
    }

    // GameSession.ProfileChanged 이벤트를 받으면 새 프로필의 성별로 다시 입힙니다.
    private void ChangeProfile(PlayerProfile profile)
    {
        if (profile == null) return;

        Apply(profile.Gender);
    }
}
