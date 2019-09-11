using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraRichEdit.API.Native;

namespace RichBox
{
    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DevExpress.XtraRichEdit.API.Native.Document doc = richEditControl1.Document;
            doc.BeginUpdate();
            //doc.Text = "我是中国人，我爱自己的pylg";
            CharacterProperties cp = doc.BeginUpdateCharacters(0, doc.Text.Length);
            cp.FontName = "宋体";
            cp.FontSize = 14;
            doc.EndUpdateCharacters(cp);
            doc.EndUpdate();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DocumentRange range = richEditControl1.Document.Range;
            CharacterProperties cp = this.richEditControl1.Document.BeginUpdateCharacters(range);
            cp.FontName = "新宋体";
            cp.FontSize =25;
            this.richEditControl1.Document.EndUpdateCharacters(cp);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            DocumentRange range = richEditControl1.Document.Paragraphs[1].Range;
            CharacterProperties cp = this.richEditControl1.Document.BeginUpdateCharacters(range);
            cp.FontName = "宋体";
            cp.FontSize = 25;
            this.richEditControl1.Document.EndUpdateCharacters(cp);
        }
    }
}
