# 스테이지 2·3 파일 위치 변경 안내

이번 변경은 Project 창의 파일·폴더 정리다. Hierarchy를 묶었던 작업은 되돌렸다. 게임 규칙 변경과 구분해서 읽는다.

## 스테이지 2 담당자

### 기존 구조

- 실제 게임 씬은 `Assets/Scenes/Stages/Stage2.unity`에 있었다.
- 코드·설정·재질·내비게이션·문서·테스트는 이미 `Assets/Game/Stages/Stage2/` 아래에 용도별로 분리돼 있었다.
- `Assets/Game/Stages/Stage2/Scenes/`에는 별도의 JinHo 원본/시스템 검증 씬이 있었다.

### 이번 변경

| 구분 | 기존 위치 | 새 위치 |
|---|---|---|
| 실제 게임 씬 | `Assets/Scenes/Stages/Stage2.unity` | `Assets/Scenes/Stages/Stage2/Stage2.unity` |
| 전용 코드·설정 등 | `Assets/Game/Stages/Stage2/` | 동일 |
| 원본·검증 씬 | `Assets/Game/Stages/Stage2/Scenes/` | 동일 |

실제 게임 씬을 스테이지별 폴더 안으로 옮겼다. 작업할 때는 새 위치의 `Stage2.unity`를 연다. `JinHo.unity`와 `JinHoSystemsValidation.unity`는 실제 게임 씬을 대체하도록 옮기거나 합치지 않았다.

씬 경로를 확인하는 실행 코드·테스트·편집 도구의 문자열과 Build Settings를 새 경로로 갱신했다. Hierarchy가 중첩돼도 기존 도구가 찾을 수 있도록 일부 탐색 코드는 하위 오브젝트까지 검색하게 보완했다.

## 스테이지 3 우체국 담당자

### 기존 구조

`Assets/Scenes/Stages/PostalDepot/` 한 폴더 안에 실제 씬, Runtime, Editor, Tests, Art, Docs와 규칙·의뢰 정의·NavMesh·로컬 빌드 프로필이 함께 있었다.

### 이번 변경

| 구분 | 기존 위치 | 새 위치 |
|---|---|---|
| 실제 게임 씬 | `Assets/Scenes/Stages/PostalDepot/PostalDepot.unity` | `Assets/Scenes/Stages/Stage3/PostalDepot.unity` |
| 실행 코드 | `Assets/Scenes/Stages/PostalDepot/Runtime/` | `Assets/Game/Stages/Stage3/Runtime/` |
| 편집 도구 | `Assets/Scenes/Stages/PostalDepot/Editor/` | `Assets/Game/Stages/Stage3/Editor/` |
| 테스트 | `Assets/Scenes/Stages/PostalDepot/Tests/` | `Assets/Game/Stages/Stage3/Tests/` |
| 전용 재질 | `Assets/Scenes/Stages/PostalDepot/Art/` | `Assets/Game/Stages/Stage3/Materials/` |
| 설명·검증 자료 | `Assets/Scenes/Stages/PostalDepot/Docs/` | `Assets/Game/Stages/Stage3/Documentation/` |
| README | `Assets/Scenes/Stages/PostalDepot/README.md` | `Assets/Game/Stages/Stage3/Documentation/README.md` |
| 의뢰 정의 | `Assets/Scenes/Stages/PostalDepot/PostalDepot Definition.asset` | `Assets/Game/Stages/Stage3/Settings/PostalDepot Definition.asset` |
| 게임 규칙 | `Assets/Scenes/Stages/PostalDepot/PostalDepot HMM Rules.asset` | `Assets/Game/Stages/Stage3/Settings/PostalDepot HMM Rules.asset` |
| 로컬 빌드 프로필 | `Assets/Scenes/Stages/PostalDepot/PostalDepot Local Playtest.asset` | `Assets/Game/Stages/Stage3/Settings/PostalDepot Local Playtest.asset` |
| NavMesh | `Assets/Scenes/Stages/PostalDepot/PostalDepot NavMesh.asset` | `Assets/Game/Stages/Stage3/Navigation/PostalDepot NavMesh.asset` |

작업할 때는 새 위치의 `PostalDepot.unity`를 연다. 새 코드는 Runtime, 배치·생성 도구는 Editor, 재질은 Materials에 넣는다. 편집 도구의 씬·재질·설정·NavMesh 생성 경로도 새 구조에 맞췄다.

## 두 담당자 공통 사항

- Unity AssetDatabase로 이동해 기존 `.meta`와 GUID를 유지했다. 파일 이동 때문에 새 에셋으로 다시 만들 필요가 없다.
- 기존 C# 네임스페이스와 어셈블리 이름은 유지한다. 폴더명이 Stage3가 됐어도 `SoundThief.PostalDepot` 이름을 일괄 변경하지 않는다.
- 실제 씬 파일명은 Stage2와 PostalDepot 그대로다. 씬 이름으로 로딩하는 코드에 불필요한 이름 변경을 하지 않는다.
- Build Settings와 저장된 직접 경로는 갱신했다. 개인 브랜치나 별도 도구에서 옛 경로를 문자열로 사용한다면 위 표의 새 경로로 바꾼다.
- 다른 브랜치의 작업을 합칠 때 옛 폴더를 다시 통째로 복사하지 않는다. 새 위치의 대응 파일에 수정 내용을 반영하고 기존 `.meta`를 유지한다. Unity가 변경 내역을 가져오는 동안 컴파일·이동 작업을 동시에 하지 않는다.
- 기존 작업 중인 씬을 먼저 저장하고 이번 변경을 병합한다. 기존 씬의 사용자 변경을 버리고 덮어쓰라는 뜻은 아니다.

## 확인 범위

이동 및 정리 과정에서 기존 컴포넌트와 월드 좌표가 유지되는지 확인했다. 두 씬의 누락 스크립트·프리팹, StageContext 오류는 0개였고 NavMesh 데이터 연결도 확인했다. 코드 컴파일 오류·경고는 0개다. 마지막 Hierarchy 복원 후 위치 보존을 다시 확인했다. 전체 플레이와 Windows 재빌드는 이번 파일 구조 변경에 대해 실행하지 않았다.

이 변경은 현재 로컬 작업이며 아직 커밋·푸시하지 않았다. 저장소에 반영된 뒤 팀원이 받아 적용할 수 있다.
