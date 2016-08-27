using System;
using System.Windows.Forms;

namespace CKAN
{
    public class MainTabControl : TabControl
    {
        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            if (SelectedTab != null && SelectedTab.Name.Equals("ManageModsTabPage"))
                ((Main)Parent).ModList.Focus();
        }
    }
}

