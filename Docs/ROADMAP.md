# MERRY MEMORY — 개발 로드맵

> 직접 개발하면서 따라가는 단계별 가이드입니다. 각 단계는 **목표 → 만들 폴더/파일 → 구현 방법 → 완료 기준** 순서로 되어 있습니다.
> 코드는 전부 써 주지 않고, 무엇을 어떤 구조로 만들지와 핵심 API·주의점을 알려 줍니다.
> 기획 내용은 [GDD.md](GDD.md)를 기준으로 합니다.

## 전체 흐름

| 단계 | 이름 | 끝나면 할 수 있는 것 |
|---|---|---|
| 0 | 프로젝트 정리 | 픽셀 아트가 선명하게 보이는 빈 마을 씬 |
| 1 | 기본 플레이 | 캐릭터로 마을을 걷고, 사람과 대화하고, 사물을 조사 |
| 2 | 최소 흐름 (버티컬 슬라이스) | 명찰 작성부터 엔딩 "MERRY CHRISTMAS!!"까지 한 번에 플레이 |
| 3 | 핵심 시스템 | 조각 수집, 퍼즐 틀, 저장/불러오기 |
| 4 | 구역 제작 | 조각 5개와 퍼즐이 모두 들어간 완성 플레이 |
| 5 | 연출·콘텐츠 | 대사, 사운드, 눈 효과, 컷신, 크레딧 |
| 6 | 마무리 | 플레이 테스트, 난이도 조정, 빌드 |

> **왜 2단계에서 엔딩부터 만드나요?**
> 이 게임의 존재 이유는 엔딩 인사입니다. 조각 1개짜리 짧은 버전으로 엔딩까지 먼저 이어 보면, 인사가 잘 전달되는지 일찍 확인할 수 있고 나머지는 "조각 늘리기" 작업이 됩니다.

### 공통 규칙
- **네임스페이스:** `MerryMemory.Player`, `MerryMemory.Dialogue`처럼 폴더 이름과 맞춥니다.
- **한 스크립트 = 한 역할.** 이동 스크립트가 대화를 직접 열거나, UI 스크립트가 게임 진행을 바꾸지 않게 합니다. 시스템끼리는 **C# 이벤트**로 소식을 전합니다.
- **데이터는 ScriptableObject로.** 대사, 조각 정보 같은 내용은 코드가 아니라 에셋으로 만듭니다.
- **단계마다 커밋.** 각 단계의 완료 기준을 통과하면 커밋합니다.

---

## 0단계 — 프로젝트 정리

### 목표
픽셀 아트가 흐릿하지 않게 보이고, 위아래로 겹친 캐릭터가 올바른 순서로 그려지는 빈 씬을 만듭니다.

### 폴더 구조
```
Assets/
├── Art/
│   ├── Characters/      # 주인공(남/여), 마을 사람 스프라이트
│   ├── Tilesets/        # 타일 이미지와 Tile 에셋
│   ├── Objects/         # 우체통, 트리, 소품
│   └── UI/              # 대화창, 명찰, 카드 UI
├── Audio/
├── Fonts/
├── Prefabs/
├── Scenes/
│   └── Village.unity    # SampleScene 이름을 바꿔서 사용
├── Scripts/
├── ScriptableObjects/
└── Settings/            # 템플릿이 만든 URP 설정 (그대로 둠)
```

### 할 일
1. **폴더 만들기:** 위 구조대로 만듭니다. 빈 폴더는 Git에 올라가지 않으니 걱정하지 않아도 됩니다(`.meta`만 생김).
2. **씬 이름 바꾸기:** `Assets/Scenes/SampleScene`을 `Village`로 바꾸고, **File → Build Profiles**의 씬 목록에 들어 있는지 확인합니다.
3. **스프라이트 임포트 설정 (가장 중요):** 픽셀 아트 이미지를 선택하고 Inspector에서
   - `Texture Type`: Sprite (2D and UI)
   - `Pixels Per Unit`: 캐릭터 한 칸의 픽셀 크기와 맞춤(예: 32×32 캐릭터면 **32**). 프로젝트 전체에서 **하나의 값으로 통일**합니다.
   - `Filter Mode`: **Point (no filter)**
   - `Compression`: **None**
   - 여러 프레임이 든 시트는 `Sprite Mode: Multiple` → Sprite Editor → Slice → Grid By Cell Size
   - 캐릭터는 `Pivot`을 **Bottom**으로. 발 위치를 기준으로 겹침 순서가 정해지기 때문입니다.
   - 매번 설정하기 귀찮으면 한 번 설정한 뒤 Inspector 우측 상단의 **Preset**으로 저장해 두고, **Project Settings → Preset Manager**에서 기본값으로 지정합니다.
4. **위아래 겹침 순서(Y-Sort):** `Assets/Settings/Renderer2D.asset`을 선택하고
   - `Transparency Sort Mode`: **Custom Axis**
   - `Transparency Sort Axis`: **(0, 1, 0)**
   - 이렇게 하면 화면에서 아래쪽(앞쪽)에 있는 오브젝트가 위에 그려집니다.
5. **Pixel Perfect Camera:** Main Camera에 **URP용** `Pixel Perfect Camera` 컴포넌트를 추가합니다.
   - Add Component에서 같은 이름이 두 개 보이면 URP 쪽(`UnityEngine.Rendering.Universal`)을 고릅니다. 프로젝트에 설치된 `2D Pixel Perfect` 패키지는 Built-in 렌더 파이프라인용이라 URP에서는 쓰지 않습니다.
   - `Assets Pixels Per Unit`: 3번에서 정한 값
   - `Reference Resolution`: 예) **320×180** 또는 **480×270** (16:9 기준 화면에 보일 픽셀 수)
   - `Crop Frame`: None, `Grid Snapping`: Upscale Render Texture (선명한 픽셀 우선)
6. **한글 폰트:** TextMeshPro 기본 폰트에는 한글이 없습니다.
   - 픽셀 한글 폰트(예: **Galmuri**, SIL OFL 라이선스)를 `Assets/Fonts/`에 넣고
   - **Window → TextMeshPro → Font Asset Creator**로 Font Asset을 만들거나, 폰트 파일 우클릭 → **Create → TextMeshPro → Font Asset**으로 만든 뒤 `Atlas Population Mode`를 **Dynamic**으로 둡니다(한글 전체를 굽지 않아도 됨).
   - **Project Settings → TextMesh Pro → Settings**의 Default Font Asset으로 지정합니다.
7. **주인공 스프라이트 정리:** 받은 시트가 고해상도 그림이라면 격자 크기(예: 32×32)에 맞게 다시 찍거나 정리해야 합니다. 필요하면 요청해 주세요. 밑그림 변환을 도와드릴 수 있습니다.

### 완료 기준
- [ ] 임시 스프라이트 하나를 씬에 놓고 Play했을 때 **흐릿하지 않고 픽셀이 또렷하다**
- [ ] 스프라이트 두 개를 위아래로 겹쳐 움직였을 때 **아래쪽이 앞에 그려진다**
- [ ] TextMeshPro 텍스트에 한글을 쓰면 **네모(□)가 아니라 글자가 나온다**

---

## 1단계 — 기본 플레이

### 목표
캐릭터로 마을을 걷고, 벽에 막히고, 카메라가 따라오고, 사람에게 말을 걸고, 사물을 조사할 수 있습니다.

### 폴더 구조
```
Assets/Scripts/
├── Core/
│   └── GameplayLock.cs        # 대화 중 이동 잠금
├── Player/
│   └── PlayerController.cs    # 이동 + 애니메이션 값 전달
├── Interaction/
│   ├── IInteractable.cs       # "상호작용 가능한 것" 약속
│   ├── PlayerInteractor.cs    # 가까운 대상 찾기 + E키 처리
│   ├── Inspectable.cs         # 조사하면 대사가 나오는 사물
│   └── NpcTalk.cs             # 말 걸면 대화가 시작되는 사람
├── Dialogue/
│   ├── DialogueLine.cs        # 대사 한 줄 (화자, 내용)
│   ├── DialogueData.cs        # 대사 묶음 (ScriptableObject)
│   └── DialogueRunner.cs      # 대화 진행 (다음 줄, 종료)
└── UI/
    ├── DialogueView.cs        # 대화창 표시 + 타자기 효과
    └── InteractPrompt.cs      # "[E] 말 걸기" 안내
Assets/ScriptableObjects/Dialogues/   # 대사 에셋
Assets/Prefabs/Characters/Player.prefab
Assets/Animations/Player/             # 애니메이션 클립, Animator Controller
```

### 1-1. 입력 설정
템플릿에 있는 `Assets/InputSystem_Actions.inputactions`를 그대로 씁니다. 더블클릭해서 엽니다.
- `Player/Move`: WASD, 방향키가 이미 연결되어 있습니다.
- ⚠️ `Player/Interact`: 템플릿 기본값에 **Hold**(꾹 누르기) Interaction이 걸려 있습니다. **삭제해야** 한 번 눌러서 상호작용할 수 있습니다. 바인딩에 `E`와 `Space`를 둡니다. (`Jump`가 Space를 쓰고 있다면 Jump는 지워도 됩니다.)
- 이 에셋은 **Project-wide Actions**로 지정되어 있어서 코드에서 바로 쓸 수 있습니다.
  ```csharp
  InputAction move = InputSystem.actions.FindAction("Player/Move");
  Vector2 dir = move.ReadValue<Vector2>();
  bool pressed = InputSystem.actions.FindAction("Player/Interact").WasPressedThisFrame();
  ```
  `FindAction`은 `Awake`에서 한 번만 찾아 변수에 담아 두세요.

### 1-2. 플레이어 이동 — `PlayerController`
**GameObject 구성 (Player)**
| 컴포넌트 | 설정 |
|---|---|
| `SpriteRenderer` | 주인공 스프라이트 |
| `Rigidbody2D` | Body Type **Dynamic**, Gravity Scale **0**, Constraints **Freeze Rotation Z**, Interpolate **Interpolate** |
| `CapsuleCollider2D` 또는 `BoxCollider2D` | **발 부분만** 덮도록 작게 (머리가 벽에 걸리지 않게) |
| `Animator` | 1-3에서 설정 |
| `PlayerController` | 이동 속도 |
| `PlayerInteractor` | 1-5에서 설정 |

**구현 포인트**
- `Update`에서 입력을 읽고, `FixedUpdate`에서 `rb.linearVelocity = 입력 * 속도`로 움직입니다. (Unity 6에서는 `velocity` 대신 **`linearVelocity`**)
- 대각선이 더 빠르지 않게 `Vector2.ClampMagnitude(입력, 1f)`
- 마지막으로 움직인 방향을 `Facing`(상/하/좌/우)으로 저장합니다. 상호작용 방향과 멈춰 있을 때의 모습에 씁니다.
- `GameplayLock`(1-4)이 잠겨 있으면 입력을 0으로 처리합니다.

### 1-3. 4방향 애니메이션
- 시트를 잘라서 방향별 클립을 만듭니다: `Idle_Down/Up/Left/Right`, `Walk_Down/Up/Left/Right`
  - 프레임들을 여러 개 선택해 Scene 뷰로 끌어 놓으면 클립이 자동으로 만들어집니다.
  - 오른쪽 스프라이트만 있다면 왼쪽은 `SpriteRenderer.flipX`로 처리해도 됩니다.
- Animator Controller에 파라미터 `MoveX`, `MoveY`, `Speed`(float)를 만듭니다.
- **Blend Tree (2D Simple Directional)** 두 개: Idle, Walk. 각각 `MoveX`/`MoveY`로 방향 클립을 고릅니다.
- `Speed > 0.1`이면 Walk, 아니면 Idle로 전이합니다. 전이의 `Has Exit Time`은 끄고 `Transition Duration`은 0으로 둡니다.
- 코드에서는 `animator.SetFloat("MoveX", Facing.x)` 식으로 값만 넘깁니다. 문자열 대신 `Animator.StringToHash`를 쓰면 좋습니다.

### 1-4. 이동 잠금 — `GameplayLock`
대화나 퍼즐 화면이 열려 있는 동안 캐릭터가 움직이면 안 됩니다. 각 시스템이 플레이어를 직접 건드리지 않도록 **공용 잠금**을 하나 둡니다.
```csharp
public static class GameplayLock
{
    // Acquire(owner) / Release(owner) / IsLocked
    // 여러 시스템이 동시에 잠글 수 있게 HashSet<object>로 관리
}
```
- 대화가 시작되면 `Acquire(this)`, 끝나면 `Release(this)`
- ⚠️ **같은 키 문제:** 대화를 끝내는 E키와 새 대화를 시작하는 E키가 같은 프레임에 처리되면 대화가 바로 다시 열립니다. 잠금이 풀린 프레임 번호(`Time.frameCount`)를 기록해 두고, 그 프레임에는 상호작용을 무시합니다.

### 1-5. 상호작용 — `IInteractable`, `PlayerInteractor`
```csharp
public interface IInteractable
{
    string Prompt { get; }                       // "말 걸기", "조사하기"
    void Interact(PlayerInteractor interactor);
}
```
- **`PlayerInteractor`:** 매 프레임 캐릭터 앞쪽(`Facing` 방향으로 조금 떨어진 곳)에 원을 그려 겹친 콜라이더를 찾습니다.
  - API: `Physics2D.OverlapCircle(중심, 반지름, ContactFilter2D, List<Collider2D>)`
  - 찾은 콜라이더에서 `GetComponentInParent<IInteractable>()`로 대상을 얻고, 가장 가까운 하나를 고릅니다.
  - 대상이 바뀌면 `event Action<IInteractable> TargetChanged`로 알립니다. 안내 UI가 이 이벤트를 듣습니다.
  - E키를 누르면 `target.Interact(this)`
- **상호작용 대상 GameObject:** 콜라이더가 꼭 있어야 합니다. 사람이라면 몸을 막는 콜라이더를 그대로 써도 되고, 범위를 넓히고 싶으면 `Is Trigger`를 켠 콜라이더를 하나 더 붙입니다.

### 1-6. 대화 — `DialogueData`, `DialogueRunner`, `DialogueView`
**데이터**
```csharp
[System.Serializable]
public class DialogueLine
{
    public string speaker;               // 화자 이름 ("우체국장", "{이름}")
    [TextArea(2, 4)] public string text;
}

[CreateAssetMenu(menuName = "Merry Memory/Dialogue")]
public class DialogueData : ScriptableObject
{
    public List<DialogueLine> lines;
}
```
Project 창 우클릭 → **Create → Merry Memory → Dialogue**로 대사 에셋을 만듭니다.

**역할 나누기**
| 스크립트 | 하는 일 | 하지 않는 일 |
|---|---|---|
| `DialogueRunner` | 몇 번째 줄인지 관리, E키로 넘기기, 시작·종료 이벤트, 이동 잠금 | 화면에 그리기 |
| `DialogueView` | 대화창 표시, 타자기 효과, "▼" 표시 | 다음 줄 결정 |

- 타자기 효과는 `TMP_Text.maxVisibleCharacters`를 0부터 늘리면 됩니다(문자열을 잘라 붙이는 것보다 가볍습니다).
- 글자가 나오는 중에 E키를 누르면 **한 번에 전부 표시**, 다 나온 뒤 누르면 **다음 줄**.
- `DialogueRunner`는 씬에 하나만 둡니다(`[Systems]` 같은 빈 오브젝트에 붙임).

**UI 구성**
```
Canvas  (Screen Space - Overlay, Canvas Scaler: Scale With Screen Size 1920×1080)
├── DialoguePanel      # 화면 아래쪽 패널 (Image)
│   ├── SpeakerName    # TMP_Text
│   ├── Body           # TMP_Text
│   └── NextArrow      # "▼"
└── InteractPrompt     # "[E] 말 걸기" (대상 머리 위를 따라다님)
```
- 머리 위 안내는 `Camera.main.WorldToScreenPoint(대상 위치)`로 화면 좌표를 구해 옮깁니다.
- `EventSystem`의 입력 모듈은 **Input System UI Input Module**이어야 합니다(템플릿 기본값).

### 1-7. 타일맵으로 마을 만들기
```
Grid
├── Ground      # 눈길, 바닥 (Order in Layer -10)
├── Decoration  # 꽃, 눈 더미 등 지나갈 수 있는 장식
└── Collision   # 벽, 울타리, 나무 (충돌)
```
- **Window → 2D → Tile Palette**에서 팔레트를 만들고 타일을 칠합니다.
- `Collision` 타일맵: `Tilemap Collider 2D` + `Composite Collider 2D`(자동으로 Rigidbody2D가 붙으면 Body Type을 **Static**으로). Tilemap Collider 2D의 `Composite Operation`을 **Merge**로 하면 충돌 모양이 하나로 합쳐집니다.
- 건물처럼 큰 오브젝트는 타일보다 **스프라이트 GameObject**로 놓는 편이 Y-Sort와 상호작용을 다루기 쉽습니다.

### 1-8. 카메라 — Cinemachine
Cinemachine 3이 설치되어 있습니다.
- **GameObject → Cinemachine → Targeted Cameras → Follow Camera**로 만들고 `Tracking Target`에 Player를 넣습니다.
- 마을 밖이 보이지 않게 하려면 `Cinemachine Confiner 2D`를 추가하고, 마을 경계 모양의 `PolygonCollider2D`(Is Trigger)를 지정합니다.
- Pixel Perfect Camera와 함께 쓸 때는 Cinemachine Camera에 **`Cinemachine Pixel Perfect`** 확장을 추가합니다.

### 완료 기준
- [ ] WASD/방향키로 4방향 걷기, 방향에 맞는 애니메이션
- [ ] 벽과 나무에 막힌다
- [ ] 카메라가 부드럽게 따라오고 마을 밖이 보이지 않는다
- [ ] 사람에게 다가가면 "[E] 말 걸기"가 뜨고, E로 대화, 대화 중에는 움직이지 않는다
- [ ] 대화를 끝낸 E키 때문에 대화가 다시 열리지 않는다
- [ ] 사물을 조사하면 설명 대사가 나온다

---

## 2단계 — 최소 흐름 (버티컬 슬라이스)

### 목표
**타이틀 → 명찰 작성 → 프롤로그 → 조각 1개 → 엔딩 "MERRY CHRISTMAS!!" → 타이틀**까지 처음부터 끝까지 이어 봅니다. 그래픽과 퍼즐은 임시로 만들어도 됩니다.

### 폴더 구조 (추가분)
```
Assets/Scenes/
├── Title.unity
├── NameBadge.unity          # 명찰 작성
├── Village.unity
└── Ending.unity             # 엔딩 연출 (Village에서 처리해도 됨)
Assets/Scripts/
├── Core/
│   ├── GameSession.cs       # 이번 플레이의 정보 보관 (이름, 성별, 진행)
│   └── SceneLoader.cs       # 씬 이동 + 페이드
├── Profile/
│   ├── Gender.cs            # enum
│   ├── PlayerProfile.cs     # 이름 + 성별
│   └── CharacterSkin.cs     # 성별에 맞는 Animator Controller 적용
├── Dialogue/
│   └── TextFormatter.cs     # {이름}, {이/가} 치환
└── UI/
    ├── TitleScreen.cs
    └── NameBadgeScreen.cs
```

### 2-1. 씬 사이에서 정보 유지 — `GameSession`
명찰에서 입력한 이름을 Village 씬에서도 써야 합니다. 방법은 두 가지입니다.
- **간단한 방법(권장):** 일반 C# 정적 클래스 `GameSession`에 `PlayerProfile`을 담아 둡니다. 씬이 바뀌어도 값이 남습니다.
- 씬에 있는 오브젝트가 필요하면 `DontDestroyOnLoad`를 쓴 매니저 오브젝트를 둡니다.
- 둘 중 하나로 정하고 섞어 쓰지 마세요.

### 2-2. 명찰 작성 화면 — `NameBadgeScreen`
```
Canvas
└── Badge (명찰 그림)
    ├── PhotoSlot      # 선택한 캐릭터 미리보기 (Image + Animator로 손 흔들기)
    ├── GenderLeft/Right 버튼 (또는 ←/→ 키)
    ├── NameInput      # TMP_InputField (Character Limit: 6)
    └── ConfirmButton  # "확인" → "○○(으)로 할까요?" → 도장 연출 → Village 로드
```
- `TMP_InputField`는 Windows에서 **한글 입력(IME)이 됩니다.** 조합 중인 글자가 제대로 보이는지 꼭 확인하세요.
- 빈 이름이나 공백만 있는 이름은 막습니다. `string.IsNullOrWhiteSpace`
- **확인 대화의 "(으)로"도** 2-3의 조사 처리를 씁니다.

### 2-3. 이름과 조사 치환 — `TextFormatter`
대사에는 `{이름}{이/가}`처럼 쓰고, 화면에 보여 주기 직전에 바꿉니다. `DialogueView`가 줄을 표시하기 전에 `TextFormatter.Format(text)`를 거치게 합니다.

**받침 판별**
```csharp
static bool HasFinalConsonant(char c)
{
    if (c < '가' || c > '힣') return false;        // 한글이 아니면 받침 없음으로 처리
    return (c - '가') % 28 != 0;
}
```
- 한글 음절은 `'가'`부터 순서대로 배열되어 있고, 28개마다 받침이 없는 글자가 옵니다.
- 예외: **(으)로**는 받침이 'ㄹ'일 때도 "로"입니다(`(c - '가') % 28 == 8`이면 ㄹ 받침).

**지원할 표기:** `{이/가}` `{은/는}` `{을/를}` `{과/와}` `{아/야}` `{으로/로}` `{이}`(호칭용: "지민이는" / "하나는")
- 조사 토큰은 **바로 앞 글자**를 보고 결정합니다. 정규식 `\{([^}/]+)/([^}]+)\}`로 찾고, 치환된 앞 문자열의 마지막 글자를 확인하면 됩니다.
- **테스트 먼저 써 보기 좋은 기능입니다.** `Tests/EditMode`에 "지민 → 지민이", "하나 → 하나가", "철 → 철로" 같은 테스트를 만들어 두면 안심됩니다(Test Framework 설치되어 있음).

### 2-4. 성별에 따른 스프라이트 — `CharacterSkin`
- 남/여 각각 **같은 구조의 Animator Controller**를 만듭니다. 상태와 파라미터 이름이 같아야 합니다.
- 더 간단한 방법: 하나의 Controller를 만들고 여자용은 **Animator Override Controller**로 클립만 바꿉니다. 파라미터와 상태가 자동으로 같아지므로 권장합니다.
- `CharacterSkin`은 `Start`에서 `GameSession.Profile.Gender`를 보고 `animator.runtimeAnimatorController`를 바꿉니다. 명찰 화면의 미리보기에도 같은 컴포넌트를 씁니다.

### 2-5. 임시 조각 1개와 엔딩
- 우체국장에게 말 걸기 → 프롤로그 대사 → 우체국 안의 "우표"를 조사하면 획득(퍼즐은 3단계에서) → 광장 트리를 조사하면 엔딩.
- **엔딩 최소 버전:**
  1. 이동 잠금 → 주인공이 아래(화면 쪽)를 보게 함
  2. 화면을 살짝 어둡게 하고, 주인공 정면 클로즈업 이미지(임시로 큰 스프라이트)를 띄움
  3. 엔딩 대사(GDD 3.4) → 화면 가득 **"MERRY CHRISTMAS!!"**
  4. 몇 초 뒤 타이틀로
- 이 단계에서는 연출을 코루틴 하나로 순서대로 처리해도 충분합니다. 5단계에서 Timeline으로 다듬습니다.

### 완료 기준
- [ ] 타이틀에서 시작해 엔딩까지 막힘없이 이어진다
- [ ] 입력한 이름이 대사에 나오고, 조사가 받침에 맞게 바뀐다
- [ ] 성별을 바꾸면 Village에서 다른 스프라이트로 걷는다
- [ ] 엔딩에서 "MERRY CHRISTMAS!!"를 봤을 때 **기분이 좋다** ← 가장 중요. 다른 사람에게도 보여 주세요.

---

## 3단계 — 핵심 시스템

### 목표
조각 5개를 받아들일 수 있는 구조, 재사용할 퍼즐 틀, 저장 기능을 만듭니다.

### 폴더 구조 (추가분)
```
Assets/Scripts/
├── Card/
│   ├── CardPieceData.cs      # 조각 정보 (ScriptableObject)
│   ├── CardProgress.cs       # 모은 조각 관리 + 이벤트
│   └── CardPieceReward.cs    # 퍼즐을 풀면 조각 지급
├── Puzzle/
│   ├── PuzzleBase.cs         # 모든 퍼즐의 공통 부모
│   └── (퍼즐별 스크립트는 4단계에서)
├── Flags/
│   └── GameFlags.cs          # "우체국장_첫대화_완료" 같은 진행 플래그
├── Save/
│   ├── SaveData.cs           # 저장할 내용 (직렬화용 클래스)
│   └── SaveSystem.cs         # JSON 저장/불러오기
└── UI/
    └── CardProgressView.cs   # 화면 구석의 조각 현황
Assets/ScriptableObjects/CardPieces/   # Piece_Stamp.asset 등 5개
```

### 3-1. 조각 데이터 — `CardPieceData`
```csharp
[CreateAssetMenu(menuName = "Merry Memory/Card Piece")]
public class CardPieceData : ScriptableObject
{
    public string id;                  // "stamp" — 저장할 때 쓰는 고유 키
    public string displayName;         // "우표"
    public Sprite icon;
    public string makerName;           // 만든 사람
    [TextArea] public string message;  // 카드에 적은 한마디 (엔딩용)
    public DialogueData clueDialogue;  // 받는 사람 단서 대사
}
```

### 3-2. 진행 관리 — `CardProgress`
- 모은 조각 id 목록을 들고 있고, `Collect(piece)`, `Has(piece)`, `Count`, `IsComplete`를 제공합니다.
- `event Action<CardPieceData> PieceCollected`를 둡니다. UI, 마을 변화, 저장이 각자 이 이벤트를 듣습니다. **CardProgress는 UI가 있는지 몰라야 합니다.**

### 3-3. 퍼즐 틀 — `PuzzleBase`
```csharp
public abstract class PuzzleBase : MonoBehaviour
{
    [SerializeField] string puzzleId;
    public bool IsSolved { get; private set; }
    public event Action<PuzzleBase> Solved;

    protected void MarkSolved() { /* IsSolved = true; Solved?.Invoke(this); 플래그 저장 */ }
    public virtual void OpenPuzzle() { }   // 퍼즐 UI 열기 (필요한 경우)
}
```
- 퍼즐마다 `PuzzleBase`를 상속해 **규칙만** 구현합니다.
- 보상은 따로: `CardPieceReward`가 `Solved` 이벤트를 듣고 조각 지급, 획득 연출, 단서 대사를 실행합니다. 퍼즐 코드에 보상 코드를 넣지 않습니다.

### 3-4. 진행 플래그 — `GameFlags`
"우체국장과 첫 대화를 했는가", "공방 문이 열렸는가" 같은 예/아니오 상태를 문자열 키로 관리합니다(`HashSet<string>`).
- 대화 분기: NPC가 플래그에 따라 다른 `DialogueData`를 고르게 합니다.
- 플래그 이름은 오타가 나기 쉬우니 `const string` 모음 클래스를 만들어 두면 좋습니다.

### 3-5. 저장 — `SaveSystem`
```csharp
[System.Serializable]
public class SaveData
{
    public string playerName;
    public Gender gender;
    public List<string> collectedPieces;
    public List<string> flags;
    public Vector2 playerPosition;
    public string sceneName;
}
```
- `JsonUtility.ToJson` → `File.WriteAllText(Path.Combine(Application.persistentDataPath, "save.json"), json)`
- **엔딩 클리어 여부는 따로 저장합니다**(새 게임을 시작해도 남아야 하니까). `PlayerPrefs.SetInt("EndingCleared", 1)`로 충분합니다.
- 30분 게임이므로 **조각을 얻을 때 자동 저장**만 해도 됩니다. 타이틀에 "이어하기"를 둘지는 선택 사항입니다.

### 완료 기준
- [ ] 조각을 얻으면 현황 UI가 채워진다
- [ ] 게임을 껐다 켜도 이름, 성별, 모은 조각이 남아 있다
- [ ] 엔딩을 본 뒤 새 게임을 시작해도 "엔딩 클리어" 기록은 남아 있다
- [ ] 임시 퍼즐(예: 스위치 두 개 누르기) 하나를 `PuzzleBase`로 만들어 조각 지급까지 이어진다

---

## 4단계 — 구역 제작

### 목표
조각 5개와 퍼즐을 모두 만듭니다. **한 구역씩 완성 → 플레이 → 다음 구역** 순서로 진행합니다.

### 폴더 구조 (구역마다 반복)
```
Assets/Scripts/Puzzle/
├── MailSortPuzzle.cs        # 1. 우체국
├── PopupAssemblyPuzzle.cs   # 2. 장난감 공방
├── BellSequencePuzzle.cs    # 3. 시계탑
├── IceSlidePuzzle.cs        # 4. 눈 언덕
└── AddressRestorePuzzle.cs  # 5. 빵집
Assets/Prefabs/Puzzles/
Assets/ScriptableObjects/Dialogues/<구역 이름>/
```
마을 하나에 구역을 다 넣을지, 건물 안은 씬을 나눌지 먼저 정하세요. **작은 게임이라 Village 한 씬 + 건물 내부는 같은 씬의 다른 위치**로 두고 문에서 순간 이동하는 방식이 가장 단순합니다.

### 구역별 구현 힌트
| 조각 | 퍼즐 | 구현 아이디어 |
|---|---|---|
| 1. 우표 (튜토리얼) | 우편물 분류 | 편지 오브젝트를 조사하면 들고, 맞는 우편함을 조사하면 넣는다. 들고 있는 물건은 `CarriedItem` 하나로 관리. 상호작용·대화를 자연스럽게 익히는 단계 |
| 2. 팝업 트리 | 장치 재조립 | 퍼즐 전용 UI 화면을 띄우고 부품 슬롯에 순서대로 넣기. UI 퍼즐 → `OpenPuzzle()`에서 잠금 + UI 표시 |
| 3. 멜로디 칩 | 종 순서 | 종 오브젝트마다 음 하나(`AudioSource.PlayOneShot`). 입력 순서를 `List<int>`로 모아 정답과 비교, 틀리면 초기화. 힌트 멜로디는 마을 사람 콧노래로 |
| 4. 리본 | 얼음 미끄럼 | 격자 기반: 한 방향으로 밀면 장애물을 만날 때까지 칸 단위로 이동. 플레이어 일반 이동과 분리된 **퍼즐 전용 이동 모드**로 만들고, 막혔을 때를 위해 **리셋 지점** 필수 |
| 5. 봉투 | 글자 복원 | 번진 주소의 빈칸에 마을에서 찾은 글자 조각을 넣는다. 정답 문자열 비교. 마지막 주소는 엔딩용이라 일부만 복원되게 해도 된다 |

### 자유 진행 주의점
- 조각 2~5는 순서가 자유롭습니다. **어떤 조각이 먼저 와도 대사가 어색하지 않은지** 확인하세요.
- 단서 대사에 "벌써 ○개나 모았네?"처럼 개수에 따라 달라지는 말을 넣고 싶다면 `CardProgress.Count`로 분기합니다.

### 완료 기준
- [ ] 다섯 퍼즐 모두 풀 수 있고, 막히면 힌트가 나온다
- [ ] 2~5번을 다른 순서로 두 번 이상 플레이해 봤다
- [ ] 퍼즐 중에 메뉴를 열거나 다른 곳에 가도 꼬이지 않는다

---

## 5단계 — 연출·콘텐츠

### 폴더 구조 (추가분)
```
Assets/Audio/
├── BGM/            # 마을, 엔딩 (같은 캐럴의 다른 편곡)
├── SFX/            # 발소리, 도장, 종, 획득음
└── Ambience/       # 바람
Assets/Scripts/Audio/
└── AudioManager.cs # BGM 교체(페이드), 효과음 재생
Assets/Timelines/   # 프롤로그, 엔딩 컷신
Assets/Prefabs/Effects/Snow.prefab
```

### 할 일
- **대사 정리:** 모든 대사를 `DialogueData`로. 마을 사람이 카드에 적는 한마디 5개를 씁니다.
- **엔딩을 Timeline으로:** 카메라 이동, 클로즈업, 캐럴, 메시지 표시를 Timeline에서 순서대로 배치합니다. 대사 진행은 Timeline의 **Signal**로 대화 시스템을 부르거나, 대사 구간에서 Timeline을 멈췄다가 이어 가는 방식(`PlayableDirector.Pause/Resume`)을 씁니다.
- **눈 효과:** Particle System을 카메라 자식으로 붙이면 어디서든 눈이 내립니다. 픽셀 느낌을 살리려면 작은 사각형 스프라이트를 쓰고 크기를 PPU 기준 1~2픽셀로 맞춥니다.
- **조명:** URP 2D의 `Light 2D`. Global Light는 밤 분위기(어두운 파랑), 창문과 가로등에는 Point Light 2D(따뜻한 주황). 스프라이트 머티리얼이 **Sprite-Lit-Default**여야 빛을 받습니다.
- **크레딧:** 카드의 "From." 칸 형식으로 마을 사람, 주인공 {이름}, 제작자 이름
- **타이틀 변화:** `EndingCleared`가 1이면 우체통에 "배달 완료" 도장 스프라이트를 켭니다.

### 완료 기준
- [ ] 엔딩에서 캐럴이 흐르고 카드가 펼쳐지며 모두가 "MERRY CHRISTMAS!!"를 외친다
- [ ] 소리 없이 진행되는 구간이 없다
- [ ] 엔딩 후 타이틀에 "배달 완료" 도장이 보인다

---

## 6단계 — 마무리

### 플레이 테스트 체크리스트
- [ ] 처음 하는 사람이 **30분 안에** 끝낸다 (직접 재 보기)
- [ ] 어디서도 영영 막히지 않는다 (힌트가 나오는가)
- [ ] 이름 예외: 1글자, 6글자, 영문, 숫자, 받침 'ㄹ'(예: "별")
- [ ] 대화 중 ESC, 퍼즐 중 ESC, 엔딩 중 ESC
- [ ] 창 크기를 바꿔도 픽셀이 깨지지 않는다 (1920×1080, 1280×720, 2560×1440)
- [ ] 저장 → 종료 → 이어하기가 정상

### 빌드
- **File → Build Profiles**에서 Windows 선택, 씬 순서: Title → NameBadge → Village (→ Ending)
- **Player Settings:** Company/Product Name, 아이콘, 기본 해상도, `Fullscreen Mode`
- 빌드한 실행 파일을 **다른 컴퓨터에서** 한 번 실행해 봅니다(폰트, 저장 경로 확인).

---

## 도움이 필요할 때

이 프로젝트에는 **Unity MCP**가 연결되어 있어서, 요청하시면 에디터 안의 씬·오브젝트·콘솔을 직접 보면서 도와드릴 수 있습니다.

**이렇게 요청하면 좋아요**
- "1-5 상호작용 만들었는데 리뷰해 줘" → 코드와 씬 구성을 보고 문제점을 알려 드립니다.
- "콘솔에 이 에러가 나" → 콘솔을 직접 읽고 원인을 찾습니다.
- "얼음 퍼즐 구조가 감이 안 와" → 설계를 같이 잡거나 예시 코드를 드립니다.
- "이 부분은 그냥 만들어 줘" → 직접 구현해 드립니다.

**MCP를 쓰려면**
1. Unity로 이 프로젝트를 엽니다. 처음 열 때 MCP 패키지(`com.coplaydev.unity-mcp`)가 자동으로 설치됩니다.
2. 메뉴 **Window → MCP for Unity**에서 연결 상태를 확인합니다(다른 프로젝트에서 쓰던 것과 같은 화면).
3. Claude 세션을 새로 시작하면 Unity 도구가 연결됩니다.
