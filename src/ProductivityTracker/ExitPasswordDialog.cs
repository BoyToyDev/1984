using System.Drawing;
using System.Windows.Forms;

namespace ProductivityTracker;

internal sealed class ExitPasswordDialog : Form
{
    private readonly TextBox _passwordBox = new();

    public string Password => _passwordBox.Text;

    public ExitPasswordDialog(string productName, string actionName, string locale = "en")
    {
        Text = productName;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(360, 130);

        var label = new Label
        {
            Text = Loc.Get("enter_password", locale) + ":",
            AutoSize = true,
            Left = 16,
            Top = 18
        };

        _passwordBox.Left = 16;
        _passwordBox.Top = 44;
        _passwordBox.Width = 328;
        _passwordBox.UseSystemPasswordChar = true;

        var okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Left = 174,
            Width = 80,
            Top = 88
        };

        var cancelButton = new Button
        {
            Text = Loc.Get("cancel", locale),
            DialogResult = DialogResult.Cancel,
            Left = 264,
            Width = 80,
            Top = 88
        };

        AcceptButton = okButton;
        CancelButton = cancelButton;
        Controls.Add(label);
        Controls.Add(_passwordBox);
        Controls.Add(okButton);
        Controls.Add(cancelButton);
    }
}
