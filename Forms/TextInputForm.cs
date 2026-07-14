namespace RasterEditor.WinFormsDemo.Forms;

public class TextInputForm : Form
{
    public string InputValue => _textBox.Text;
    private TextBox _textBox = new();

    public TextInputForm(string prompt, string defaultValue)
    {
        Text = "Ввод текста";
        Width = 440;
        Height = 160;
        StartPosition = FormStartPosition.CenterParent;

        var label = new Label { Text = prompt, AutoSize = true, Location = new Point(10, 10), MaximumSize = new Size(400, 0) };
        _textBox.Text = defaultValue;
        _textBox.Location = new Point(10, 36);
        _textBox.Width = 400;

        var buttonOk = new Button { Text = "Готово", Location = new Point(240, 72), Width = 90, DialogResult = DialogResult.OK };
        var buttonCancel = new Button { Text = "Отмена", Location = new Point(340, 72), Width = 90, DialogResult = DialogResult.Cancel };

        Controls.Add(label);
        Controls.Add(_textBox);
        Controls.Add(buttonOk);
        Controls.Add(buttonCancel);

        AcceptButton = buttonOk;
        CancelButton = buttonCancel;
    }
}
