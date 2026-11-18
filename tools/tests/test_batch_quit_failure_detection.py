import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

VERIFIER = Path(__file__).resolve().parents[1] / "verify-batch-quit.py"

class BatchQuitFailureDetectionTests(unittest.TestCase):
    def run_case(self, stage, error):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            probe = root / "Project/Assets/MixedNotificationProbe/Runtime/MixedNotificationViewModel.cs"
            smoke = root / "Project/Assets/BatchQuitProbe/Editor/BatchQuitSmoke.cs"
            probe.parent.mkdir(parents=True)
            smoke.parent.mkdir(parents=True)
            probe.write_bytes(b"// dedicated test fixture\n")
            smoke.write_bytes(b"// boundary fixture\n")
            original = probe.read_bytes()
            producer = root / "UnityResponseFixture"
            # 只模拟外部进程的日志/退出码边界，断言真实验证器的结果而非调用次数。
            producer.write_text("#!" + sys.executable + "\n" +
                "import pathlib, sys\nargs=sys.argv[1:]\n" +
                "verify='-executeMethod' in args\n" +
                "content='FUI_BATCH_QUIT_SMOKE Mixed=1; Pure=1\\n' if verify else ''\n" +
                "if verify == " + repr(stage == "verify") + ": content += " + repr(error) + "\n" +
                "pathlib.Path(args[args.index('-logFile')+1]).write_text(content)\n")
            producer.chmod(0o755)
            report = root / "Report"
            result = subprocess.run([sys.executable, str(VERIFIER), "--unity", str(producer),
                "--project", str(root / "Project"), "--report-dir", str(report)], capture_output=True, text=True)
            receipt = json.loads((report / "result.json").read_text())
            self.assertEqual(probe.read_bytes(), original)
            self.assertTrue(receipt["probe_restored"])
            return result.returncode, receipt

    def test_success_marker_does_not_hide_exit_processing_failure(self):
        for error in ["FUI ILPP automatic check failed: boundary failure",
                      "ObservableObject IL PostProcessor: Property injection failed: boundary failure"]:
            with self.subTest(error=error):
                code, receipt = self.run_case("verify", error)
                self.assertEqual(code, 1)
                self.assertEqual(receipt["smoke"], "failed")

    def test_compile_processing_failure_is_rejected(self):
        code, receipt = self.run_case("compile", "FUI ILPP automatic check failed: boundary failure")
        self.assertEqual(code, 1)
        self.assertEqual(receipt["smoke"], "not_run")

    def test_healthy_receipt_still_passes(self):
        code, receipt = self.run_case("verify", "ordinary warning")
        self.assertEqual(code, 0)
        self.assertEqual(receipt["smoke"], "passed")

if __name__ == "__main__":
    unittest.main()
