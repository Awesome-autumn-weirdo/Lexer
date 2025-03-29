using Lexer.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lexer
{
    public partial class Help : Form
    {
        public Help()
        {
            InitializeComponent();

            Text = "Справка";
            webBrowser1.Navigate(Directory.GetCurrentDirectory().Replace("\\bin\\Debug", "") + "\\Properties\\Справка.html");

        }
    }
}
