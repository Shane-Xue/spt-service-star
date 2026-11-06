from pathlib import Path
project = Path(__file__).resolve().parents[1]
p = project / 'package.ps1'
s = p.read_text(encoding='utf-8-sig').replace('1.2.0', '1.2.1').replace('49 项自动断言与 14 项接口', '56 项自动断言与 16 项接口')
s = s.replace('扩展手雷、刀具、独立及下挂发射器的击杀统计、检视显示与服务端分类校验；自动识别沿用游戏类型与模板继承关系的模组新增武器，保留已有进度。', '检视与升星通知改为自动跟随游戏 locale，ch 中文、en 英文，其他未翻译语言回退英文；武器名使用游戏本地化，移除手动 Chinese 开关。')
p.write_text(s, encoding='utf-8-sig')
p = project / 'tools/finalize.py'
s = p.read_text(encoding='utf-8').replace('1.2.0', '1.2.1').replace('"automatic_assertions": 49', '"automatic_assertions": 56').replace('"assembly_binding_checks": 14', '"assembly_binding_checks": 16')
p.write_text(s, encoding='utf-8')
