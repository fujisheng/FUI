# FUI.Testing Headless

可复用纯托管测试宿主。Testing~/Headless源码由tools/dotnet/FUI.Testing.csproj编译为netstandard2.1，引用实际FUI.Core；无第三方新依赖或Unity程序集。Testing~不被Unity导入，本轮不提供UPM/asmdef或声明Unity发布验收。

## 使用

页面/行VM使用Pure字段和现有IListView.ItemsSource、ITextElement.Text、IClickElement.Click等语义接口，通过实际Generator生成绑定；不手动Synchronize。完整可执行例子见tools/dotnet/Tests/HeadlessListScenario.cs.txt与GeneratedListBindingTests.cs。

```csharp
var list = new HeadlessListElement("Rows", item =>
    new HeadlessView("Row", new HeadlessTextElement("Label"), new HeadlessClickElement("Button")));
var view = new HeadlessView("List", list);
using (var projection = Projection.Create(view, vm))
{
    projection.Activate();
    var text = list.Rows[0].View.GetElement<ITextElement>("Label").Text.Value;
    ((HeadlessClickElement)list.Rows[0].View.GetElement<IClickElement>("Button")).Input();
}
```

## API与所有权

HeadlessView(string name, params IElement[] elements)严格按完整名字查找，缺失/类型错/重复名报错。元素索引不可变；一个HeadlessElement只属于一个View。Destroy拥有并清理IDisposable元素，重复调用幂等；一项失败仍继续清理其他元素；保留单个原始错误或聚合多个清理错误。

HeadlessTextElement/HeadlessClickElement/HeadlessFloatElement/HeadlessBoolElement实现现有语义接口。float/bool的Input只对有效变值发送一次ValueChanged；程序写Value不发用户命令。Input服从Owner的Visible/Interactable与释放状态；直接Command.Invoke是底层命令调用，不模拟设备门禁。

HeadlessListElement(string name, Func<ObservableObject, HeadlessView> createRow)实现IListView。工厂为新增行返回独立且未销毁的View；HeadlessListRow拥有真实Projection，Rows只读暴露Item/View以观察生成绑定结果。排序按对象身份保留行；重复对象按出现次数保留独立行。移除/替换/Destroy释放行；null集合清空行，null项明确失败。

RowsChanged在每次成功提交刷新后触发，包括内容未变的集合交付与重新显示刷新，可检测重复交付。列表元素只观察Items属性，不直接订阅集合；集合事件由真实生成代码建立与解绑。不能把手动调用IListView事件方法当成生成绑定验收。

新增行创建/激活失败时释放同一刷新事务内新建的View，保留已有行；失败行自身清理与回滚中其他行清理的错误和原始绑定错误一并保留。每条移除/回滚/销毁路径均尝试释放全部行，先清理容器/监听终态再报告错误。源集合可能已改变，宿主需修正数据或关闭页面，后续正常集合事件可恢复。

## 生命周期与范围

Visible=false暂停已有行Projection的属性/命令绑定；根投影仍绑定时，集合结构变化继续创建/移除行，并不冻结完整列表快照。隐藏期间新建行先持有未激活Projection，文本初值/缺失目标等绑定验证延迟到Visible=true的Activate；重显失败将页面恢复隐藏，失败行仍由Rows拥有，修正数据或关闭页面可释放。缓存Close会同时解绑根集合通知，与仅隐藏不同；重开通过实际Navigator同步并激活当前行。Interactable传播到行，阻止模拟输入穿透。此规则只属于Headless，不宣称等同uGUI行生命周期/复用策略。

单线程使用；不模拟GPU、布局、字体、像素、命中测试、焦点、虚拟化或平台输入；没有大列表性能保证。既有uGUI/Mixed/游戏行为未改。Mixed setter的Unity ILPP未在.NET实现，后端与游戏验收仍需后续Unity门禁。本阶段只保留隔离产物，未推广。

## 离线验证

```sh
dotnet restore tools/dotnet/FUI.Portable.Tests.csproj --configfile tools/dotnet/NuGet.Config
dotnet test tools/dotnet/FUI.Portable.Tests.csproj --no-restore -m:1 -nodeReuse:false -p:UseSharedCompilation=false
dotnet test Packages/com.fujisheng.fui/FUI.Generators~/Tests/FUI.Generators.Tests.csproj --no-restore -m:1 -nodeReuse:false -p:UseSharedCompilation=false
```

只使用已有缓存；缺依赖停止，不下载。VSTest需要本机socket，沙箱拒绝属于执行环境错误，不是NUnit行为RED。

## 资源与可见性意图

HeadlessVisibilityElement提供IVisibilityElement.Visible，HeadlessDynamicElement提供IDynamicViewElement.Source/ViewModel，只记录共享UI的节点可见性、资源键与真实VM引用；不加载资源、创建子Projection、布局或Spine，且不拥有/释放VM。uGUI以原有BindableProperty直接实现这些接口，实际后端需另行验收。

### 显示与嵌套投影

HeadlessGraphicElement记录颜色/图像键/填充及动画动作意图，不加载图像或播放动画。HeadlessModelElement使用真实生成Projection与独立子view，不拥有借用VM。HeadlessListElement支持节点可见性与中性滚动位置；所属页面隐藏仍遮盖行，不通过列表Visible绕过页面状态。真实Battle场景子页与坐标/设备/计时实现需各自后端验收。


HeadlessGraphicElement另实现ITextAlignmentElement/IFormattedTextElement：记录原对齐值和格式/参数/provider请求，并按现有input可见门禁输入/销毁；不执行翻译、字体、排版或绘制。真实uGUI TextElement以原Alignment/FormattedText属性适配，原FormattedText身份与本地化执行路径保留。Common实际三页共享测试不替代该后端实机/ILPP验证。


HeadlessGraphicElement另提供raw LocalPositionValue/LocalScaleValue与DynamicSource/ViewModel；不转换坐标或渲染/计时/加载特效。HeadlessPrototypeElement使用真实Core ProjectionTemplate/ViewInstance与生成绑定，拥有新独立View及释放回调；拒绝已初始化/已有行owner/父自身/已销毁View，外部工厂中同步关闭后取得的新资源也独立清理。子自身Visible/Interactable请求AND父层级，隐藏/禁交互父级不能被Activate绕过；关闭后旧回调与资源的行为由真实共享Presenter/生命周期负责。


HeadlessDynamicProjectionElement以source工厂提供独立raw HeadlessView，使用真实Core生成工厂/绑定/Presenter/UpdateVM/Enable/DestroyProjection；拥有释放callback，父可见/交互约束沿用模板ChildView。Source候选创建成功再交接，失败恢复原Source、独立释放候选，禁止借用已初始化/行owner/父自身；Source工厂同步销毁按终态取消。VM同类型重绑不重新OnOpen，空VM停投影保留Source视觉；同步打开回调改变VM按每Entry串行同步最新版本，防递归Enable漏订阅。严格测试后端会抛工厂失败，不等同Unity资源失败日志；不加载资源、布局、动画或证明Native视觉/设备表现。


C1a游戏测试在Client/tools/dotnet中复用实际Core/List/Projection/Navigator与生产CardItem/PropItem绑定。游戏层ICardStarElement/IQualityEffectElement测试后端只存已有值/动作，未扩展FUI Core渲染架构、不模拟奖励或实际星绘制/效果；模型/奖励/广告/存储/计时仅原始边界，默认uGUI/Unity/MixedILPP/视觉仍留后续。


### C1b Inventory/UserInfo共享测试

GameInventoryUser*测试运行实际两页VM/Presenter/生成绑定、三真实子Projection及PropItem/Status/ItemDetail；共享PlayerLevelRewardBuilder保持原类体。读取/事件/导航为原始宿主事实，不执行等级经验/库存经济/发奖/保存/登录；Native/默认宿主仅声明壳C#，Unity/MixedILPP/视觉暂停。HeadlessModelElement现通过已有IVisibilityElement控制实际嵌套Projection与输入；原Runtime接口不变。测试矩阵/精确原源结果见FUI/docs/portable-game-inventory-user-20261013.md及本批结果收据；原库存选中项移除到空占位的NullReference已记录未改。


### C1c 周奖励/等级提升共享测试

GameWeekLevel*运行实际RewardWeek/PlayerLevelUp VM/Presenter、两真实奖励行、原Config/Builder与既有RewardGet/Prop子链。真实生成通知、List/Model Projection、Nav、回调生命周期及重入边界已测；读取/eligibility/领奖返回值仅原始宿主事实，不执行真实奖励/经验/时间/保存算法。默认适配仅外部声明壳C#检查，Unity/MixedILPP/设备视觉暂停。目标Game621（旧541+80）/Portable111/Generator26以FUI/docs/portable-game-week-level-20261013.md及本批最终实际TRX为准。Inventory旧删除选中项遇空占位NullReference保留且单独披露，不能因测试通过称已修复。
