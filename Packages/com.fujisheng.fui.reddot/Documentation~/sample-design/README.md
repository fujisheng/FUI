# 原创 RTS Sample 制作与复现

## 功能先行

先阅读 ../sample-coverage.md。每个页面围绕独立的游戏动作组织，不把整个样例做成一屏测试开关。页面资源位于 Sample/Resources/UI/Prefabs；每页一个FUI Route，追踪终端使用Overlay/Transient/None，不隐藏当前页且关闭即释放。

## 制作顺序与来源

1. 从公开端口、Core实现、UGUI生命周期及测试建立R01–R20矩阵。
2. 八个View分别用Codex内置imagegen生成design-master.png；各目录prompt.md保存完整提示词。图片是参考稿，绝不引用到Prefab。
3. 对照整页设计复用匹配的原创ArtV2面板、按钮、物资、地图、通讯资源；重新生成ArtV3单位插画、三个单位头像和推演图标。新素材提示词及来源见asset-generation-log.json。
4. 原始资源在独立测试工程Temp/WebToUgui/<View>/assets_raw，后处理仅改变尺寸与保留透明通道；不使用代码绘制美术。
5. <View>.html是可复现的正式布局源；必须复制到Temp/WebToUgui后，经固定extract-visual-ui.mjs提取。不手改visual-ui.json。
6. 每个Prefab均经ui.web_to_ugui_prefab的dry_run预检与正式生成；可变文案由真实UGUI Text渲染。

ArtV2/ArtV3表示生成批次，不是两套混合皮肤。没有引入商业游戏截图、角色、标志或未注明来源的第三方美术；不能把AI生成说明当作绝对的法律担保。

## 原型与运行态的差异

参考图里的长文案、数量与状态是布局示意；最终页面只呈现实际游戏事实。机器头像与标题可替换，但Key、规则与界面不能相互写入。实际Unity截图独立保存为验收证据，不用Web截图冒充。

Runtime断开时Element清除旧视觉。分队调动是实际的隐藏、换父级、恢复生命周期；取消Scope编制经过真实组件销毁通知，不手写红点回填。

## 再生成

先把本目录HTML复制到独立工程Temp/WebToUgui/<View>/<View>.html。运行：

```sh
node <fui-cli>/Skills/fui-cli/scripts/extract-visual-ui/extract-visual-ui.mjs \
  --input Temp/WebToUgui/<View>/<View>.html --view <View> \
  --json Temp/WebToUgui/<View>/<View>.visual-ui.json \
  --png Temp/WebToUgui/<View>/<View>.web.png
```

通过CLI调用ui.web_to_ugui_prefab，先dry_run:true，再dry_run:false。Prefab路径选择已导入Sample的Resources/UI/Prefabs目录。新版提取器默认输出FUI-CLI目录，因此必须显式指定--json/--png以保持当前技能的Temp工作区约定。

## 计量边界

推演结果直接测量Flush，统计采集与文字格式化均在区间外。相同Rule重复失效与1000个不同Rule失效分别演示。无变化批次不通知、不递增状态BatchVersion。诊断快照会分配，不能放入零GC计量区间。

故障自检由Core隔离；Source.Start失败向调用者报告，Source.Stop重入发布被拒绝并记录SourceFailure。保留最近32条诊断，不用清空Console掩盖故障。
