using System.Drawing;
using System.Windows.Forms;

namespace ProductivityTracker;

internal sealed class FirstRunSetupForm : Form
{
    private readonly TextBox _passwordBox = new();
    private readonly TextBox _confirmPasswordBox = new();
    private readonly string _locale;

    public string Password => _passwordBox.Text;

    public FirstRunSetupForm(string productName, string locale = "en")
    {
        _locale = locale;
        Text = productName;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(420, 210);

        var titleLabel = new Label
        {
            Text = Loc.Get("create_password", locale),
            AutoSize = true,
            Left = 16,
            Top = 16,
            Font = new Font(Font, FontStyle.Bold)
        };

        var passwordLabel = new Label { Text = Loc.Get("password_label", locale) + ":", AutoSize = true, Left = 16, Top = 58 };
        _passwordBox.Left = 150;
        _passwordBox.Top = 54;
        _passwordBox.Width = 240;
        _passwordBox.UseSystemPasswordChar = true;

        var confirmLabel = new Label { Text = Loc.Get("confirm_password_label", locale) + ":", AutoSize = true, Left = 16, Top = 94 };
        _confirmPasswordBox.Left = 150;
        _confirmPasswordBox.Top = 90;
        _confirmPasswordBox.Width = 240;
        _confirmPasswordBox.UseSystemPasswordChar = true;

        var noteLabel = new Label
        {
            Text = Loc.Get("password_note", locale),
            AutoSize = false,
            Left = 16,
            Top = 130,
            Width = 374,
            Height = 34
        };

        var okButton = new Button
        {
            Text = Loc.Get("save", locale),
            Left = 220,
            Width = 80,
            Top = 170
        };
        okButton.Click += (_, _) => Save();

        var cancelButton = new Button
        {
            Text = Loc.Get("exit", locale),
            DialogResult = DialogResult.Cancel,
            Left = 310,
            Width = 80,
            Top = 170
        };

        AcceptButton = okButton;
        CancelButton = cancelButton;
        Controls.Add(titleLabel);
        Controls.Add(passwordLabel);
        Controls.Add(_passwordBox);
        Controls.Add(confirmLabel);
        Controls.Add(_confirmPasswordBox);
        Controls.Add(noteLabel);
        Controls.Add(okButton);
        Controls.Add(cancelButton);
    }

    private void Save()
    {
        if (_passwordBox.Text.Length < 5)
        {
            MessageBox.Show(Loc.Get("password_min_length", _locale), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_passwordBox.Text != _confirmPasswordBox.Text)
        {
            MessageBox.Show(Loc.Get("passwords_do_not_match", _locale), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
