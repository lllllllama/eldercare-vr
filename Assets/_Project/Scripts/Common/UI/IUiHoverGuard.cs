// 世界空间面板的“指针悬停”状态出口：玩法交互（抓镖/搭弦）在指针悬停 UI 时
// 应当让位给按钮点击，避免扳机双职责互相打架。
public interface IUiHoverGuard
{
    bool IsPointerOver { get; }
}
