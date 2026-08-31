# FAQ

## Does FUI require Addressables?

No. Implement `IViewProvider` for any resource backend or use a future optional provider package.

## Does FUI use reflection for binding?

Generated binding, routes, and factories avoid runtime type scanning and activation. Editor tooling may inspect editor-only metadata.

## Is TextMeshPro required?

FUI supports Legacy Text and TMP Elements. The editor package references TextMeshPro for the built-in TMP creation tools.

## Which property mode should a new project use?

Pure mode. Use Mixed mode only when automatic properties are required by an existing model contract.

## Can one ViewModel be rendered by more than one View?

Yes. Use `Route<T>.CreateViewVariant` or `OpenWithViewModel` while keeping each top-level ViewHandle independent.

## Are CLI tools required?

No. They are independent optional packages.
