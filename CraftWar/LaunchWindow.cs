using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace CraftWar
{
    public partial class LaunchWindow : Form
    {
        public LaunchWindow()
        {
            InitializeComponent();
        }

        public NetworkManager networkManager = new NetworkManager(null, null);
        public int seed;
        private void startButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(usernameTextBox.Text) || string.IsNullOrEmpty(ipAddressTextBox.Text))
            {
                MessageBox.Show("All fields must be filled.");
                return;
            }

            try
            {
                IPAddress testAddress = IPAddress.Parse(ipAddressTextBox.Text);
            }
            catch
            {
                MessageBox.Show("Invalid IP address.");
                return;
            }

            networkManager.ipAddress = ipAddressTextBox.Text;
            networkManager.username = usernameTextBox.Text;

            try
            {
                networkManager.connect();
            }
            catch
            {
                MessageBox.Show("Error while trying to connect.");
            }

            label1.Visible = false;
            label2.Visible = false;
            usernameTextBox.Visible = false;
            ipAddressTextBox.Visible = false;
            startButton.Visible = false;
            pictureBox1.Visible = false;
            connectingLabel.Visible = true;

            if (!networkManager.host)
            {
                //Await HOSTINFO before closing
                receiveHostInfo();
            }
            else
            {
                seed = networkManager.gameServer.mapSeed;
                DialogResult = DialogResult.OK;
                Hide();
                return;
            }
        }

        public const int timeBetweenRequests = 100;
        private async void receiveHostInfo()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    foreach (string b in networkManager.localGameClient.readIncomingAsString().Split(GameServer.messageSeparator))
                    {
                        string[] data = b.Split(GameServer.dataSeparator);
                        if (data[0] == ((int)GameServer.NetworkKeyword.hostInfo).ToString())
                        {
                            //Host data found
                            try
                            {
                                seed = int.Parse(data[1]);
                                networkManager.localGameClient.clientID = int.Parse(data[2]);
                                //Data successfully collected. Can now close
                                DialogResult = DialogResult.OK;
                                Hide();
                                return;
                            }
                            catch
                            {
                            }
                        }
                    }

                    System.Threading.Thread.Sleep(timeBetweenRequests);
                }
            });
        }

        private void LaunchWindow_Load(object sender, EventArgs e)
        {
            IPAddress[] ipv4Addresses = Array.FindAll(Dns.GetHostEntry(string.Empty).AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
            if (ipv4Addresses[0] != null)
            {
                ipAddressTextBox.Text = ipv4Addresses[0].ToString();
            }
        }
    }
}
