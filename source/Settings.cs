using System;
using System.IO;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Localization;

namespace YesRelic2;

public sealed class Preferences
{
    public bool StartWithRelic { get; set; } = true;
    public bool AddToCommonPool { get; set; }
    public bool LimitedUses { get; set; }
    public int UseCount { get; set; } = 3;
}

internal static class Settings
{
    internal static Preferences Current = new Preferences();
    static string ConfigPath => ProjectSettings.GlobalizePath("user://mods/YesRelic2/settings.json");
    static PanelContainer panel;
    static CheckButton start, pool, limited;
    static SpinBox count;
    static Label status, title, countLabel, description;
    static Button save, close;
    static string displayedLanguage;
    static int statusKind;
    static string saveError;
    static bool keyHeld;

    internal static void Load()
    {
        try
        {
            if (File.Exists(ConfigPath)) Current = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(ConfigPath)) ?? new Preferences();
            Current.UseCount = Math.Clamp(Current.UseCount, 1, 999);
        }
        catch (Exception e) { GD.PrintErr("[YesRelic2] Cannot read settings; using defaults: " + e.Message); }
    }

    internal static void AttachUi()
    {
        var tree = Engine.GetMainLoop() as SceneTree;
        if (tree == null) return;
        var layer = new CanvasLayer { Layer = 110, Name = "YesRelic2Settings" };
        tree.Root.AddChild(layer);
        panel = new PanelContainer { Visible = false, CustomMinimumSize = new Vector2(490, 0) };
        var style = new StyleBoxFlat { BgColor = new Color("201d28"), BorderColor = new Color("dbb865"),
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12, CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12,
            ContentMarginLeft = 24, ContentMarginRight = 24, ContentMarginTop = 20, ContentMarginBottom = 20 };
        panel.AddThemeStyleboxOverride("panel", style);
        layer.AddChild(panel);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 12);
        panel.AddChild(box);
        title = new Label { Text = "Yes!  ·  " + Entry.L("单人版设置", "Single-player settings") };
        title.AddThemeFontSizeOverride("font_size", 28);
        title.AddThemeColorOverride("font_color", new Color("f5d889"));
        box.AddChild(title);
        start = new CheckButton { Text = Entry.L("新局开局额外获得遗物", "Start new runs with the relic") }; box.AddChild(start);
        pool = new CheckButton { Text = Entry.L("加入共享普通遗物池", "Include in the common relic pool") }; box.AddChild(pool);
        limited = new CheckButton { Text = Entry.L("限制使用次数", "Limit uses") }; box.AddChild(limited);
        var row = new HBoxContainer(); box.AddChild(row);
        countLabel = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; row.AddChild(countLabel);
        count = new SpinBox { MinValue = 1, MaxValue = 999, Step = 1, CustomMinimumSize = new Vector2(120, 0) }; row.AddChild(count);
        description = new Label { Text = Entry.L("取消次数限制即为无限次。\n次数设置用于之后获得的遗物；已有遗物的次数保持不变。\n拿第2张牌才扣次数，同场战斗只扣一次。\n仅战斗卡牌奖励生效，联机不生效。", "Unlimited when the limit is off.\nUse settings apply to newly obtained relics.\nThe second card costs one use per combat.\nCombat card rewards only; disabled in multiplayer."), AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(450, 0) }; box.AddChild(description);
        status = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart }; box.AddChild(status);
        save = new Button { Text = Entry.L("保存设置", "Save settings") }; box.AddChild(save);
        save.Pressed += Save;
        close = new Button { Text = Entry.L("关闭（F9）", "Close (F9)") }; box.AddChild(close);
        close.Pressed += () => panel.Hide();
        var timer = new Godot.Timer { WaitTime = 0.05, ProcessMode = Node.ProcessModeEnum.Always };
        layer.AddChild(timer);
        timer.Timeout += () => {
            if (displayedLanguage != (LocManager.Instance?.Language ?? "eng")) RefreshLanguage();
            bool pressed = Input.IsPhysicalKeyPressed(Key.F9);
            if (pressed && !keyHeld)
            {
                if (panel.Visible) panel.Hide();
                else
                {
                    start.ButtonPressed = Current.StartWithRelic;
                    pool.ButtonPressed = Current.AddToCommonPool;
                    limited.ButtonPressed = Current.LimitedUses;
                    count.Value = Current.UseCount;
                    statusKind = 0; RefreshLanguage();
                    panel.Position = (tree.Root.GetVisibleRect().Size - new Vector2(520, 480)) / 2;
                    panel.Show();
                }
            }
            keyHeld = pressed;
        };
        RefreshLanguage();
        timer.Start();
    }

    // The mod can initialize before LocManager. Refresh when it becomes ready,
    // and whenever the game changes locale, without resetting unsaved controls.
    static void RefreshLanguage()
    {
        if (panel == null) return;
        displayedLanguage = LocManager.Instance?.Language ?? "eng";
        title.Text = "Yes!  ·  " + Entry.L("单人版设置", "Single-player settings");
        start.Text = Entry.L("新局开局额外获得遗物", "Start new runs with the relic");
        pool.Text = Entry.L("加入共享普通遗物池", "Include in the common relic pool");
        limited.Text = Entry.L("限制使用次数", "Limit uses");
        countLabel.Text = Entry.L("初始次数", "Starting uses");
        description.Text = Entry.L(
            "取消次数限制即为无限次。\n次数设置用于之后获得的遗物；已有遗物的次数保持不变。\n拿第2张牌才扣次数，同场战斗只扣一次。\n仅战斗卡牌奖励生效，联机不生效。",
            "Unlimited when the limit is off.\nUse settings apply to newly obtained relics; existing relics keep their remaining uses.\nTaking a second card spends one use per combat.\nCombat card rewards only; disabled in multiplayer.");
        save.Text = Entry.L("保存设置", "Save settings");
        close.Text = Entry.L("关闭（F9）", "Close (F9)");
        status.Text = statusKind == 1
            ? Entry.L("已保存。开局选项在新局生效。", "Saved. Starting relic applies to new runs.")
            : statusKind == 2 ? Entry.L("保存失败：", "Save failed: ") + saveError
            : Entry.Compatible ? Entry.L("支持游戏版本：0.111.0", "Supported game: 0.111.0")
            : Entry.L("游戏版本未验证，功能已停用。", "Unverified game build: gameplay disabled.");
    }

    static void Save()
    {
        var next = new Preferences { StartWithRelic = start.ButtonPressed, AddToCommonPool = pool.ButtonPressed,
            LimitedUses = limited.ButtonPressed, UseCount = (int)count.Value };
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            File.WriteAllText(ConfigPath + ".tmp", JsonSerializer.Serialize(next, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(ConfigPath + ".tmp", ConfigPath, true);
            Current = next;
            statusKind = 1; RefreshLanguage();
        }
        catch (Exception e) { statusKind = 2; saveError = e.Message; RefreshLanguage(); }
    }
}

