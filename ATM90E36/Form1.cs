// ATM90E36 utility - read and write registers, calibrate.
// Copyright (C) 2019 Mastro Gippo
//
// This program is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the Free Software
// Foundation, either version 3 of the License, or (at your option) any later
// version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of  MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License along with
// this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace ATM90E36
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();
                button1.Text = "Open";
            }
            else
            {
                serialPort1.Open();
                button1.Text = "Close";
            }
        }

        ARegister[] regs = new ARegister[256];
        private class ARegister
        {
            public string Name = "";
            public UInt16 address = 0;
            public UInt16 value = 0;
            public string desc = "";

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] fr = File.ReadAllLines("reg_def.txt");
            foreach (string s in fr)
            {
                string[] dat = s.Split('#');
                if (dat[0] != "")
                {
                    string[] eq = dat[0].Split('=');
                    if (eq.Length == 2)
                    {
                        UInt16 ad = Convert.ToUInt16(eq[1].Trim(), 16);
                        if (ad <= 256)
                        {
                            regs[ad] = new ARegister();
                            regs[ad].Name = eq[0];
                            regs[ad].address = ad;
                            if (dat.Length > 1)
                                regs[ad].desc = dat[1];

                            ListViewItem lvi = new ListViewItem(string.Format("{0:X2}", ad));
                            lvi.SubItems.Add(regs[ad].Name);
                            lvi.SubItems.Add("");
                            lvi.SubItems.Add(regs[ad].desc);

                            listView1.Items.Add(lvi);

                        }
                    }
                }
            }

            listView2.Items.Add("Urms (V)");
            listView2.Items.Add("Irms (A)");
            listView2.Items.Add("Pmean(W)");
            //listView2.Items.Add("Qmean(Kvar)");
            //listView2.Items.Add("Smean(Kva)");
            listView2.Items.Add("PF");
            listView2.Items.Add("PmeanF(kW)");
            listView2.Items.Add("PmeanH(kW)");
            listView2.Items.Add("Phase (°)");
            listView2.Items.Add("Freq (Hz)");
            listView2.Items.Add("Temp (°C)");
            listView2.Items.Add("SYS");

            foreach (ListViewItem l in listView2.Items)
            {
                l.SubItems.Add("");
                l.SubItems.Add("");
                l.SubItems.Add("");
                l.SubItems.Add("");
            }

            button1_Click(sender, e);
        }

        public void UpdateItem(UInt16 Addr)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<UInt16>(UpdateItem), new object[] { Addr });
                return;
            }
            if (regs[Addr] == null) return;

            string Adds = string.Format("{0:X2}", Addr);
            regs[Addr].value = (UInt16)(((UInt16)(buff[(Addr * 2) + 5]) << 8) + (UInt16)(buff[(Addr * 2) + 4]));
            listView1.FindItemWithText(Adds).SubItems[2].Text = string.Format("{0:X4}", regs[Addr].value);

        }

        string CalcVal(int BaseAddr, double mul, int decimals)
        {
            double tmp;
            tmp = ((double)regs[BaseAddr].value) * mul;
            tmp += ((double)(regs[BaseAddr + 0x10].value >> 8)) * (mul / 256);

            if (decimals == 1)
                return tmp.ToString("0.#");
            else if(decimals == 2)
                return tmp.ToString("0.##");
            else if (decimals == 3)
                return tmp.ToString("0.###");
            else
                return tmp.ToString("0");
        }
        string CalcSign(int BaseAddr, double mul, int decimals)
        {
            double tmp;
            if (regs[BaseAddr].value >= 0x8000) //MSB set, negative
                tmp = (double)(regs[BaseAddr].value - 0x10000);
            else
                tmp = (double)regs[BaseAddr].value;
            tmp *= mul;

            if (decimals == 1)
                return tmp.ToString("0.#");
            else if (decimals == 2)
                return tmp.ToString("0.##");
            else if (decimals == 3)
                return tmp.ToString("0.###");
            else
                return tmp.ToString("0");
        }

        public void UpdateVals()
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action(UpdateVals), new object[] {  });
                return;
            }

            double tmp;
            int li = 0;
            //listView2.Items.Add("Urms (V)");
            listView2.Items[li].SubItems[2].Text = CalcVal(0xD9, 0.01, 2);
            listView2.Items[li].SubItems[3].Text = CalcVal(0xDA, 0.01, 2);
            listView2.Items[li].SubItems[4].Text = CalcVal(0xDB, 0.01, 2);
            li++;

            //listView2.Items.Add("Irms (A)");
            listView2.Items[li].SubItems[2].Text = CalcVal(0xDD, 0.001, 3);
            listView2.Items[li].SubItems[3].Text = CalcVal(0xDE, 0.001, 3);
            listView2.Items[li].SubItems[4].Text = CalcVal(0xDF, 0.001, 3);
            li++;

            //listView2.Items.Add("Pmean(W)");
            listView2.Items[li].SubItems[1].Text = CalcVal(0xB0, 4, 2);
            listView2.Items[li].SubItems[2].Text = CalcVal(0xB1, 1, 2);
            listView2.Items[li].SubItems[3].Text = CalcVal(0xB2, 1, 2);
            listView2.Items[li].SubItems[4].Text = CalcVal(0xB3, 1, 2);
            li++;

            //listView2.Items.Add("Qmean(Kvar)");
            //listView2.Items.Add("Smean(Kva)");

            //listView2.Items.Add("PF");
            listView2.Items[li].SubItems[1].Text = CalcSign(0xBC, 0.001, 3);
            listView2.Items[li].SubItems[2].Text = CalcSign(0xBD, 0.001, 3);
            listView2.Items[li].SubItems[3].Text = CalcSign(0xBE, 0.001, 3);
            listView2.Items[li].SubItems[4].Text = CalcSign(0xBF, 0.001, 3);
            li++;

            //listView2.Items.Add("PmeanF(kW)");
            listView2.Items[li].SubItems[1].Text = CalcVal(0xD0, 4, 2);
            listView2.Items[li].SubItems[2].Text = CalcVal(0xD1, 1, 2);
            listView2.Items[li].SubItems[3].Text = CalcVal(0xD2, 1, 2);
            listView2.Items[li].SubItems[4].Text = CalcVal(0xD3, 1, 2);
            li++;

            //listView2.Items.Add("PmeanH(kW)");
            listView2.Items[li].SubItems[1].Text = CalcVal(0xD4, 4, 2);
            listView2.Items[li].SubItems[2].Text = CalcVal(0xD5, 1, 2);
            listView2.Items[li].SubItems[3].Text = CalcVal(0xD6, 1, 2);
            listView2.Items[li].SubItems[4].Text = CalcVal(0xD7, 1, 2);
            li++;

            //listView2.Items.Add("Phase (°)");
            listView2.Items[li].SubItems[2].Text = CalcSign(0xF9, 0.1, 1);
            listView2.Items[li].SubItems[3].Text = CalcSign(0xFA, 0.1, 1);
            listView2.Items[li].SubItems[4].Text = CalcSign(0xFB, 0.1, 1);
            li++;


            //listView2.Items.Add("Freq (Hz)");
            tmp = ((double)regs[0xF8].value) * 0.01;
            listView2.Items[li].SubItems[1].Text = tmp.ToString("0.##");
            li++;

            //listView2.Items.Add("Temp (°C)");
            //NOTE: TEST NEGATIVE TEMP!
            listView2.Items[li].SubItems[1].Text = CalcSign(0xFC, 1, 0);
            li++;

            listView2.Items[li].SubItems[1].Text = regs[0x01].value.ToString("X4");
            li++;
        }

        UInt16 Checks(int staA, int endA)
        {
            //This register should be written after the 31H - 3AH registers are written. 
            //The calculation of the CS0 register is as follows:
            //The low byte of 3BH register is: L3B = MOD(H31 + H32 +...+ H3A + L31 + L32 +...+ L3A, 2 ^ 8)
            //The high byte of 3BH register is: H3B = H31 XOR H32 XOR...XOR H3A XOR L31 XOR L32 XOR... XOR L3A

            int tmpl = 0;
            int tmph = 0;
            for (int i = staA; i <= endA; i++)
            {
                tmpl += (byte)(regs[i].value) + (byte)(regs[i].value >> 8);
                tmph ^= (byte)(regs[i].value) ^ (byte)(regs[i].value >> 8);
            }
            UInt16 CS = (UInt16)((byte)(((tmpl % 256) + 256) % 256) + (UInt16)((tmph << 8) & 0xFF00));
            return CS;
        }

        void ReadReg(byte StartAddr, byte EndAddr)
        {
            byte[] cmd = { 0x69, (byte)'R', StartAddr, EndAddr };
            serialPort1.Write(cmd, 0, 4);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ReadReg(0x00, 0xFF);
        }

        byte[] buff = new byte[1000];
        int pos = 0;
        int le = 0;
        int lstart = 0;
        int lend = 0;
        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            byte inb;
            while (serialPort1.BytesToRead != 0)
            {
                int f = serialPort1.ReadByte();
                inb = (byte)f;
                if (pos < 2)
                {
                    if (inb == 0x69)
                        buff[pos++] = inb;
                    else
                        pos = 0;
                }
                else if (pos < 3)
                {
                    //lend = inb;
                    lstart = inb;
                    buff[pos++] = inb;
                }
                else if (pos < 4)
                {
                    lend = inb;
                    //lstart = inb;
                    le = lend - lstart;
                    buff[pos++] = inb;
                }
                else
                {
                    buff[pos++] = inb;

                    if (pos >= (((le + 1) * 2) + 4))
                    {
                        //end
                        for (int i = lstart; i <= lend; i++)
                            UpdateItem((UInt16)(i));
                        UpdateVals();
                        pos = 0;
                    }
                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            timer1.Enabled = checkBox1.Checked;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            button2_Click(sender, e);
        }

        void WriteReg(UInt16 addr, UInt16 val)
        {
            byte[] cmd = { 0x69, (byte)'W', 0x00, 0x00, 0x00, 0x00 };
            cmd[2] = (byte)(addr >> 8);
            cmd[3] = (byte)(addr);
            cmd[4] = (byte)(val >> 8);
            cmd[5] = (byte)(val);
            serialPort1.Write(cmd, 0, 6);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            WriteReg(Convert.ToUInt16(txtAddr.Text, 16), Convert.ToUInt16(txtVal.Text, 16));
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count != 1) return;
            txtAddr.Text = listView1.SelectedItems[0].Text;
            txtVal.Text = listView1.SelectedItems[0].SubItems[2].Text;

        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }


        UInt16 OffsetCal(int reg)
        {
            //a.Read measurement registers(32 bits).It is suggested to read several times to get the average value;
            UInt32 tmp = regs[reg].value;
            tmp = tmp << 16;
            tmp += regs[reg + 0x10].value;

            //b.Right shift the 32 - bit data by 7 bits(ignore the lowest 7 bits);
            tmp = tmp >> 7;

            //c.Invert all bits and add 1(2’s complement);
            tmp = (~tmp) + 1;

            //d.Write the lower 16 - bit result to the offset register
            return (UInt16)(tmp);
        }

        private void button4_Click(object sender, EventArgs e)
        {

            //Offset
/*            WriteReg(0x63, OffsetCal(0xD9));
            WriteReg(0x67, OffsetCal(0xDA));
            WriteReg(0x6B, OffsetCal(0xDB));
            WriteReg(0x64, OffsetCal(0xDD));
            WriteReg(0x68, OffsetCal(0xDE));
            WriteReg(0x6C, OffsetCal(0xDF));*/

            //Offset
            WriteReg(0x63, 0xFC60);
            WriteReg(0x67, 0xFC60);
            WriteReg(0x6B, 0xFC60);
            WriteReg(0x64, 0xFC60);
            WriteReg(0x68, 0xFC60);
            WriteReg(0x6C, 0xFC60);
        }

        /*
            WriteReg(0x30, 0x5678);
            WriteReg(0x3B, Checks(0x31, 0x3A));
            WriteReg(0x30, 0x8765);

            WriteReg(0x40, 0x5678);
            WriteReg(0x4D, Checks(0x41, 0x4D));
            WriteReg(0x40, 0x8765);

            WriteReg(0x50, 0x5678);
            WriteReg(0x57, Checks(0x51, 0x56));
            WriteReg(0x50, 0x8765);
        */

        void GainCal(byte BaseAddr, byte CalAddr, double mul, double Measured)
        {
            double Reg;
            Reg = ((double)regs[BaseAddr].value) * mul;
            Reg += ((double)(regs[BaseAddr + 0x10].value >> 8)) * (mul / 256);

            UInt16 cal = (UInt16)(Math.Floor( (Measured / Reg) * (double)(regs[CalAddr].value)));
            WriteReg(CalAddr, cal);

        }
        private void button5_Click(object sender, EventArgs e)
        {
            GainCal(0xD9, 0x61, 0.01, Convert.ToDouble(textBox1.Text));
            GainCal(0xDD, 0x62, 0.001, Convert.ToDouble(textBox2.Text));
            /*GainCal(0xDA, 0x65, 0.01, Convert.ToDouble(textBox1.Text));
            GainCal(0xDB, 0x69, 0.01, Convert.ToDouble(textBox1.Text));
            GainCal(0xDE, 0x66, 0.001, Convert.ToDouble(textBox2.Text));
            GainCal(0xDF, 0x6A, 0.001, Convert.ToDouble(textBox2.Text));*/
            //double Meas = Convert.ToDouble(textBox1.Text);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //ReadReg(0x00, 0xFF);
            if (regs[0x60].value == 0x5678)
            {
                button6.Text = "Start CAL gain/offset";
                WriteReg(0x6F, Checks(0x61, 0x6E));
                WriteReg(0x60, 0x8765);
            }
            else
            {
                button6.Text = "SAVE CAL gain/offset";
                WriteReg(0x60, 0x5678);
            }
        }
    }
}
