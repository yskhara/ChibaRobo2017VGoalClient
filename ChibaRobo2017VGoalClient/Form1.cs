using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;

namespace ChibaRobo2017VGoalClient
{
    public partial class Form1 : Form
    {
        Timer timer = new Timer();
        SerialPort port = new SerialPort("COM8", 9600, Parity.None, 8, StopBits.One);

        char[] red_status = new char[11] { 'n', 'n', 'n', 'n', 'n', 'n', 'n', 'n', 'n', 'n', 'n' };
        char[] blue_status = new char[11] { 'n', 'n', 'n', 'n', 'n', 'n', 'n', 'n', 'n', 'n', 'n' };

        int red_cnt = 0;
        int blue_cnt = 0;

        private void refreshPortsList()
        {
            this.comboBox1.Items.Clear();
            this.comboBox1.Items.AddRange(SerialPort.GetPortNames());
        }

        private string getCommandString()
        {
            string data = "";

            for (int i = 0; i < 10; i++)
            {
                data += "r";
                switch (red_status[i])
                {
                    case 'w':
                        data += "w";
                        break;
                    case 'd':
                        data += "y";
                        break;
                    case 'n':
                    default:
                        data += "f";
                        break;
                }
                data += i.ToString();
                data += "\n";

                data += "b";
                switch (blue_status[i])
                {
                    case 'w':
                        data += "w";
                        break;
                    case 'd':
                        data += "y";
                        break;
                    case 'n':
                    default:
                        data += "f";
                        break;
                }
                data += i.ToString();
                data += "\n";
            }

            if (red_status[10] == 'v')
            {
                // red vgoal
                data += "vrx";
            }
            else if (blue_status[10] == 'v')
            {
                // red vgoal
                data += "vbx";
            }
            else
            {
                data += "vfx";
            }
            data += "\n";

            return data;
        }

        public Form1()
        {
            InitializeComponent();

            refreshPortsList();

            port.DataReceived += Port_DataReceived;

            timer.Interval = 500;
            timer.Tick += (sender, e) =>
            {
                if(!port.IsOpen)
                {
                    return;
                }

                port.Write(getCommandString());
            };

            timer.Start();
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if(port.BytesToRead < 3)
            {
                return;
            }

            if(port.ReadByte() != 0xaa)
            {
                return;
            }

            red_cnt = port.ReadByte();
            blue_cnt = port.ReadByte();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(port.IsOpen)
            {
                port.Close();
                this.button1.Text = "Connect";
                this.button2.Enabled = true;
                this.comboBox1.Enabled = true;
                refreshPortsList();

                return;
            }


            this.button1.Enabled = false;
            this.button2.Enabled = false;
            this.comboBox1.Enabled = false;

            string portName = (string)this.comboBox1.SelectedItem;

            if (!SerialPort.GetPortNames().Contains(portName))
            {
                this.button1.Enabled = true;
                this.button2.Enabled = true;
                this.comboBox1.Enabled = true;

                return;
            }
            else
                port.PortName = portName;

            try
            {
                port.Open();
            }
            catch
            {
                port.Close();

                this.button1.Enabled = true;
                this.button2.Enabled = true;
                this.comboBox1.Enabled = true;

                return;
            }

            if(port.IsOpen)
            {
                this.button1.Text = "Disonnect";
                this.button1.Enabled = true;
                this.button2.Enabled = false;
                this.comboBox1.Enabled = false;
                port.NewLine = "\n";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            refreshPortsList();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            buttonClear.Enabled = checkBox1.Checked && port.IsOpen;

            bool en = checkBox1.Checked;
            buttonRVGoal.Enabled = en;
            buttonRAllReset.Enabled = en;

            buttonBVGoal.Enabled = en;
            buttonBAllReset.Enabled = en;
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            if (port.IsOpen)
            {
                port.Write(new byte[] { 0xce }, 0, 1);
            }
            else
            {
                buttonClear.Enabled = false;
            }
            checkBox1.Checked = false;
        }

        private void updateScore()
        {
            red_cnt = 0;
            blue_cnt = 0;
            for(int i = 0; i < 10; i++)
            {
                if (red_status[i] == 'w')
                {
                    if (i < 3)
                        red_cnt += 1;
                    else if (i < 7)
                        red_cnt += 2;
                    else
                        red_cnt += 3;
                }

                if (blue_status[i] == 'w')
                {
                    if (i < 3)
                        blue_cnt += 1;
                    else if (i < 7)
                        blue_cnt += 2;
                    else
                        blue_cnt += 3;
                }
            }

            label1.Text = red_cnt.ToString();
            label2.Text = blue_cnt.ToString();
        }

        private void buttonWire_Click(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var str = button.Text.Split(' ');
  
            int target = Convert.ToUInt16(str[0][1].ToString());
            if(target < 0 || 9 < target)
            {
                return;
            }

            char op = 'n';
            switch (str[1])
            {
                case "配線":
                    op = 'w';
                    break;
                case "断線":
                    op = 'd';
                    break;
                case "リセット":
                    op = 'n';
                    break;
                default:
                    return;
                    //break;
            }

            if (str[0][0] == 'R')
            {
                // red field
                if(op == 'd' && red_status.Contains('d'))
                {
                    return;
                }

                red_status[target] = op;

                switch(op)
                {
                    case 'w':
                        button.Parent.BackColor = Color.Crimson;
                        break;
                    case 'd':
                        button.Parent.BackColor = Color.Gold;
                        break;
                    case 'n':
                        button.Parent.BackColor = SystemColors.Control;
                        break;
                    default:
                        return;
                }
            }
            else if(str[0][0] == 'B')
            {
                if (op == 'd' && blue_status.Contains('d'))
                {
                    return;
                }

                //blue
                blue_status[target] = op;

                switch (op)
                {
                    case 'w':
                        button.Parent.BackColor = Color.DodgerBlue;
                        break;
                    case 'd':
                        button.Parent.BackColor = Color.Gold;
                        break;
                    case 'n':
                        button.Parent.BackColor = SystemColors.Control;
                        break;
                    default:
                        return;
                }
            }

            checkBox1.Checked = false;

            updateScore();
        }

        private void buttonRVGoal_Click(object sender, EventArgs e)
        {
            red_status[10] = 'v';
            ((Button)sender).Parent.BackColor = Color.Crimson;
            checkBox1.Checked = false;

            updateScore();
        }

        private void buttonBVGoal_Click(object sender, EventArgs e)
        {
            blue_status[10] = 'v';
            ((Button)sender).Parent.BackColor = Color.DodgerBlue;
            checkBox1.Checked = false;

            updateScore();
        }

        private void buttonRVReset_Click(object sender, EventArgs e)
        {
            red_status[10] = 'n';
            ((Button)sender).Parent.BackColor = SystemColors.Control;
            checkBox1.Checked = false;

            updateScore();
        }

        private void buttonBVReset_Click(object sender, EventArgs e)
        {
            blue_status[10] = 'n';
            ((Button)sender).Parent.BackColor = SystemColors.Control;
            checkBox1.Checked = false;

            updateScore();
        }

        private void buttonRAllReset_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 11; i++)
            {
                red_status[i] = 'n';
            }
            new List<Panel>() { panelR0, panelR1, panelR2, panelR3, panelR4, panelR5, panelR6, panelR7, panelR8, panelR9, panelR10 }
                .ForEach(p => p.BackColor = SystemColors.Control);
            checkBox1.Checked = false;

            updateScore();
        }

        private void buttonBAllReset_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 11; i++)
            {
                blue_status[i] = 'n';
            }
            new List<Panel>() { panelB0, panelB1, panelB2, panelB3, panelB4, panelB5, panelB6, panelB7, panelB8, panelB9, panelB10 }
                .ForEach(p => p.BackColor = SystemColors.Control);
            checkBox1.Checked = false;

            updateScore();
        }

        private void button33_Click(object sender, EventArgs e)
        {
            System.Console.WriteLine(getCommandString());
        }
    }
}
