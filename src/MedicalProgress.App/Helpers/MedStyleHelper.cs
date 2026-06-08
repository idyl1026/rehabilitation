using System.Drawing.Drawing2D;

namespace MedicalProgress.App.Helpers;

/// <summary>
/// 医疗深蓝风格 UI 辅助类（替代 AppleStyleHelper）
/// </summary>
public static class MedStyleHelper
{
    // ── 颜色常量 ────────────────────────────────────────────────
    public static readonly Color HeaderBg      = Color.FromArgb(0x1B, 0x3A, 0x6B);   // #1B3A6B 深蓝顶栏
    public static readonly Color HeaderText    = Color.White;
    public static readonly Color SidebarBg     = Color.FromArgb(0xEE, 0xF2, 0xF8);   // #EEF2F8 左侧浅蓝灰
    public static readonly Color ContentBg     = Color.FromArgb(0xF5, 0xF7, 0xFA);   // #F5F7FA 主背景
    public static readonly Color CardBg        = Color.White;
    public static readonly Color PrimaryBlue   = Color.FromArgb(0x16, 0x77, 0xFF);   // #1677FF 主蓝
    public static readonly Color LightBlue     = Color.FromArgb(0xE8, 0xF0, 0xFE);   // #E8F0FE 患者信息栏
    public static readonly Color SuccessGreen  = Color.FromArgb(0x52, 0xC4, 0x1A);   // #52C41A
    public static readonly Color WarningOrange = Color.FromArgb(0xFA, 0x8C, 0x16);   // #FA8C16
    public static readonly Color DangerRed     = Color.FromArgb(0xFF, 0x4D, 0x4F);   // #FF4D4F
    public static readonly Color TextDark      = Color.FromArgb(0x1D, 0x1D, 0x1F);   // #1D1D1F
    public static readonly Color TextGray      = Color.FromArgb(0x8C, 0x8C, 0x8C);   // #8C8C8C
    public static readonly Color BorderColor   = Color.FromArgb(0xE8, 0xEC, 0xF0);   // #E8ECF0
    public static readonly Color StatusBarBg   = Color.FromArgb(0x1B, 0x2B, 0x3B);   // #1B2B3B 底部状态栏

    // 兼容旧名（MainForm 仍引用）
    public static readonly Color BgGray        = ContentBg;
    public static readonly Color CardBorder    = BorderColor;
    public static readonly Color SelectionBg   = LightBlue;
    public static readonly Color DisabledGray  = TextGray;
    public static readonly Color HeaderDark    = HeaderBg;

    // ── 字体常量 ────────────────────────────────────────────────
    public static readonly Font FontTitle  = new("Microsoft YaHei UI", 12f, FontStyle.Bold);
    public static readonly Font FontBody   = new("Microsoft YaHei UI", 10f);
    public static readonly Font FontSmall  = new("Microsoft YaHei UI", 9f);
    public static readonly Font FontBold   = new("Microsoft YaHei UI", 10f, FontStyle.Bold);

    // ═══════════════════════════════════════════════════════════
    //  Form 初始化
    // ═══════════════════════════════════════════════════════════

    public static void ApplyWindowStyle(Form form)
    {
        form.BackColor = ContentBg;
        form.Font      = FontBody;
    }

    /// <summary>兼容旧代码的别名</summary>
    public static void ApplyFormStyle(Form form) => ApplyWindowStyle(form);

    // ═══════════════════════════════════════════════════════════
    //  顶栏 Header
    // ═══════════════════════════════════════════════════════════

    /// <summary>返回深蓝色顶栏 Panel（48px）</summary>
    public static Panel CreateHeader(string title, string subtitle = "")
    {
        var header = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 48,
            BackColor = HeaderBg
        };

        var lblTitle = new Label
        {
            Text      = "  " + title,
            Font      = FontTitle,
            ForeColor = HeaderText,
            Dock      = DockStyle.Left,
            Width     = 400,
            TextAlign = ContentAlignment.MiddleLeft
        };

        header.Controls.Add(lblTitle);

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            var lblSub = new Label
            {
                Text      = subtitle + "  ",
                Font      = FontSmall,
                ForeColor = Color.FromArgb(180, 210, 255),
                Dock      = DockStyle.Right,
                Width     = 500,
                TextAlign = ContentAlignment.MiddleRight
            };
            header.Controls.Add(lblSub);
        }

        return header;
    }

    // ═══════════════════════════════════════════════════════════
    //  底部状态栏
    // ═══════════════════════════════════════════════════════════

    /// <summary>返回深色底部状态栏（28px），items 用竖线分隔</summary>
    public static Panel CreateStatusBar(params string[] items)
    {
        var bar = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 28,
            BackColor = StatusBarBg
        };

        var flow = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            BackColor     = Color.Transparent,
            Padding       = new Padding(6, 4, 0, 0)
        };

        for (int i = 0; i < items.Length; i++)
        {
            flow.Controls.Add(new Label
            {
                Text      = items[i],
                Font      = FontSmall,
                ForeColor = Color.FromArgb(180, 200, 220),
                AutoSize  = true
            });
            if (i < items.Length - 1)
            {
                flow.Controls.Add(new Label
                {
                    Text      = "  |  ",
                    Font      = FontSmall,
                    ForeColor = Color.FromArgb(80, 100, 120),
                    AutoSize  = true
                });
            }
        }

        bar.Controls.Add(flow);
        return bar;
    }

    // ═══════════════════════════════════════════════════════════
    //  按钮工厂
    // ═══════════════════════════════════════════════════════════

    /// <summary>蓝色主按钮</summary>
    public static Button CreatePrimaryBtn(string text, int width = 120)
        => MakeButton(text, PrimaryBlue, Color.White, width);

    /// <summary>绿色成功按钮</summary>
    public static Button CreateSuccessBtn(string text, int width = 120)
        => MakeButton(text, SuccessGreen, Color.White, width);

    /// <summary>红色危险按钮</summary>
    public static Button CreateDangerBtn(string text, int width = 120)
        => MakeButton(text, DangerRed, Color.White, width);

    /// <summary>白底蓝边次要按钮</summary>
    public static Button CreateSecondaryBtn(string text, int width = 120)
    {
        var btn = MakeButton(text, Color.White, PrimaryBlue, width);
        btn.FlatAppearance.BorderColor = PrimaryBlue;
        btn.FlatAppearance.BorderSize  = 1;
        return btn;
    }

    // ── 兼容旧名 ─────────────────────────────────────────────

    public static Button CreatePrimaryButton(string text)                    => CreatePrimaryBtn(text, 120);
    public static Button CreatePrimaryButton(string text, int width)         => CreatePrimaryBtn(text, width);
    public static Button CreateSecondaryButton(string text)                  => CreateSecondaryBtn(text, 120);
    public static Button CreateSecondaryButton(string text, int width)       => CreateSecondaryBtn(text, width);
    public static Button CreateDangerButton(string text)                     => CreateDangerBtn(text, 120);
    public static Button CreateDangerButton(string text, int width)          => CreateDangerBtn(text, width);

    private static Button MakeButton(string text, Color bg, Color fg, int width)
    {
        var btn = new Button
        {
            Text      = text,
            Width     = width,
            Height    = 32,
            BackColor = bg,
            ForeColor = fg,
            FlatStyle = FlatStyle.Flat,
            Font      = FontBody,
            Cursor    = Cursors.Hand,
            Margin    = new Padding(0, 0, 6, 0)
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.SizeChanged += (_, _) => ApplyRoundedRegion(btn, 6);
        return btn;
    }

    // ═══════════════════════════════════════════════════════════
    //  卡片
    // ═══════════════════════════════════════════════════════════

    /// <summary>白色卡片 Panel，可选带标题行</summary>
    public static Panel CreateCard(string title = "")
    {
        var panel = new Panel
        {
            BackColor = CardBg,
            Padding   = new Padding(0)
        };
        panel.Paint += (_, e) => DrawCardBorder(panel, e.Graphics);

        if (!string.IsNullOrWhiteSpace(title))
        {
            var titleBar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 36,
                BackColor = Color.FromArgb(248, 250, 254),
                Padding   = new Padding(12, 0, 8, 0)
            };
            titleBar.Controls.Add(new Label
            {
                Text      = title,
                Font      = FontBold,
                ForeColor = TextDark,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            var sep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = BorderColor };
            titleBar.Controls.Add(sep);
            panel.Controls.Add(titleBar);
        }

        return panel;
    }

    private static void DrawCardBorder(Control ctrl, Graphics g)
    {
        using var pen = new Pen(BorderColor);
        g.DrawRectangle(pen, 0, 0, ctrl.Width - 1, ctrl.Height - 1);
    }

    // ═══════════════════════════════════════════════════════════
    //  搜索框
    // ═══════════════════════════════════════════════════════════

    public static TextBox CreateSearchBox(string placeholder)
    {
        return new TextBox
        {
            Font            = FontBody,
            PlaceholderText = placeholder,
            BackColor       = Color.White,
            BorderStyle     = BorderStyle.FixedSingle,
            ForeColor       = TextDark
        };
    }

    // 兼容旧名
    public static TextBox CreateTextField() => CreateSearchBox("");

    // ═══════════════════════════════════════════════════════════
    //  标签
    // ═══════════════════════════════════════════════════════════

    public static Label CreateLabel(string text, bool bold = false, float size = 10f)
    {
        return new Label
        {
            Text      = text,
            Font      = bold ? new Font("Microsoft YaHei UI", size, FontStyle.Bold) : new Font("Microsoft YaHei UI", size),
            ForeColor = TextDark,
            AutoSize  = true
        };
    }

    // 兼容旧名
    public static Label CreateTitle(string text) => CreateLabel(text, true, 12f);

    /// <summary>小色块 Badge Panel</summary>
    public static Panel CreateBadge(string text, Color bg)
    {
        var lbl = new Label
        {
            Text      = " " + text + " ",
            Font      = FontSmall,
            ForeColor = Color.White,
            AutoSize  = true,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = bg
        };
        var panel = new Panel
        {
            BackColor = bg,
            AutoSize  = true,
            Padding   = new Padding(4, 2, 4, 2)
        };
        panel.Controls.Add(lbl);
        return panel;
    }

    // ═══════════════════════════════════════════════════════════
    //  ListView / ListBox 样式
    // ═══════════════════════════════════════════════════════════

    public static void StyleListView(ListView lv)
    {
        lv.BorderStyle   = BorderStyle.None;
        lv.BackColor     = Color.White;
        lv.Font          = FontBody;
        lv.FullRowSelect = true;
        lv.GridLines     = false;
        lv.View          = View.Details;

        // 交替行颜色通过 OwnerDraw 实现（调用方自己挂 DrawItem 事件）
    }

    public static void StyleListBox(ListBox lb)
    {
        lb.BorderStyle    = BorderStyle.None;
        lb.BackColor      = Color.White;
        lb.Font           = FontBody;
        lb.IntegralHeight = false;
        lb.DrawMode       = DrawMode.OwnerDrawFixed;
    }

    // ═══════════════════════════════════════════════════════════
    //  工具方法
    // ═══════════════════════════════════════════════════════════

    public static void DrawRoundRect(Graphics g, Rectangle rect, int radius, Color borderColor)
    {
        using var pen  = new Pen(borderColor);
        using var path = BuildRoundRectPath(rect, radius);
        g.DrawPath(pen, path);
    }

    public static void FillRoundRect(Graphics g, Rectangle rect, int radius, Color fillColor)
    {
        using var brush = new SolidBrush(fillColor);
        using var path  = BuildRoundRectPath(rect, radius);
        g.FillPath(brush, path);
    }

    private static GraphicsPath BuildRoundRectPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
        path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
        path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseAllFigures();
        return path;
    }

    private static void ApplyRoundedRegion(Control ctrl, int radius)
    {
        if (ctrl.Width < 1 || ctrl.Height < 1) return;
        try
        {
            ctrl.Region = new Region(BuildRoundRectPath(new Rectangle(0, 0, ctrl.Width, ctrl.Height), radius));
        }
        catch { /* 忽略 Region 异常 */ }
    }
}
