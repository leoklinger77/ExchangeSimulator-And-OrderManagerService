using DevExpress.XtraEditors;
using KlingerOmsGui.Controls.VerticalBook;
using System.Windows.Forms;

namespace KlingerOmsGui {
    public partial class Form1 : DevExpress.XtraBars.Ribbon.RibbonForm {
        public Form1() {
            InitializeComponent();
        }

        private void barButtonItem1_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e) {
            var control = new VerticalBookControl();
            control.Dock = DockStyle.Fill;
            var form = new XtraForm();
            form.Size = control.Size;
            form.Controls.Add(control);
            form.Show();
        }
    }
}
