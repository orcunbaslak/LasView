using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace LasView {
    static class Program {
        [STAThread]
        static void Main() {
            using (MainForm form = new MainForm(1024, 768)) {
                form.Run(30.0);
            }
        }
    }
}
