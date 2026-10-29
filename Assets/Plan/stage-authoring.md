# 팀원용 스테이지 제작 안내

## 2026-10-30 상시 강조 제거 후속

소리 사물은 평소에 강조하지 않는다. 상호작용 거리에서 직접 조준한 사물 하나에만 얇고 반투명한 황동색 테두리를 0.15초에 걸쳐 표시하고, 시선을 떼면 사라진다. 무음·주입 상태는 기존 상호작용 안내와 실제 소리 반응으로 읽는다. 파란색 테두리와 평상시 연결선은 제거하고, 추출·주입에 성공한 직후에만 0.3초 연결 효과를 사용한다.

경비 시야는 상시 표시하지 않는다. 10m 이내에서 벽에 가리지 않은 활동 중 경비를 약 0.4초 동안 직접 바라보면, 현재 시야가 바닥에 옅은 적갈색으로 1.4초 동안 나타나고 사라진다. 계속 바라봐도 상시 표시로 이어지지 않는다. 시선을 0.25초 이상 뗐다가 다시 관찰하면 재확인할 수 있다. 벽·가구·닫힌 문 차폐와 수면·기절·패닉 시 숨김을 유지한다. 실제 감지 판정은 바꾸지 않는다.

현재 한국어 하이어라키와 짧은 검증 결과는 hierarchy-and-feedback.md를 따른다. 아래의 이전 청록색/상시 시야 기록은 변경 이력이다.


## 시작하기

1. 각자 `Assets/Scenes/Stages/<스테이지명>/` 아래 새 씬을 저장한다. 기본 카메라는 제거한다(공통 PlayerRig에 카메라·AudioListener가 포함됨).
2. `Sound Thief > Stage > Install Foundation In Current Scene`을 실행한다. StageSystems와 PlayerRig, 씬 전용 StageDefinition이 생성되고 빌드 목록에 추가된다. 중복 설치는 거부한다.
3. StageSystems의 StageContext에서 생성된 설정을 확인한다. StageDefinition의 stageId(팀 전체 고유값), title, briefing, requiredItemIds, objectiveText, exitText를 작성한다. startImmediately를 켜면 씬 직접 실행도 바로 플레이할 수 있다.
4. PlayerRig를 시작 위치에 놓고 바닥·벽·가림막을 배치한다. 프로젝트 레이어 **8**은 소리 폭발과 시야를 가리는 지형이다. 플레이어·상호작용 물체는 기본 레이어를 사용한다. 일반 청각은 벽을 통과한다.
5. `Assets/SoundThief/Prefabs/`에서 필요한 물체를 드래그한다. 아래 설정을 맞춘다.
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

2026-10-29 최신 위키 연결 기준: 길드에서 의뢰를 수락해도 그 자리에서 실전 씬을 로드하지 않는다. 담당 전환 코드는 **의뢰 수락 + 출구 문 상호작용**을 확인한 뒤 로드한다. 연습 여부·튜토리얼 완료는 조건으로 검사하지 않는다. 위의 MissionDirector.Accept()는 실전 씬의 시작 처리이며 길드의 수락 상태 저장·이동 허용과 별개다.

길드 담당의 로비 뒤쪽 연습장에서는 퇴장 시 운반 소리·관계 무음·주입·시각 효과·연습 기록·연습 기절과 NPC의 위치/목표/타이머/감지를 초기화한다. 로비 체류 중 연습 NPC의 감지를 중단하고 재입장 시 복원한다. 의뢰 수락과 실제 능력은 보존하며 연습 소리를 실전으로 넘기지 않는다. 이 요구는 길드 쪽 연결 기준으로 전달하고, 현재 스테이지 제작자가 길드 씬을 추가하거나 실전 재시작 규칙을 바꿀 필요는 없다. 상세 출처·반영 상태는 wiki-sync.md를 따른다.

R 및 경비 접촉은 **MissionDirector가 속한 씬 경로**를 다시 로드하여 위치·소지품·문·소리·경비를 초기화한다. 씬은 저장하고 빌드 목록에 등록해야 한다. Tab은 시간을 멈추지 않으며 Esc만 명시적으로 일시정지한다.

투척은 개발 보류이며 공통 SoundRules.throwingEnabled는 false로 유지한다. 기본 HUD에 좌클릭 투척을 표시하지 않는다. 입력: WASD 이동, 마우스 시점, F 상호작용, E 추출, Q 주입, Ctrl 웅크리기, Tab 의뢰, Esc 정지, R 재시작.

## 팀 작업 규약

공통 담당 영역은 `Assets/SoundThief/Runtime`, `Editor`, `Prefabs`, 공통 SoundRules다. 스테이지 담당은 자기 씬 폴더, StageDefinition, 미술·배치·NavMesh를 수정한다. 공통 프리팹을 직접 덮어쓰기보다 변형 프리팹 또는 씬 인스턴스 설정을 사용한다. .meta를 함께 커밋하고 다른 팀원 씬을 같은 파일로 합치지 않는다.

공통 동작 변경이 필요하면 개별 씬에서 코드를 복사하지 말고 공통 담당과 인터페이스를 조정한다. 정식 외형은 프리팹의 시각 자식을 교체하되 플레이어 CharacterController, 문 힌지/장애물, 대상 Collider와 관계/출력 참조를 유지한다.

## 참고 예제와 현재 한계

`FrameworkExample.unity`는 copper_key로 여는 문, relic_a/relic_b 두 목표, 공명체·경비·출구를 배치한 연결 검증 예제다. `ManorPrototype.unity`는 기존 능력·경비 회귀 예제다. 둘은 같은 공통 코드를 사용한다.

기본 도형·합성 소리·Windows 한글 글꼴은 임시 자산이다. 정식 아트·애니메이션·음향, 저장/온라인, 여러 플레이어, 길드·훈련장 제작은 별도 작업이다. StageDefinition은 수집 후 탈출 의뢰를 지원하며 특수 퍼즐/보스 목표는 별도 목표 인터페이스를 확장해야 한다.

빠르게 시작하려면 StageTemplate.unity를 자기 폴더에 복사하고, 함께 생성된 Definition을 복제해 StageContext에 지정한다. StageDefinition을 공유한 채 수정하면 다른 씬의 임무도 바뀌므로 씬마다 전용 설정을 사용한다. 기본 빌드 시작 씬은 FrameworkExample이다.



## 소리 중심 잠입 설정 (2026-10-29)

SoundRelation.soundClip에 실제 관계 소리를 넣으면 주입 대상이 달라도 그 음색을 유지한다. 미지정 관계만 등급별 임시 합성음을 사용한다. 익숙한 시계 등의 반복 환경음은 RelationAmbience에 world·간격·음량을 지정한다. 이 환경음은 의심 자극과 구분되며 관계의 무음 상태를 따른다.

구조 벽 Collider의 루트에 SoundBarrier를 추가한다. 기본 가청 범위 배율은 0.65이고 닫힌 문은 SoundRules.closedDoorHearingMultiplier(기본 0.55)를 사용한다. 여러 구조물은 배율을 곱하며 등급·보유 시간은 바꾸지 않는다. 모든 가구 Collider에 붙이지 않는다. 열린 문은 문 감쇠를 적용하지 않는다.

Guard.startsAwake=true와 씬의 patrolPoints 배열·patrolWaitSeconds로 근무 경로를 지정한다. false이면 수면으로 시작한다. 경비가 소리 확인·추적·경계를 마친 뒤 원래 근무로 돌아온다. 시작 지점과 경로 지점 모두 NavMesh 위에 놓고 실제 경로를 확인한다. 순찰 자체를 다른 팀원 씬에 강제하지 않는다.

Door.autoCloseSeconds=0은 기존의 열림 유지다. 양수는 자동 닫힘이며 원래 관계의 소리를 사용하고 플레이어·경비가 2.5m 안에 있으면 미룬다. 목표물에도 SoundRelation을 지정하면 획득 순간에 소리가 나며, 미리 관계를 훔친 경우에만 그 소리가 억제된다.

씬 인스턴스에서 공통 프리팹 컴포넌트를 편집하는 에디터 도구는 PrefabUtility.RecordPrefabInstancePropertyModifications 또는 SerializedObject를 사용해 값을 저장한다. 저장 후 씬을 다시 열어도 설정과 참조가 유지되는지 반드시 확인한다.


## 소리 사물·경비의 읽기 쉬운 표시

SoundObjectOutline은 ThiefInteractable과 같은 루트에 붙이고 Manor sound outline 재질을 지정한다. 기본 표시 거리는 10m이며 실제 모델 외곽선만 표시한다. 소리를 낼 관계나 주입기가 있는 대상에 사용하고, 목표물처럼 획득 시 사라지는 물건에는 주입기를 추가하지 않는다. 안정적으로 남는 사물만 주입기로 사용한다.

GuardVisionCone은 GuardAgent와 같은 루트에 붙이고 Manor guard floor vision 재질을 지정한다. SoundRules의 sightRange/sightAngle과 geometryMask의 눈높이 차폐를 사용하며 수면·기절·패닉에는 표시하지 않는다. 표시용 메시에는 Collider가 없고 경비의 실제 시야나 길찾기를 변경하지 않는다. 다른 팀원 씬은 필요한 경우에만 이 컴포넌트를 추가한다.

투구/장비를 맞출 때 SkinnedMeshRenderer.bounds가 실제 머리보다 넓게 잡힐 수 있다. 실제 포즈를 구운 꼭짓점으로 머리 크기와 높이를 확인하고 장비를 머리뼈 자식으로 유지한 뒤 대기·걷기 중 위치를 확인한다.


## 경비·가구 이동망 후속

현재 저택은 가구 폴더 이동에 안전하도록 NavMeshSurface 수집을 All로 설정한다. 가구에는 물리 충돌과 이동 불가 영역을 지정하고, 수정 후 이동망을 재생성한다. 낮은 소파 상판도 경비가 올라가지 않도록 Not Walkable을 지정한다. 현재 이동망과 자세 보정은 guard-presentation-and-collision.md를 따른다.
