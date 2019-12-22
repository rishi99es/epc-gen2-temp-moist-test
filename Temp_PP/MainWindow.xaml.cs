using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ThingMagic;
using System.IO;
namespace Temp_PP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            string uri = "tmr://192.168.0.2/";
            Reader r = Reader.Create(uri);
            try
            {
                r.Connect();
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
                return;
            }
            if (Reader.Region.UNSPEC == (Reader.Region)r.ParamGet("/reader/region/id"))
            {
                r.ParamSet("/reader/region/id", Reader.Region.NA);
            }

            r.ParamSet("/reader/radio/readpower", 2000);                //30 dBm
            //r.ParamSet("/reader/gen2/t4", 3000);

            StreamWriter file;
            string file_name = "C:/Users/Rishi/ttest2.csv";
            file = File.CreateText(file_name);
            file.Close();

            SimpleReadPlan readPlan;
            TagReadData[] readCalib, readRSSI, readTemp;
            TagReadData[] sens_moistData;
            Gen2.Select select;

            ///*
            byte[] mask = { Convert.ToByte("1F", 16) };

            select = new Gen2.Select(false, Gen2.Bank.USER, 0xD0, 8, mask);
            TagOp calibRead = new Gen2.ReadData(Gen2.Bank.USER, 0x8, 4);
            readPlan = new SimpleReadPlan(new int[] { 1 }, TagProtocol.GEN2, select, calibRead, true, 100);
            System.Threading.Thread.Sleep(100);
            r.ParamSet("/reader/read/plan", readPlan);
            readCalib = r.Read(200);
            System.Threading.Thread.Sleep(100);


            select = new Gen2.Select(false, Gen2.Bank.USER, 0xD0, 8, mask);
            System.Threading.Thread.Sleep(300);
            TagOp chipRSSIRead = new Gen2.ReadData(Gen2.Bank.RESERVED, 0xD, 1);
            readPlan = new SimpleReadPlan(new int[] { 1 }, TagProtocol.GEN2, select, chipRSSIRead, true, 100);
            System.Threading.Thread.Sleep(300);
            r.ParamSet("/reader/read/plan", readPlan);
            System.Threading.Thread.Sleep(300);
            readRSSI = r.Read(200);
            System.Threading.Thread.Sleep(1000);

            //*/

            select = new Gen2.Select(false, Gen2.Bank.USER, 0xE0, 0, new byte[0]);
            // add 3ms delay here. Recommended in datasheet.
            TagOp tempRead = new Gen2.ReadData(Gen2.Bank.RESERVED, 0xE, 1);
            readPlan = new SimpleReadPlan(new int[] { 1 }, TagProtocol.GEN2, select, tempRead, 100);
            r.ParamSet("/reader/read/plan", readPlan);
            readTemp = r.Read(200);
            System.Threading.Thread.Sleep(1000);

            //   /*
            // Data - Moisture (Magnus 2)
            select = new Gen2.Select(false, Gen2.Bank.USER, 0x00, 0, new byte[0]);
            TagOp moistRead = new Gen2.ReadData(Gen2.Bank.RESERVED, 0xB, 1);
            readPlan = new SimpleReadPlan(new int[] { 1 }, TagProtocol.GEN2, select, moistRead, true, 100);
            r.ParamSet("/reader/read/plan", readPlan);
            sens_moistData = r.Read(200);
            System.Threading.Thread.Sleep(1000);
            //*/

            SortedDictionary<string, int> epc_code1 = new SortedDictionary<string, int>();
            SortedDictionary<string, int> epc_code2 = new SortedDictionary<string, int>();
            SortedDictionary<string, int> epc_temp1 = new SortedDictionary<string, int>();
            SortedDictionary<string, int> epc_temp2 = new SortedDictionary<string, int>();
            SortedDictionary<string, int> epc_tempV = new SortedDictionary<string, int>();
            SortedDictionary<string, int> epc_rssi = new SortedDictionary<string, int>();
            SortedDictionary<int, string> n_epc = new SortedDictionary<int, string>();

            SortedDictionary<string, int> epc_moistC = new SortedDictionary<string, int>();

            List<string> l_epc_moistC = new List<string>();
            List<int> lMoistC = new List<int>();

            List<string> l_epc_rssi = new List<string>();
            List<string> l_epc_tempV = new List<string>();
            List<string> lEPC3 = new List<string>();
            List<int> lRSSI = new List<int>();
            List<int> lCode1 = new List<int>();
            List<int> lCode2 = new List<int>();
            List<int> lTemp1 = new List<int>();
            List<int> lTemp2 = new List<int>();
            List<int> lTempV = new List<int>();
            List<float> temp_val = new List<float>();

            int count = 0;

            ///*
            foreach (TagReadData result in readCalib)
            {
                count++;
                string EPC = result.EpcString;
                string frequency = result.Frequency.ToString();
                string tempCodeHex = ByteFormat.ToHex(result.Data, "", "");
                string chunk_1 = tempCodeHex.Substring(0, 4);
                int crc = Convert.ToInt32(chunk_1, 16);
                string chunk_2 = tempCodeHex.Substring(4, 3);
                int code1 = Convert.ToInt32(chunk_2, 16);
                string chunk_3 = tempCodeHex.Substring(7, 3);
                int temp1 = Convert.ToInt32(chunk_3, 16);
                temp1 = (temp1 & 0xFFE) >> 1;                          // bit shift temp1
                string chunk_4 = tempCodeHex.Substring(9, 4);
                int code2 = Convert.ToInt32(chunk_4, 16);
                code2 = (code2 & 0x1FFE) >> 1;                         // bit shift code 1
                string chunk_5 = tempCodeHex.Substring(12, 4);
                int temp2 = Convert.ToInt32(chunk_5, 16);
                temp2 = (temp2 & 0x1FFC) >> 2;                         // bit shift temp 2

                n_epc.Add(count, EPC.Substring(19, 5));
                epc_code1.Add(EPC.Substring(19, 5), code1);
                epc_code2.Add(EPC.Substring(19, 5), code2);
                epc_temp1.Add(EPC.Substring(19, 5), temp1);
                epc_temp2.Add(EPC.Substring(19, 5), temp2);
            }
            foreach (TagReadData result in readRSSI)
            {
                string EPC = result.EpcString;
                string frequency = result.Frequency.ToString();
                string rssiHex = ByteFormat.ToHex(result.Data, "", "");
                int rssi = Convert.ToInt32(rssiHex, 16);

                epc_rssi.Add(EPC.Substring(19, 5), rssi);
            }

            ///*
            foreach (TagReadData result in readTemp)
            {
                string EPC = result.EpcString;
                string frequency = result.Frequency.ToString();
                string tempCodeHex = ByteFormat.ToHex(result.Data, "", "");
                int tempCode;
                if (tempCodeHex == "")
                    tempCode = 0;
                else
                    tempCode = Convert.ToInt32(tempCodeHex, 16);
                if (tempCode > 1000 && tempCode < 3500)
                    epc_tempV.Add(EPC.Substring(19, 5), tempCode);
            }

            foreach (TagReadData result in sens_moistData)
            {
                string EPCM;
                //if (result.EpcString.Length == 4)
                {
                    EPCM = result.EpcString.Substring(19,5);
                    //string frequency = result.Frequency.ToString();
                    string moistCodeHex = ByteFormat.ToHex(result.Data, "", "");
                    int moistCode;
                    if (moistCodeHex == "")
                        moistCode = 0;
                    else
                        moistCode = Convert.ToInt32(moistCodeHex, 16);

                    epc_moistC.Add(EPCM, moistCode);
                }
            }

            //*/
            epc_code1.Keys.Except(epc_rssi.Keys).ToList().ForEach(k => epc_rssi.Add(k, 0));
            epc_code1.Keys.Except(epc_tempV.Keys).ToList().ForEach(k => epc_tempV.Add(k, 0));

            foreach (KeyValuePair<string, int> kvp in epc_rssi)
            {
                l_epc_rssi.Add(kvp.Key);
                lRSSI.Add(kvp.Value);
            }

            foreach (KeyValuePair<string, int> kvp in epc_code1)
            {
                lEPC3.Add(kvp.Key);
                lCode1.Add(kvp.Value);

            }
            foreach (KeyValuePair<string, int> kvp in epc_code2)
            {
                lCode2.Add(kvp.Value);
            }
            foreach (KeyValuePair<string, int> kvp in epc_temp1)
            {
                lTemp1.Add(kvp.Value);
            }
            foreach (KeyValuePair<string, int> kvp in epc_temp2)
            {
                lTemp2.Add(kvp.Value);
            }
            ///*
            foreach (KeyValuePair<string, int> kvp in epc_tempV)
            {
                l_epc_tempV.Add(kvp.Key);
                lTempV.Add(kvp.Value);
            }
            foreach (KeyValuePair<string, int> kvp in epc_moistC)
            {
                l_epc_moistC.Add(kvp.Key);
                lMoistC.Add(kvp.Value);

                //*/

                for (int item = 0; item < lEPC3.Count; item++)
                {
                    float ratio = ((lTemp2[item] - lTemp1[item]) / (lCode2[item] - lCode1[item]));
                    float tempV;
                    if (lTempV[item] == 0)
                        tempV = 0;
                    else
                        tempV = (float)((0.1) * ((ratio * (lTempV[item] - lCode1[item])) + (lTemp1[item] - 800)));

                    temp_val.Add(tempV);
                }

                //file = File.AppendText("C:/Users/Rishi/tFile.csv");
                ///*
                file = File.CreateText("C:/Users/Rishi/ttest2.csv");
                file.Write("Tag EPC, RSSI, Temperature \n");
                file.Close();
                file = File.AppendText("C:/Users/Rishi/ttest2.csv");
                for (int i = 0; i < lEPC3.Count; i++)
                {
                    file.Write(lEPC3[i] + ", " + lRSSI[i] + ", " + temp_val[i] + "\n");
                }
                //file.WriteLine();
                file.Close();
                //*/
                file = File.CreateText("C:/Users/Rishi/mtest2.csv");
                file.Write("Tag EPC, RSSI, MoistC \n");
                file.Close();
                file = File.AppendText("C:/Users/Rishi/mtest2.csv");
                for (int i = 0; i < l_epc_moistC.Count; i++)
                {
                    string test = l_epc_moistC[i].Substring(0, 1);
                    if (test.Contains("1"))
                    { }
                    else
                        file.Write(l_epc_moistC[i] + ", " + "-" + ", " + lMoistC[i] + "\n");
                }
                //file.WriteLine();
                file.Close();
            }
        }
    }
}


