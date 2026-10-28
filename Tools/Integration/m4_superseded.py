"""M4(2026-10-28) 새 맵 본편 전환으로 대체된 옛 맵 검사 표시.

옛 배치(직사각형 맵 11개·45,000칸, layout D, C3a·C6·연못 고정본, 거리 mode current)를 그대로 고정하던 검사다.
새 맵 13개는 맵 구역을 감싸는 직사각형이 서로 겹치고, 경계 전체가 출입구라 이 검사들의 전제가 맞지 않는다.
대신 검사: test_m4_production(겹친 칸 인증·등록·S0 도달·출입구·옛 ID·layoutD 거리·옛 저장 도착 자리),
validate_world.py(출입구 왕복), Unity GoryeoRegion·SaveCompat·Q1·R6·TradeBalance·TownDistance 검사.
"""
import unittest
REASON='M4 새 맵(2026-10-28)으로 옛 맵 고정 검사 대체: test_m4_production·validate_world가 대신 검사'
superseded=unittest.skip(REASON)
