#!/usr/bin/env python3
"""只对已准备的隔离 Core 测试工程验证立即退出后的真实 Pure/Mixed 通知。"""

import argparse
import json
from pathlib import Path
import subprocess
import uuid


def run(unity, project, log, extra):
    command = [str(unity), "-batchmode", "-nographics", "-quit",
               "-projectPath", str(project), "-logFile", str(log), *extra]
    result = subprocess.run(command, stdin=subprocess.DEVNULL,
                            stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    content = log.read_text(errors="replace") if log.exists() else ""
    compiler_failed = any(marker in content for marker in
                          ["error CS", "Compilation failed", "Scripts have compiler errors"])
    ilpp_failed = any(marker in content for marker in
                      ["FUI ILPP automatic check failed",
                       "ObservableObject IL PostProcessor: Property injection failed"])
    return {"exit_code": result.returncode, "compiler_failed": compiler_failed,
            "ilpp_failed": ilpp_failed}, content


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--unity", required=True, type=Path)
    parser.add_argument("--project", required=True, type=Path)
    parser.add_argument("--report-dir", required=True, type=Path)
    options = parser.parse_args()
    project = options.project.resolve()
    probe = project / "Assets/MixedNotificationProbe/Runtime/MixedNotificationViewModel.cs"
    smoke = project / "Assets/BatchQuitProbe/Editor/BatchQuitSmoke.cs"
    if not options.unity.is_file() or not probe.is_file() or not smoke.is_file():
        parser.error("缺少 Unity 或已准备的隔离 Mixed/Settings/BatchQuitSmoke 测试夹具")
    if (project / "Temp/UnityLockfile").exists():
        parser.error("工程存在 UnityLockfile；先确认空闲，不删除未知锁或停止其他 Editor")
    report = options.report_dir.resolve()
    # 每次使用新的结果目录，不能把旧日志冒充本轮结果。
    report.mkdir(parents=True, exist_ok=False)
    original = probe.read_bytes()
    (report / "probe-before.cs.txt").write_bytes(original)
    receipt = {"project": str(project), "smoke": "not_run"}
    try:
        # 只修改这个专用测试源，确保本轮确实有目标程序集编译，退出后恢复精确字节。
        probe.write_bytes(original + ("\n// batch-quit regression " + str(uuid.uuid4()) + "\n").encode())
        receipt["compile"], _ = run(options.unity, project, report / "compile.log", [])
        if (receipt["compile"]["exit_code"] != 0 or receipt["compile"]["compiler_failed"]
                or receipt["compile"]["ilpp_failed"]):
            return 1
        receipt["verify"], content = run(options.unity, project, report / "verify.log",
                                         ["-executeMethod", "BatchQuitSmoke.Verify"])
        passed = (receipt["verify"]["exit_code"] == 0
                  and not receipt["verify"]["compiler_failed"]
                  and not receipt["verify"]["ilpp_failed"]
                  and "FUI_BATCH_QUIT_SMOKE Mixed=1; Pure=1" in content)
        receipt["smoke"] = "passed" if passed else "failed"
        return 0 if passed else 1
    finally:
        probe.write_bytes(original)
        receipt["probe_restored"] = probe.read_bytes() == original
        (report / "result.json").write_text(json.dumps(receipt, indent=2) + "\n")
        print(json.dumps(receipt))


if __name__ == "__main__":
    raise SystemExit(main())
