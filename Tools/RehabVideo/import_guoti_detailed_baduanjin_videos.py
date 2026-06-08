#!/usr/bin/env python3
"""Import the 30 detailed Guoti Baduanjin mp4 files from a zip archive."""

from __future__ import annotations

import argparse
import shutil
import sys
import zipfile
from pathlib import Path


PROJECT_RELATIVE_TARGET = Path("Assets/_Project/Videos/Rehab/Baduanjin/GuotiDetailed")

MAPPINGS = [
    ("国体动作/1. 无极桩/1. 无极桩-1.mp4", "00_wuji_zhuang.mp4"),
    ("国体动作/2.1 抱球桩（预备势）/2.1 抱球桩（预备势）-1.mp4", "01_baoqiu_zhuang.mp4"),
    ("国体动作/3.1 两手托天理三焦（6 次）/3.1 两手托天理三焦（6 次）-1.mp4", "02_liangshou_tuotian.mp4"),
    ("国体动作/4.1 右开弓/4.1 右开弓-1.mp4", "03_you_kaigong.mp4"),
    ("国体动作/4.2 右开工并步/4.2 右开工并步-1.mp4", "04_you_kaigong_bingbu.mp4"),
    ("国体动作/4.3 左开工/4.3 左开工-1.mp4", "05_zuo_kaigong.mp4"),
    ("国体动作/4.4 左开弓并步/4.4 左开弓并步-1.mp4", "06_zuo_kaigong_bingbu.mp4"),
    ("国体动作/4.1 右上举/4.1 右上举-1.mp4", "07_you_shangju.mp4"),
    ("国体动作/5.2 右下落/5.2 右下落-1.mp4", "08_you_xialuo.mp4"),
    ("国体动作/5.3 左上举/5.3 左上举-1.mp4", "09_zuo_shangju.mp4"),
    ("国体动作/5.4 左下落/5.4 左下落-1.mp4", "10_zuo_xialuo.mp4"),
    ("国体动作/6.1 右后瞧/6.1 右后瞧-1.mp4", "11_you_houqiao.mp4"),
    ("国体动作/6.1 右后瞧转正/6.1 右后瞧转正-1.mp4", "12_you_houqiao_zhuanzheng.mp4"),
    ("国体动作/6.3 左后瞧/6.3 左后瞧-1.mp4", "13_zuo_houqiao.mp4"),
    ("国体动作/6.4  左后瞧转正/6.4  左后瞧转正-1.mp4", "14_zuo_houqiao_zhuanzheng.mp4"),
    ("国体动作/7.1 上托下按/7.1 上托下按-1.mp4", "15_shangtuo_xiaan.mp4"),
    ("国体动作/7.2 右旋摇头摆尾/7.-1.mp4", "16_youxuan_yaotou_baiwei.mp4"),
    ("国体动作/7.3 左旋摇头摆尾/7.2 左旋摇头摆尾-1.mp4", "17_zuoxuan_yaotou_baiwei.mp4"),
    ("国体动作/8.1 两手攀足固肾腰/8.1 两手攀足固肾腰-1.mp4", "18_liangshou_panzu.mp4"),
    ("国体动作/8.2 抬手反穿/8.2 抬手反穿-1.mp4", "19_taishou_fanchuan.mp4"),
    ("国体动作/8.3 反穿攀足/8.3 反穿攀足-1.mp4", "20_fanchuan_panzu.mp4"),
    ("国体动作/8.4 攀足举手/8.4 攀足举手-1.mp4", "21_panzu_jushou.mp4"),
    ("国体动作/8.5 举手下按复位/8.5 举手下按复位-1.mp4", "22_jushou_xiaan_fuwei.mp4"),
    ("国体动作/9.1 攒拳马步/9.1 攒拳马步-1.mp4", "23_cuanquan_mabu.mp4"),
    ("国体动作/9.2 出拳收拳/9.2 出拳收拳-1.mp4", "24_chuquan_shouquan.mp4"),
    ("国体动作/9.3 换手出拳收拳/9.3 换手出拳收拳-1.mp4", "25_huanshou_chuquan_shouquan.mp4"),
    ("国体动作/9.4 结束复位/9.4 结束复位-1.mp4", "26_jieshu_fuwei.mp4"),
    ("国体动作/10.1 提踵/10.1 提踵-1.mp4", "27_tizhong.mp4"),
    ("国体动作/11.1 双手抱腹/11.1 双手抱腹-1.mp4", "28_shuangshou_baofu.mp4"),
    ("国体动作/11.2 收势调息/11.2 收势调息-1.mp4", "29_shoushi_tiaoxi.mp4"),
]


def normalized_zip_name(name: str) -> str:
    return name.replace("\\", "/").lstrip("./")


def find_member(zip_file: zipfile.ZipFile, source_path: str) -> zipfile.ZipInfo | None:
    expected = normalized_zip_name(source_path)
    for member in zip_file.infolist():
        member_name = normalized_zip_name(member.filename)
        if member.is_dir():
            continue
        if member_name == expected or member_name.endswith("/" + expected):
            return member
    return None


def resolve_project_root(script_path: Path) -> Path:
    return script_path.resolve().parents[2]


def import_videos(zip_path: Path, project_root: Path) -> int:
    target_dir = project_root / PROJECT_RELATIVE_TARGET
    target_dir.mkdir(parents=True, exist_ok=True)

    imported: list[tuple[str, Path]] = []
    missing: list[str] = []

    with zipfile.ZipFile(zip_path, "r") as zip_file:
        for source, target_name in MAPPINGS:
            member = find_member(zip_file, source)
            target_path = target_dir / target_name
            if member is None:
                missing.append(source)
                continue

            with zip_file.open(member, "r") as src, target_path.open("wb") as dst:
                shutil.copyfileobj(src, dst)
            imported.append((member.filename, target_path))

    print(f"Target directory: {target_dir}")
    print(f"Imported {len(imported)} / {len(MAPPINGS)} videos:")
    for source, target_path in imported:
        print(f"  {source} -> {target_path.relative_to(project_root).as_posix()}")

    if missing:
        print("\nMissing videos:", file=sys.stderr)
        for source in missing:
            print(f"  {source}", file=sys.stderr)
        return 1

    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Import detailed Guoti Baduanjin mp4 files from zip.")
    parser.add_argument("zip_path", type=Path, help="Path to 国体动作.zip")
    parser.add_argument(
        "--project-root",
        type=Path,
        default=resolve_project_root(Path(__file__)),
        help="Unity project root. Defaults to the repository root.",
    )
    args = parser.parse_args()

    zip_path = args.zip_path.expanduser().resolve()
    if not zip_path.is_file():
        print(f"Zip file not found: {zip_path}", file=sys.stderr)
        return 2

    return import_videos(zip_path, args.project_root.expanduser().resolve())


if __name__ == "__main__":
    raise SystemExit(main())
