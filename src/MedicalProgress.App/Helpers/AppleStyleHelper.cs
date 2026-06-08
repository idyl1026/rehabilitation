namespace MedicalProgress.App.Helpers;

/// <summary>
/// 苹果 Mac 风格 UI 辅助类
/// </summary>
public static class AppleStyleHelper
{
    // ── 颜色 ────────────────────────────────────────────────────
    public static readonly Color BgGray       = Color.FromArgb(245, 245, 247);   // #F5F5F7
    public static readonly Color White        = Color.White;                      // #FFFFFF
    public static readonly Color CardBorder   = Color.FromArgb(224, 224, 224);   // #E0E0E0
    public static readonly Color PrimaryBlue  = Color.FromArgb(0,   113, 227);   // #0071E3
    public static readonly Color DangerRed    = Color.FromArgb(255, 59,  48);    // #FF3B30
    public static readonly Color InputBorder  = Color.FromArgb(199, 199, 204);   // #C7C7CC
    public static readonly Color DisabledGray = Color.FromArgb(142, 142, 147);   // #8E8E93
    public static readonly Color SelectionBg  = Color.FromArgb(232, 240, 254);   // #E8F0FE
    public static readonly Color HeaderDark   = Color.FromArgb(29,  29,  31);    // #1D1D1F

    // ── 字体 ────────────────────────────────────────────────────
    public static readonly Font FontTitle  = new("Segoe UI", 13f, FontStyle.Bold);
    public static readonly Font FontBody   = new("Segoe UI", 10f);
    public static readonly Font FontSmall  = new("Segoe UI", 9f);
    public static readonly Font FontBold   = new("Segoe UI", 10f, FontStyle.Bold);

    // ── Form ─────────────────────────────────────────────────────

    /// <summary>设置窗体背景色和字体为苹果风格</summary>
    public static void ApplyFormStyle(Form form)
    {
        form.BackColor = BgGray;
        form.Font      = FontBody;
    }

    // ── 按钮 ─────────────────────────────────────────────────────

    /// <summary>蓝色主按钮</summary>
    public static Button CreatePrimaryButton(string text)
    {
        var btn = MakeButton(text, PrimaryBlue, Color.White);
        return btn;
    }

    /// <summary>次要按钮（白底蓝字）</summary>
    public static Button CreateSecondaryButton(string text)
    {
        var btn = MakeButton(text, Color.White, PrimaryBlue);
        btn.FlatAppearance.BorderColor = PrimaryBlue;
        btn.FlatAppearance.BorderSize  = 1;
        return btn;
    }

    /// <summary>危险按钮（红色）</summary>
    public static Button CreateDangerButton(string text)
    {
        return MakeButton(text, DangerRed, Color.White);
    }

    private static Button MakeButton(string text, Color backColor, Color foreColor)
    {
        var btn = new Button
        {
            Text      = text,
            AutoSize  = false,
            Height    = 32,
            BackColor = backColor,
            ForeColor = foreColor,
            FlatStyle = FlatStyle.Flat,
            Font      = FontBody,
            Cursor    = Cursors.Hand,
            Margin    = new Padding(0, 0, 8, 0)
        };
        btn.FlatAppearance.BorderSize = 0;
        // 模拟圆角（WinForms 无原生圆角，用 Region 近似）
        btn.SizeChanged += (_, _) => ApplyRoundedRegion(btn, 6);
        return btn;
    }

    // ── 卡片 ─────────────────────────────────────────────────────

    /// <summary>白色卡片 Panel（圆角+细边框）</summary>
    public static Panel CreateCard()
    {
        var panel = new Panel
        {
            BackColor = White,
            Padding   = new Padding(12)
        };
        panel.Paint += (_, e) => DrawCardBorder(panel, e.Graphics);
        return panel;
    }

    private static void DrawCardBorder(Control ctrl, Graphics g)
    {
        var rect = new Rectangle(0, 0, ctrl.Width - 1, ctrl.Height - 1);
        using var pen = new Pen(CardBorder);
        g.DrawRectangle(pen, rect);
    }

    // ── 输入框 ───────────────────────────────────────────────────

    /// <summary>样式化输入框</summary>
    public static TextBox CreateTextField()
    {
        return new TextBox
        {
            Font        = FontBody,
            BackColor   = White,
            BorderStyle = BorderStyle.FixedSingle,
            ForeColor   = Color.FromArgb(29, 29, 31)
        };
    }

    // ── 列表框 ───────────────────────────────────────────────────

    /// <summary>样式化 ListBox</summary>
    public static ListBox CreateListBox()
    {
        return new ListBox
        {
            Font            = FontBody,
            BackColor       = White,
            BorderStyle     = BorderStyle.FixedSingle,
            DrawMode        = DrawMode.OwnerDrawFixed,
            IntegralHeight  = false,
            ItemHeight      = 28
        };
    }

    // ── 标签 ─────────────────────────────────────────────────────

    /// <summary>标题 Label（13pt Bold）</summary>
    public static Label CreateTitle(string text)
    {
        return new Label
        {
            Text      = text,
            Font      = FontTitle,
            ForeColor = HeaderDark,
            AutoSize  = true
        };
    }

    /// <summary>普通 Label</summary>
    public static Label CreateLabel(string text)
    {
        return new Label
        {
            Text      = text,
            Font      = FontBody,
            ForeColor = Color.FromArgb(60, 60, 67),
            AutoSize  = true
        };
    }

    // ── 工具方法 ─────────────────────────────────────────────────

    private static void ApplyRoundedRegion(Control ctrl, int radius)
    {
        if (ctrl.Width < 1 || ctrl.Height < 1) return;
        try
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
            path.AddArc(ctrl.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
            path.AddArc(ctrl.Width - radius * 2, ctrl.Height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(0, ctrl.Height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseAllFigures();
            ctrl.Region = new Region(path);
        }
        catch { /* 忽略 Region 异常 */ }
    }

    /// <summary>
    /// 快捷创建指定宽度的主按钮
    /// </summary>
    public static Button CreatePrimaryButton(string text, int width)
    {
        var btn = CreatePrimaryButton(text);
        btn.Width = width;
        return btn;
    }

    /// <summary>
    /// 快捷创建指定宽度的次要按钮
    /// </summary>
    public static Button CreateSecondaryButton(string text, int width)
    {
        var btn = CreateSecondaryButton(text);
        btn.Width = width;
        return btn;
    }

    /// <summary>
    /// 快捷创建指定宽度的危险按钮
    /// </summary>
    public static Button CreateDangerButton(string text, int width)
    {
        var btn = CreateDangerButton(text);
        btn.Width = width;
        return btn;
    }
}
