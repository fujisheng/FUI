# Batch quit regression fixture

`BatchQuitSmoke.cs.txt` is test-only. Copy it as `Assets/BatchQuitProbe/Editor/BatchQuitSmoke.cs` into an idle, isolated Core test project that already contains the Settings Sample and the following Mixed probe. Install the same packaged Generator DLL beside the probe's `.asmdef`, using the existing Setup workflow, and select `MixedNotificationProbe.Runtime` as the IL post-processing target. This fixture does not install packages or change a consumer game's settings.

`Assets/MixedNotificationProbe/Runtime/MixedNotificationProbe.Runtime.asmdef`:

```json
{
  "name": "MixedNotificationProbe.Runtime",
  "references": ["FUI.Core", "FUI.Navigation"],
  "autoReferenced": true,
  "noEngineReferences": true
}
```

`Assets/MixedNotificationProbe/Runtime/MixedNotificationViewModel.cs`:

```csharp
using FUI.Binding;
using FUI.Presentation;
using FUI.Rendering;

[assembly: ViewModule("MixedNotificationProbe.Generated", PropertyMode = PropertyGenerationMode.Mixed)]

namespace MixedNotificationProbe
{
    [ViewContract("MixedNotificationView")]
    public partial class MixedNotificationViewModel : ViewModel
    {
        [Bind("Label", nameof(ITextElement.Text))]
        public string Message { get; set; } = "initial";
    }
}
```

Run the reproducible check with the configured Unity executable and a fresh report directory:

```sh
python3 tools/verify-batch-quit.py \
  --unity /path/to/Unity \
  --project /path/to/isolated-core-project \
  --report-dir /path/to/new-report-directory
```

The check forces a real probe compilation, exits through ordinary `-batchmode -nographics -quit`, then starts a second process with `-executeMethod BatchQuitSmoke.Verify`. The method checks actual Mixed and Pure notifications and equal-value deduplication without invoking manual injection, synchronization or clean-compilation repair. Missing fixtures, an existing lock, a nonzero process exit, compiler errors, either known ILPP failure prefix or a missing success receipt cause failure. The dedicated probe's original bytes are restored; do not edit it concurrently. Raw Unity logs may contain licensing/environment details and should be reviewed before sharing.

Because the check restores its source fixture, a later launch may compile that source again. Use this script as a regression verifier, not as a cache preparation command. To prepare a consumer project, use ordinary compile-and-exit followed by a separate test/action process and verify actual notification behavior for that project's target.

`result.json` must say `smoke: passed`, both process exit codes must be zero, and `probe_restored` must be true. An earlier failing Unity run can leave an unheld lock; confirm ownership and let Unity handle that known stale lock normally rather than deleting unknown locks or stopping another Editor.
