using System;
using Avalonia.Controls;

namespace TerminalEmbellish.templates.Components;

public static class ComponentLoader
{
    public static Control? Load(string componentName)
    {
        Console.WriteLine($"嘎！ComponentLoader.Load 被调用，componentName={componentName}");
        var type = Type.GetType($"TerminalEmbellish.page_ui.Dialogs.{componentName}")
                ?? Type.GetType($"TerminalEmbellish.page_ui.dialogs.{componentName}");
        Console.WriteLine($"嘎！解析到的 type = {(type != null ? type.FullName : "null")}");

        if (type == null) return new TextBlock { Text = $"组件 '{componentName}' 未找到嘎！" };
        return Activator.CreateInstance(type) as Control;
    }
}

