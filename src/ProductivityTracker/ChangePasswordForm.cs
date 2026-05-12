using System.Drawing;
using System.Windows.Forms;

namespace ProductivityTracker;

internal sealed class ChangePasswordForm : Form
{
    private readonly TextBox _currentPasswordBox = new();
    private readonly TextBox _newPasswordBox = new();
    private readonly TextBox _confirmPasswordBox = new();
    private readonly string _locale;

    public string CurrentPassword => _currentPasswordBox.Text;
    public string NewPassword => _newPasswordBox.Text;

    public ChangePasswordForm(string productName, string locale = "en")
    {
        _locale = locale;
        Text = productName;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(420, 210);

        AddLabel(Loc.Get("current_password", locale) + ":", 16, 22);
        SetupPasswordBox(_currentPasswordBox, 160, 18);
        AddLabel(Loc.Get("new_password", locale) + ":", 16, 62);
        SetupPasswordBox(_newPasswordBox, 160, 58);
        AddLabel(Loc.Get("confirm_password", locale) + ":", 16, 102);
        SetupPasswordBox(_confirmPasswordBox, 160, 98);

        var okButton = new Button { Text = Loc.Get("save", locale), Left = 230, Width = 80, Top = 158 };
        okButton.Click += (_, _) => Save();
        var cancelButton = new Button { Text = Loc.Get("cancel", locale), DialogResult = DialogResult.Cancel, Left = 320, Width = 80, Top = 158 };

        AcceptButton = okButton;
        CancelButton = cancelButton;
        Controls.Add(okButton);
        Controls.Add(cancelButton);
    }

    private void AddLabel(string text, int left, int top)
    {
        Controls.Add(new Label { Text = text, AutoSize = true, Left = left, Top = top });
    }

    private void SetupPasswordBox(TextBox box, int left, int top)
    {
        box.Left = left;
        box.Top = top;
        box.Width = 240;
        box.UseSystemPasswordChar = true;
        Controls.Add(box);
    }

    private void Save()
    {
        if (_newPasswordBox.Text.Length < 5)
        {
            MessageBox.Show(Loc.Get("password_min_length", _locale), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_newPasswordBox.Text != _confirmPasswordBox.Text)
        {
            MessageBox.Show(Loc.Get("passwords_do_not_match", _locale), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
