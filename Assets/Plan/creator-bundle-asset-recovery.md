# Creator Bundle 에셋 복구

Harboria와 Slavica의 누락 파일은 원본 Unity GUID와 에셋 참조를 보존하여 `Assets/EmaceArt/`에 복구한다. 기존 프로젝트에 동일 GUID가 이미 존재하는 파일은 덮어쓰지 않으며 프리팹은 그 기존 파일을 참조한다. 특히 Harboria의 세 재질과 Slavica 주민의 두 애니메이션 및 재질은 원본과 내용이 다르므로 프로젝트의 기존 버전을 유지한다.

Dungeon, Kenney 가구·효과음 및 Eighties 모델·LOD·텍스처 원본은 `Assets/ThirdParty/RestoredCreatorBundle/` 아래에 분리한다. 패키지의 원본 저작권·라이선스 파일을 함께 보존한다. `AssetImportStaging`은 가져오기 준비용 사본이며 저장소에 포함하지 않는다.

복구 에셋은 프로젝트에 추가한 상태이며, 모든 프리팹이 현재 스테이지에 배치되었다는 뜻은 아니다. 추가로 배치할 때는 재질·크기·충돌체·NavMesh를 해당 스테이지에서 확인한다.
