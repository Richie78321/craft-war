﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CraftWar
{
    public partial class LoadingScreen : Form
    {
        public LoadingScreen()
        {
            InitializeComponent();
        }

        public void updateLoadingLabel(string newText)
        {
            loadingLabel.Text = newText;
        }

        private void loadingLabel_Click(object sender, EventArgs e)
        {

        }

        private void LoadingScreen_Load(object sender, EventArgs e)
        {

        }
    }
}
