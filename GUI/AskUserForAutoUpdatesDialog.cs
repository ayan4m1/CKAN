using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CKAN
{
    /// <summary>
    /// Simple dialog to prompt user about auto-update preferences.
    /// </summary>
    public partial class AskUserForAutoUpdatesDialog : Form
    {
        public AskUserForAutoUpdatesDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Ensure that the dialog result of this form is non-empty if the user hit the "close" window icon.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AskUserForAutoUpdatesDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.None && e.CloseReason == CloseReason.UserClosing)
            {
                DialogResult = DialogResult.Cancel;
            }
        }
    }
}
