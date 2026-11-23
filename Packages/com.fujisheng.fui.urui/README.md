# FUI.URUI

需要 Unity 6000.3、URP 17.3、FUI Core 2.0 和 URUI Core 0.8。通过 Unity Package Manager 分别安装 Core、URUI 和下列适配包。Git 安装可使用下列 URL，也可用固定提交 SHA 替换 URL 中的分支名。

```text
https://github.com/fujisheng/FUI.git?path=/Packages/com.fujisheng.fui#main
https://github.com/fujisheng/URUI.git?path=/Packages/com.fujisheng.urui#main
https://github.com/fujisheng/FUI.git?path=/Packages/com.fujisheng.fui.urui#main
```

FUI.Rendering.URUI 将后端无关的FUI契约接到URUI，不包含业务资源路径、页面名分支或游戏模型。后端在开发/启动时选择；VM、Presenter、路由和游戏服务无需更改。

`FUI.Rendering.URUI.ViewCatalog` 以 ViewModel 类型索引不可变的 `ViewDefinition`。普通绑定使用 HTML 节点的 `id`，大小写与 `Bind`、`Command` 的名字一致。按钮、图片、文本、文字输入、范围输入和复选框按节点类型提供默认能力。每个条目有自己的绑定作用域，可以复用模板中的 ID。游戏组件可以用显式适配器声明额外能力。

```html
<label for="Slider_Music">音乐音量</label>
<input id="Slider_Music" type="range" min="0" max="1" step="any" value="1">
<button id="Btn_Close" type="button">关闭</button>
```

`CreateBindings` 的显式声明优先匹配同名绑定。渲染扩展通过适配器装配控件，ViewModel 和 Presenter 使用共享能力、属性和命令。

`ViewContext` 持有 `Assets`、`Clock`、`Scope` 和表现资源。资源按获取顺序逆序释放，最后由 `View` 释放 `ViewScope`。`Stage` 持有共享的 `Surface`，并为每个顶层页面创建独立的 `Document` 和 `ViewScope`。文档样式由 HTML head 中的样式表链接决定。`ConfigureDocument` 在各文档首次加载前装配可选模块，资源由注入的 `IViewAssets` 按完整路径解析。

HTML/CSS拥有静态节点、布局、皮肤和动画；列表克隆预声明模板，运行时只更新数据、类型化数值、class及必要几何。通用滚动/逐项入场/路径属于URUI；Effect独立可选；项目负责英雄视频、品质规则、音效选择等业务表现。

正式页面由 `IViewAssets.LoadCompiled` 提供 `CompiledDocumentAsset`，预加载租约持有同一资产。Editor 编译页面、CSS、模板和简单选择器索引。列表接收编译资源的规范 URI，或同一文档中的 `<template>` 节点。列表模板包含一个元素根，`ul` 和 `ol` 直接克隆 `li`。业务用 `IListView.ItemsSource` 提供数据，用 `IScrollableListView.ScrollRequest` 请求定位。

```html
<ul id="RewardList"></ul>
<template id="RewardItemTemplate">
  <li><span id="Txt_Number">0</span></li>
</template>
```

文档装配把 `RewardList` 与 `RewardItemTemplate` 交给 `ListElement`，用 `View.CreateForBinding` 创建各条目的绑定作用域。`ScrollListElement` 根据视口和作者布局复用横向或纵向的固定尺寸行。

列表、固定槽和动态子视图在 Document 更新批次内初始化；公开 Initialize 仍可立即读取几何，真实几何读取提前同步。空间属性在实际访问时创建，普通文字/图标不分配整套空间绑定。列表增删和重排保留相同模型对象的已有投影，包括重复模型对象的独立条目；删除和失败仍按所属投影释放。

ClassTransition以实际作者目标的命名动画组等待完成，取消、隐藏、卸载和重入不会发出旧成功事件。number进度位于实际节点上，不需要透明度信号节点。原生回调期间的值/命令发布延迟到安全边界；空间同步保持等值回传和嵌套写保护。

`ClassTransition` 配置的 `completionClassName` 在动画结束帧绘制前接管样式，并保留到过渡停止或再次播放。退出过渡可以在该类声明弹窗的隐藏或收起状态。

Editor作者校验检查局部选择器、显式能力、资源租约、真实native CSS语法、跨依赖动画冲突、无范围全局规则和已安装效果名称。效果参数由原生类型化快照及EffectDefinition逐节点验证。错误保留URI/行号，运行时效果错误包含目标节点与参数。

URP通过共享Surface.SubmitRendering提交一次，以保留提交完成及GPU退役通知。多个页面的层级、遮挡、模态和焦点由Stage与独立Document的presentation状态共同管理；关闭一个Scope只退役所属页面。不实现运行中替换后端。当前preview公共API已统一，旧构造重载、页面工厂分支和旧ShaderFloat桥已删除。
