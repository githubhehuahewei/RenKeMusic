using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
namespace 全网音乐
{
    class RichTextBoxEx : RichTextBox
    {
        //public RichTextBoxEx()
        //{
        //    this.Cursor = Cursors.Arrow;//设置鼠标样式
        //}
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x7 || m.Msg == 0x201 || m.Msg == 0x202 || m.Msg == 0x203 || m.Msg == 0x204 || m.Msg == 0x205 || m.Msg == 0x206 || m.Msg == 0x0100 || m.Msg == 0x0101)
            {
                return;
            }
            base.WndProc(ref m);
        }
    }
}
