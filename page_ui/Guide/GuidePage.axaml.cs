#nullable enable

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using TerminalEmbellish.templates.@private.Behaviors;

namespace TerminalEmbellish.page_ui.Guide;

public partial class GuidePage : UserControl
{
    public event Action? OnGuideFinished;

    public GuidePage()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            // 拖拽顶部区域移动窗口嘎
            var dragArea = this.FindControl<Border>("DragArea");
            if (dragArea != null)
            {
                DragHelper.EnableDrag(dragArea);
            }
        };
    }

    // 引导完成后调用嘎
    public void FinishGuide()
    {
        OnGuideFinished?.Invoke();
    }
}
