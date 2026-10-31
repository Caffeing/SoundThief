# 팀원용 스테이지 제작 안내

## 시작하기

1. 각자 `Assets/Scenes/Stages/<스테이지명>/` 아래 새 씬을 저장한다. 기본 카메라는 제거한다(공통 PlayerRig에 카메라·AudioListener가 포함됨).
2. `Sound Thief > Stage > Install Foundation In Current Scene`을 실행한다. StageSystems와 PlayerRig, 씬 전용 StageDefinition이 생성되고 빌드 목록에 추가된다. 중복 설치는 거부한다.
3. StageSystems의 StageContext에서 생성된 설정을 확인한다. StageDefinition의 stageId(팀 전체 고유값), title, briefing, requiredItemIds, objectiveText, exitText를 작성한다. startImmediately를 켜면 씬 직접 실행도 바로 플레이할 수 있다.
4. PlayerRig를 시작 위치에 놓고 바닥·벽·가림막을 배치한다. 프로젝트 레이어 **8**은 소리 폭발과 시야를 가리는 지형이다. 플레이어·상호작용 물체는 기본 레이어를 사용한다. 일반 청각은 벽을 통과한다.
5. `Assets/Game/Prefabs/`에서 필요한 물체를 드래그한다. 아래 설정을 맞춘다.
6. 경비를 쓰는 씬은 AI Navigation의 NavMeshSurface로 이동 가능한 바닥을 굽는다. 문에는 carving NavMeshObstacle이 포함된다. 경비 발 위치를 NavMesh 위에 놓는다.
7. `Sound Thief > Stage > Validate Current Scene`을 실행해 오류를 고친 후 저장하고 Play한다. 목표물을 모두 모으고 출구 F, R 재시작까지 확인한다.

## 공통 프리팹

| 프리팹 | 스테이지 담당자가 설정할 값 |
|---|---|
| PlayerRig | 시작 위치·회전. 카메라 중복 배치 금지 |
| StageSystems | StageContext의 definition·player 참조. rules는 공통 PrototypeRules 공유 |
| Key / Collectible | ThiefInteractable.itemId(예: tower_key, relic_a), displayName |
| Door | 힌지 위치·회전, requiredItemId, requiresKey, openAngle. 자식 문짝과 Collider 유지 |
| Resonator | SoundRelation의 두 매개체 이름·등급(1~3), SoundEmitter 유지 |
| Guard | 배치·방향. 같은 씬 StageContext가 world를 연결 |
| Exit | 배치·표시명. 필수 아이템을 모두 소지한 경우에만 완료 |

같은 아이템 ID는 소지품 하나로 취급한다. 필수 아이템을 여러 개 요구하려면 각기 다른 ID를 목록에 넣는다. 문 열쇠 ID와 목표 ID는 독립이다. 열쇠는 사용해도 소모되지 않는다. 원래 저택 호환용 HasKey/HasTreasure는 각각 key/objective ID에 대응하며 새 코드에서는 Inventory를 사용한다.

## 공통 동작과 확장 연결

StageContext는 씬 시작 전에 MissionDirector·플레이어·HUD·SoundWorld·경비를 연결한다. 공통 시스템은 씬마다 하나이며 DontDestroyOnLoad 싱글턴을 쓰지 않는다. 동시 다중 스테이지 실행은 현재 지원 범위가 아니다.

MissionDirector의 Started/Succeeded/Failed 이벤트에 씬 담당 어댑터를 연결할 수 있다. 길드 담당자는 수락 후 자기 전환 코드에서 스테이지 씬을 로드하면 된다. startImmediately=false인 경우 외부에서 MissionDirector.Accept()를 호출해 시작한다. 완료 시 기본 동작은 시간 정지·완료 HUD이며, 길드 복귀는 담당 전환 코드가 처리한다.

R 및 경비 접촉은 **MissionDirector가 속한 씬 경로**를 다시 로드하여 위치·소지품·문·소리·경비를 초기화한다. 씬은 저장하고 빌드 목록에 등록해야 한다. Tab은 시간을 멈추지 않으며 Esc만 명시적으로 일시정지한다.

투척은 개발 보류이며 공통 SoundRules.throwingEnabled는 false로 유지한다. 기본 HUD에 좌클릭 투척을 표시하지 않는다. 입력: WASD 이동, 마우스 시점, F 상호작용, E 추출, Q 주입, Ctrl 웅크리기, Tab 의뢰, Esc 정지, R 재시작.

## 팀 작업 규약

씬에서 독립적으로 배치하는 가구·아이템은 외형, 상호작용체, 충돌체를 같은 상위 객체에 묶고 그 상위 객체를 이동한다. 상호작용체의 충돌 범위는 실제 메시 위치에 맞추며 중복 충돌체가 아이템 조준을 가리지 않게 한다. 여러 외형이 하나의 벽·바닥 충돌체를 공유하는 구조는 개별 이동 대상으로 간주하지 않는다. 가구 충돌 위치를 변경하면 해당 스테이지의 NavMesh를 다시 굽고 문 및 경비 이동을 확인한다.

공통 담당 영역은 `Assets/Game/Runtime`, `Editor`, `Prefabs`, 공통 SoundRules다. 스테이지 담당은 자기 씬 폴더, StageDefinition, 미술·배치·NavMesh를 수정한다. 공통 프리팹을 직접 덮어쓰기보다 변형 프리팹 또는 씬 인스턴스 설정을 사용한다. .meta를 함께 커밋하고 다른 팀원 씬을 같은 파일로 합치지 않는다.

공통 동작 변경이 필요하면 개별 씬에서 코드를 복사하지 말고 공통 담당과 인터페이스를 조정한다. 정식 외형은 프리팹의 시각 자식을 교체하되 플레이어 CharacterController, 문 힌지/장애물, 대상 Collider와 관계/출력 참조를 유지한다.

## 참고 예제와 현재 한계

`FrameworkExample.unity`는 copper_key로 여는 문, relic_a/relic_b 두 목표, 공명체·경비·출구를 배치한 연결 검증 예제다. `ManorPrototype.unity`는 기존 능력·경비 회귀 예제다. 둘은 같은 공통 코드를 사용한다.

기본 도형·합성 소리·Windows 한글 글꼴은 임시 자산이다. 정식 아트·애니메이션·음향, 저장/온라인, 여러 플레이어, 길드·훈련장 제작은 별도 작업이다. StageDefinition은 수집 후 탈출 의뢰를 지원하며 특수 퍼즐/보스 목표는 별도 목표 인터페이스를 확장해야 한다.

빠르게 시작하려면 StageTemplate.unity를 자기 폴더에 복사하고, 함께 생성된 Definition을 복제해 StageContext에 지정한다. StageDefinition을 공유한 채 수정하면 다른 씬의 임무도 바뀌므로 씬마다 전용 설정을 사용한다. 기본 빌드 시작 씬은 FrameworkExample이다.

