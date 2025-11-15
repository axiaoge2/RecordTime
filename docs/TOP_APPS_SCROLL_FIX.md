# Avalonia 仪表盘 Top 10 应用显示问题复盘

4 号卡片（TOP 10 应用列表）曾出现“只显示 9 条且第 9 条被截断/部分消失”的问题，最终通过一次布局重构解决。这里记录整个问题、原因、修复及验证步骤，方便后续查阅。

## 现象

- 仪表盘页面能显示 TopApps.Count = 10，但卡片中只渲染 9 条，且第 9 条只有一半。
- 鼠标滚轮往下滚不会出现第 10 条；外层 ScrollViewer 的滚动也被“吃掉”。
- 某次修改后还出现过 Top 10 区域完全空白的状况。

## 根因分析

1. **嵌套 ScrollViewer 限高**  
   原实现是「外层 ScrollViewer（整个仪表盘） + 卡片内部 ItemsControl（隐含 ScrollViewer）」，Avalonia 的内部滚动区域会截断高度并吞掉滚动消息，导致只能看到 9 条。

2. **后续改动副作用**  
   为了解决滚动问题，曾加入 StackPanel/ScrollViewer/模板覆盖，但其中一次把 `ListBoxItem` 模板改成空的 `<ControlTemplate><ContentPresenter/></ControlTemplate>`，又把 `ItemsControl` 改成 `ListBox`，绑定数据没显示出来。

## 解决方案

文件：`src/RecordTime.Avalonia/Views/MainWindow.axaml`

1. **移除 ItemsControl 自带 ScrollViewer**  
   用 `ListBox` 承载 `TopApps`，高度固定为 420px，让 ListBox 内部滚动即可。

2. **保持 ListBoxItem 的默认模板**  
   只保留 Padding/Margin/Background 等样式，不再覆盖 `Template`，避免内容无法渲染。

3. **继续使用自定义 ItemTemplate**  
   `RankText / AppName / DurationText / SessionCountText` 的布局完全沿用之前的 Grid 结构，用户视觉不变。

最终代码片段（精简）：

```xml
<ListBox ItemsSource="{Binding TopApps}"
         Height="420"
         BorderThickness="0"
         Background="Transparent"
         ScrollViewer.HorizontalScrollBarVisibility="Disabled"
         ScrollViewer.VerticalScrollBarVisibility="Auto">
    <ListBox.Styles>
        <Style Selector="ListBoxItem">
            <Setter Property="Padding" Value="0"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Background" Value="Transparent"/>
            <Setter Property="Margin" Value="0,2"/>
            <Setter Property="Focusable" Value="False"/>
        </Style>
    </ListBox.Styles>
    <ListBox.ItemTemplate>
        <!-- 保持原来的 Border + Grid 布局 -->
    </ListBox.ItemTemplate>
</ListBox>
```

## 验证步骤

1. `dotnet build src/RecordTime.Avalonia/RecordTime.Avalonia.csproj`
2. `dotnet run --project src/RecordTime.Avalonia/RecordTime.Avalonia.csproj`
3. 打开仪表盘，滚轮悬停在 Top 10 卡片内部上下滚动，应能看到完整的 10 条记录；同时卡片右侧会出现细滚动条，外层页面滚动不再受影响。

## 后续建议

- UI 做嵌套滚动时，尽量只保留一个 ScrollViewer，或固定内层高度 + ListBox/ListView。
- 添加 UI 自动化测试前，可以考虑写一个简单的 Avalonia UITest/截图测试，断言 TopApps.List.Count 可见项 == TopApps.Count。
