using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lexer
{
    public partial class About: Form
    {
        public About()
        {
            InitializeComponent();

            Text = "О программе";
            label1.Text = "Версия 0.0.1";
            label2.Text = "Автор: Качигина Валерия Алексеевна";
            label3.Text = "Языковой процессор";
        }

        
    }
}
