# 에셋 정리 도구

프로젝트 폴더 운영은 [asset-structure.md](../../Assets/Plan/asset-structure.md)를 따른다.

- `prepare_originals.py`: Downloads의 구매 원본을 SHA-256으로 확인하고 SourceAssets에 보관·추출한다. Unity 패키지를 기존 Assets 위에 덮어쓰지 않는다.
- `deduplicate.py --restore`: 추출된 Unity 패키지에서 프로젝트에 없는 GUID의 에셋만 보충한다. 현재 게임에 적용한 파일을 우선한다.
- `deduplicate.py`: 파일 내용과 가져오기 설정이 같은 중복을 검사하고, 참조를 변경 전 백업한 후 통합 대상 목록을 Logs에 기록한다. Unity에서 중복 파일을 삭제하기 전에 목록을 검토한다. 바이너리 에셋 의존성 보호 목록 `Logs/binary-asset-dependencies.txt`가 필요하다.

대규모 이동·삭제는 Unity 에디터의 AssetDatabase를 사용한다. 도구 실행 전 에디터의 씬을 저장하고, 정리 후 자동 가져오기를 복구하고 컴파일과 게임 씬 참조를 확인한다. SourceAssets와 Logs는 로컬 보관용이며 Git에 추가하지 않는다.
