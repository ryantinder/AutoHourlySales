using System;
using System.Windows.Forms;
using System.Xml;
namespace SettingsForm
{
    public partial class SettingsForm : Form
    {
        XmlDocument xml = new XmlDocument();

        public SettingsForm()
        {
            InitializeComponent();
            xml.Load("./SettingsForm.dll.config");

            StoreId_panel.SendToBack();
            StoreId_box.BorderStyle = BorderStyle.None;
            textBox2.Text = ReadKey("StoreId");


            string store = ReadKey("Store");
            store = store.Substring(1, store.Length - 1);
            textBox4.Text = store;


            string comboLoadText = ReadKey("pointedTo");
            if (comboLoadText == "testDB")
            {
                comboBox1.SelectedIndex = 0;
            }
            else if (comboLoadText == "publicDev") {
                comboBox1.SelectedIndex = 1;
            }
            else
            {
                comboBox1.SelectedIndex = 2;
            }

            checkBox1.Checked = ReadKey("skipUpdate") == "true" ? true : false;
            checkBox2.Checked = ReadKey("consoleReadKey()") == "true" ? true : false;

            string dateOverride = ReadKey("dateOverride");
            if (dateOverride == "false")
            {
                checkBox3.Checked = false;
                dateTimePicker1.Enabled = false;
            }
            else
            {
                checkBox3.Checked = true;
                dateTimePicker1.Enabled = true;
                dateTimePicker1.Value = DateTime.ParseExact(dateOverride, "yyyy-MM-dd", null);
            }

            checkBox4.Checked = ReadKey("mobileOff") == "true" ? true : false;
        }

        

        private void WriteKey(string key, string value)
        {
            XmlNodeList nodes = xml.SelectNodes("appSettings/add");
            foreach (XmlNode node in nodes)
            {
                XmlAttributeCollection nodeAtt = node.Attributes;
                if (nodeAtt["key"].Value.ToString() == key)
                {
                    XmlAttribute nValue = node.Attributes["value"];
                    nValue.Value = value;
                    xml.Save("./SettingsForm.dll.config");
                    return;
                }
            }
        }
        private string ReadKey(string key)
        {
            XmlNodeList nodes = xml.SelectNodes("appSettings/add");
            foreach (XmlNode node in nodes)
            {
                XmlAttributeCollection nodeAtt = node.Attributes;
                if (nodeAtt["key"].Value == key)
                {
                    return nodeAtt["value"].Value;
                }
            }
            return "Node not found";
        }

        private void StoreID_TextChanged(object sender, EventArgs e)
        {
            string newID = textBox2.Text;
            WriteKey("StoreId", newID);
            
        }

        private void Store_TextChanged(object sender, EventArgs e)
        {
            string newStore = "#" + textBox4.Text;
            WriteKey("Store", newStore);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string newTarget;

            switch (comboBox1.SelectedIndex)
            {
                case 0:
                    newTarget = "testDB";
                    break;
                case 1:
                    newTarget = "publicDev";
                    break;
                case 2:
                    newTarget = "liveTacomayo";
                    break;
                default:
                    newTarget = "publicDev";
                    break;
            }

            WriteKey("pointedTo", newTarget);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            WriteKey("Override1am", checkBox1.Checked ? "true" : "false");
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            WriteKey("consoleReadKey()", checkBox2.Checked ? "true" : "false");
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
            {
                dateTimePicker1.Enabled = true;
                WriteKey("dateOverride", dateTimePicker1.Value.ToString("yyyy-MM-dd"));
            }
            else
            {
                dateTimePicker1.Enabled = false;
                WriteKey("dateOverride", "false");
            }
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            WriteKey("dateOverride", dateTimePicker1.Value.ToString("yyyy-MM-dd"));
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked)
            {
                textBox5.Enabled = true;
                WriteKey("hourOverride", textBox5.Text);
            }
            else
            {
                textBox5.Enabled = false;
                WriteKey("hourOverride", "false");
            }
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            WriteKey("hourOverride", textBox5.Text);
        }
    }

}
