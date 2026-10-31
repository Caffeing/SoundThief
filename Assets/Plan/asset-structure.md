# 에셋과 프로젝트 폴더 운영

## 기본 구조

- `Assets/Game`: 공통 게임 코드, 에디터 도구, 프리팹, Resources, 셰이더, 게임용 생성 재질과 테스트.
- `Assets/Game/Stages/Stage2`: 기존 JinHo 폴더의 스테이지 2 구현. C# 네임스페이스와 어셈블리 이름은 유지한다.
- `Assets/Scenes`: 실제 게임 씬. 기존 씬 경로와 의뢰의 씬 연결을 유지한다.
- `Assets/ThirdParty`: 제작사·팩 단위로 모은 외부 에셋.
- `Assets/Settings`: 렌더러, 입력과 프로젝트 공통 설정.
- `Assets/Plan`: 기획·개발 문서와 에셋 목록.
- `Assets/TextMesh Pro`: TMP가 사용하는 표준 폴더.
- `SourceAssets`: 원본 압축파일, 추출본, 변경 전 백업. Unity가 가져오지 않으며 Git에서도 제외한다.
- `Tools/AssetPipeline`: 원본 확인과 에셋 정리에 사용한 도구.

## 원본과 중복 처리

Slavica Unity 6, Harboria Unity, Dungeon 전체 팩은 구매 페이지에서 다시 다운로드했다. 기존 Kenney·80년대 팩의 원본 압축파일도 해시를 확인해 보관함에 통합한다. 원본은 `SourceAssets/Archives`, 추출본은 `SourceAssets/Extracted`에 둔다. 다운로드 주소에 포함된 개인 구매 키는 문서나 저장소에 기록하지 않는다.

별도 원본 압축파일이 없는 WRADArms·Quaternius·Grab3D·MasterStylizedProjectiles는 현재 사용 중인 에셋과 의존성을 `SourceAssets/Snapshots`의 Unity 패키지로 백업했다. 이 파일들은 제작사에서 새로 받은 배포본과 구분한다. 기존 준비 폴더는 `SourceAssets/LegacySources`, 중복 정리 전 파일은 `SourceAssets/Backups`에서 확인할 수 있다.

Unity에서 이미 사용 중인 GUID와 게임용 재질·충돌 설정을 우선한다. 원본과 같은 GUID의 파일을 다시 덮어쓰지 않는다. 새 GUID의 에셋만 보충한다. 외부 팩의 데모 씬과 다른 엔진용 출력물은 원본 보관함에서 확인한다.

중복은 파일 내용과 가져오기 설정이 모두 같은 경우에만 합친다. 내용이나 충돌·리깅·재질 연결이 다른 경우는 변형본으로 보존한다. 중복 삭제 전에 씬·프리팹의 GUID 참조를 남길 에셋으로 바꾸고 변경 전 파일을 백업한다. Resources와 StreamingAssets는 경로 기반 로딩을 보호하기 위해 자동 중복 제거에서 제외한다.

폴더 이동은 `.meta`를 보존한다. 코드에 문자열로 저장된 에셋 경로도 갱신한다. 에셋을 다시 받을 때는 원본을 먼저 `SourceAssets`에 넣고 바로 기존 Assets 위에 덮어쓰지 않는다.

## 대용량 파일

모델·이미지·효과음과 외부 팩의 대형 `.asset` 파일은 Git LFS를 사용한다. 원본 압축파일과 추출본을 Git에 강제로 추가하지 않는다. 팀원은 저장소를 받은 뒤 Git LFS 파일을 함께 받아야 한다.
