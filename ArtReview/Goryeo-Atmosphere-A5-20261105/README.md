# A5 승인·본편 연결 (2026-11-05)

사용자 “A5 이대로 승인”을 approval.json에 원문/검토 원본 SHA256과 함께 기록했다. GoryeoPlaceLabelsReview.PromoteApproved → Resources/Goryeo/PlaceLabels.json → 전용 Director로 고려5/중앙 동쪽8 본편 맵에 연결했다. 승인된4칸 임계·기존 장소21개/실시간 장터·게시판·공방·이정표·객주 담당자 표시와 중첩/중복 정리 그대로다. 실제 NPC/시설 ID·DisplayName·상호작용·퀘스트 표식·Space/목표 안내는 유지한다.

본편Play407확인:13맵 자동 설치/중복0·승인 상태/크기 검증, 거리/일반사람 구분/표시 중첩0·객주 중복 제거·모달 숨김/끄기·재활성·승인 취소 시 공용UI 복원, 실제 이름/재산/상호작용 불변·저장OFF. 다른3문명에는 모듈이 붙지 않고 공용 이름표가 활성 상태로 복원됐다. Unity/production.json·production- 접두어480×2709장이다. 공용 WorldLabels 소스는 수정하지 않고 enabled만 소유 지역에서 대신 제어하며 원래 상태로 돌린다. 맵/씬/공용/다른 지역 파일·판정/이동/경제·저장 형식은 고치지 않았다.

승인 전 검토Play385/기존·가까이·멀리13캡처는 기존 기록으로 보존했다. 최종 승인 그림은 game-comparison-final.png다. source.json은 review 원본 그대로 두고 승인 상태를 approval.json과 생성 Resources에 구분했다. 이후 검토 전용 QA는 설치된 승인 모듈을 메모리에서 제거한 뒤 별도 검토 모듈로 확인할 수 있다. 본편 배치 파일은 손대지 않는다.

최종 정적385 두 환경(23/32skip)/월드0오류1437출구/Unity 지정8종·저장9파일 보존. 사람 직접·장시간/온라인/빌드FPS 미검사. 상세 Docs/Validation/Goryeo-A5-Binding-A2-2026-11-05. 로컬 커밋만/push·PR 없음.
