namespace MedicalProgress.App.Forms;

public class DuplicateReviewForm : Form
{
    private readonly RichTextBox _txtOriginal;
    private readonly TextBox _txtReport;
    private readonly TextBox _txtRevised;

    public string RevisedContent => _txtRevised.Text;
    public bool AcceptedRevision { get; private set; }

    public DuplicateReviewForm(string originalContent, string duplicateReport, string revisedContent, IEnumerable<string>? duplicateTexts = null)
    {
        Text = "重复内容复核";
        Size = new Size(1180, 760);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(245, 247, 250);
        MinimizeBox = false;

        var top = new Label
        {
            Text = "黄色标记为可能重复的内容。请核对中间的问题列表，右侧为系统改写稿；确认后再采用。",
            Dock = DockStyle.Top,
            Height = 42,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold),
            BackColor = Color.White
        };

        var splitOuter = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 390
        };

        var splitRight = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 330
        };

        _txtOriginal = CreateRichTextBox(originalContent);
        HighlightDuplicates(_txtOriginal, duplicateTexts ?? Enumerable.Empty<string>());
        _txtReport = CreateTextBox(duplicateReport);
        _txtRevised = CreateTextBox(revisedContent);

        splitOuter.Panel1.Controls.Add(Wrap("当前病程（黄色为重复点）", _txtOriginal));
        splitRight.Panel1.Controls.Add(Wrap("重复问题", _txtReport));
        splitRight.Panel2.Controls.Add(Wrap("改写稿", _txtRevised));
        splitOuter.Panel2.Controls.Add(splitRight);

        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10),
            BackColor = Color.White
        };

        var btnAccept = CreateButton("采用改写稿", Color.FromArgb(40, 167, 69));
        btnAccept.Click += (_, _) =>
        {
            AcceptedRevision = true;
            DialogResult = DialogResult.OK;
        };

        var btnCancel = CreateButton("暂不修改", Color.FromArgb(108, 117, 125));
        btnCancel.Click += (_, _) =>
        {
            AcceptedRevision = false;
            DialogResult = DialogResult.Cancel;
        };

        bottom.Controls.Add(btnAccept);
        bottom.Controls.Add(btnCancel);

        Controls.Add(splitOuter);
        Controls.Add(bottom);
        Controls.Add(top);
    }

    private static RichTextBox CreateRichTextBox(string text)
    {
        return new RichTextBox
        {
            Text = text,
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = RichTextBoxScrollBars.Both,
            WordWrap = true,
            Font = new Font("Microsoft YaHei UI", 9),
            BorderStyle = BorderStyle.FixedSingle,
            ReadOnly = true
        };
    }

    private static TextBox CreateTextBox(string text)
    {
        return new TextBox
        {
            Text = text,
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = true,
            Font = new Font("Microsoft YaHei UI", 9),
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    private static void HighlightDuplicates(RichTextBox box, IEnumerable<string> duplicateTexts)
    {
        foreach (var duplicateText in duplicateTexts.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct())
        {
            var start = 0;
            while (start < box.TextLength)
            {
                var index = box.Text.IndexOf(duplicateText, start, StringComparison.Ordinal);
                if (index < 0)
                    break;

                box.Select(index, duplicateText.Length);
                box.SelectionBackColor = Color.Yellow;
                start = index + duplicateText.Length;
            }
        }

        box.Select(0, 0);
    }

    private static Control Wrap(string title, Control content)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10) };
        var label = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold)
        };
        panel.Controls.Add(content);
        panel.Controls.Add(label);
        return panel;
    }

    private static Button CreateButton(string text, Color color)
    {
        var button = new Button
        {
            Text = text,
            Width = 130,
            Height = 34,
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei UI", 9),
            Margin = new Padding(8, 0, 0, 0)
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }
}
