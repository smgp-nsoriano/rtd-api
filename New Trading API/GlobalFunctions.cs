using New_Trading_API.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Xml;

using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Newtonsoft.Json;

namespace New_Trading_API
{
    public class GlobalFunctions : IHttpModule
    {
        private static readonly UTF8Encoding Encoder = new UTF8Encoding();
        /// <summary>
        /// You will need to configure this module in the Web.config file of your
        /// web and register it with IIS before being able to use it. For more information
        /// see the following link: http://go.microsoft.com/?linkid=8101007
        /// </summary>
        #region IHttpModule Members

        public void Dispose()
        {
            //clean-up code here.
        }

        public void Init(HttpApplication context)
        {
            // Below is an example of how you can handle LogRequest event and provide 
            // custom logging implementation for it
            context.LogRequest += new EventHandler(OnLogRequest);
        }

        #endregion

        public void OnLogRequest(Object source, EventArgs e)
        {
            //custom logging logic can go here
        }

        public static string Encrypt(string unencrypted)
        {
            if (string.IsNullOrEmpty(unencrypted))
                return string.Empty;

            try
            {
                var encryptedBytes = MachineKey.Protect(Encoder.GetBytes(unencrypted));

                if (encryptedBytes != null && encryptedBytes.Length > 0)
                    return HttpServerUtility.UrlTokenEncode(encryptedBytes);
            }
            catch (Exception)
            {
                return string.Empty;
            }

            return string.Empty;
        }

        public static string Decrypt(string encrypted)
        {
            if (string.IsNullOrEmpty(encrypted))
                return string.Empty;

            try
            {
                var bytes = HttpServerUtility.UrlTokenDecode(encrypted);
                if (bytes != null && bytes.Length > 0)
                {
                    var decryptedBytes = MachineKey.Unprotect(bytes);
                    if (decryptedBytes != null && decryptedBytes.Length > 0)
                        return Encoder.GetString(decryptedBytes);
                }

            }
            catch (Exception)
            {
                return string.Empty;
            }

            return string.Empty;
        }

        /// <summary>
        /// public variable for PI data archive Server Name
        /// </summary>
        public static string piServerName = ConfigurationManager.AppSettings["PIServerName"];

        /// <summary>
        /// public variable for AF Server Name
        /// </summary>
        public static string serverName = ConfigurationManager.AppSettings["AssetServerName"];

        public static string HostServerName = ConfigurationManager.AppSettings["HostServerName"];

        /// <summary>
        /// Public variable for AF Database
        /// </summary>
        public static string dbName = ConfigurationManager.AppSettings["AssetDatabaseName"];

        public static int ActualUnder = Convert.ToInt16(ConfigurationManager.AppSettings["ActualUnder"]);

        public static int ActualOver = Convert.ToInt16(ConfigurationManager.AppSettings["ActualOver"]);

        public static string invoke(string operation, string url, string certName, string password, string friendlyName, string xmlFile, string path)
        {
            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

                // Create an Uri object.
                Uri uriSAF = new Uri(url);

                // Create a request to the Uir assigned.
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                ServicePointManager.Expect100Continue = true;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
                //ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uriSAF);
                request.Headers.Add("SOAPAction", operation);
                request.ContentType = "text/xml;charset=\"utf-8\"";
                request.Accept = "text/xml";
                request.Method = "POST";
                request.KeepAlive = false;
                request.ProtocolVersion = HttpVersion.Version10;

                if (!File.Exists(certName))
                {
                    return $"Certificate file not found: {certName}";
                }
                int certSwitch = 1;
                if ((certName != "") && (password != ""))
                {
                    certSwitch = 2;
                }

                X509Certificate2 cert = null;
                switch (certSwitch)
                {
                    case 1:
                        X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                        store.Open(OpenFlags.ReadOnly);
                        foreach (X509Certificate2 cert2 in store.Certificates)
                        {
                            if (0 == friendlyName.CompareTo(cert2.FriendlyName))
                            {
                                cert = cert2;
                                break;
                            }
                        }
                        if (cert == null)
                        {
                            //Console.WriteLine("ERROR: Certificate with specified friendly name doesn't exist.");
                            return "Certificate with specified friendly name doesn't exist.";
                        }
                        break;
                    case 2:
                        SecureString passwordSS = new SecureString();

                        foreach (char ch in password)
                            passwordSS.AppendChar(ch);

                        

                        cert = new X509Certificate2(certName, passwordSS);
                        break;
                }

                try
                {
                    request.ClientCertificates.Add(cert);
                    request.PreAuthenticate = true;
                }
                catch (Exception e)
                {
                    return e.Message.ToString();
                }


                // ----- Import certificate ----- //
                //X509Certificate2Collection certificates = new X509Certificate2Collection();
                //try
                //{
                //    certificates.Import(certName, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
                //}
                //catch
                //{
                //    Console.WriteLine("Exception.");
                //    return;
                //}
                //request.ClientCertificates = certificates;
                // ----- end ----- //

                // ----- Generate the soap message ----- //
                string text = @"<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/""><soap:Body>";
                try
                {
                    XmlDocument exportResultsDoc = new XmlDocument();
                    exportResultsDoc.Load(xmlFile);
                    XmlNode root = exportResultsDoc.DocumentElement;
                    text += root.OuterXml;
                }
                catch
                {
                    //Console.WriteLine("ERROR: Failed to load xml file.");
                    return "Failed to load xml file.";
                }
                text += @"</soap:Body></soap:Envelope>";

                //Console.WriteLine("INFO: SOAP message was generated.");

                XmlDocument soapEnvelopeXml = new XmlDocument();
                soapEnvelopeXml.LoadXml(text);

                using (Stream stream = request.GetRequestStream())
                {
                    soapEnvelopeXml.Save(stream);
                }

                //Console.WriteLine("INFO: Requesting...");

                string msg = "";
                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        int counter = 0;
                        string ln;

                        while ((ln = rd.ReadLine()) != null)
                        {
                            if (counter == 5)
                            {
                                msg = ln.Replace("<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\"><SOAP-ENV:Header></SOAP-ENV:Header><SOAP-ENV:Body><Status xmlns=\"\"><Message>", "")
                                         .Replace("</Message></Status></SOAP-ENV:Body></SOAP-ENV:Envelope>", "")
                                         .Replace("SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\"><SOAP-ENV:Header></SOAP-ENV:Header><SOAP-ENV:Body><SOAP-ENV:Fault><faultcode xmlns=\"\">CRUDPEMCPLUGIN_DC_ADD_FAILED</faultcode><faultstring xmlns=\"\">", "")
                                         .Replace("</faultstring><faultactor xmlns=\"\">SiemsnSAF</faultactor></SOAP-ENV:Fault></SOAP-ENV:Body></SOAP-ENV:Envelope>", "");
                            }
                            counter++;
                        }
                        rd.Close();

                        //string soapResult = rd.ReadToEnd();

                        //StreamWriter sw = null;
                        //try
                        //{
                        //    sw = new StreamWriter(path);
                        //}
                        //catch (Exception e)
                        //{
                        //    //MessageBox.Show(e.Message);
                        //    return (0);
                        //}

                        //sw.Write(soapResult);
                        //sw.Close();
                        //Console.WriteLine("INFO: Response was saved in submitBidResponse.xml.");
                    }
                }
                return msg;

            }
            catch (Exception e)
            {
                //return "Failed";
                Exception baseException = e.GetBaseException();
                return $"Base Exception: {baseException.Message} | StackTrace: {baseException.StackTrace}";
            }

        }

        public static string GetUser(string DomainUser)
        {
            string user = "";
            user = DomainUser;
            user = user.Replace(System.Environment.MachineName + "\\", "");
            user = user.Replace(System.Environment.MachineName + @"\\", "");
            user = user.Replace(System.Environment.UserDomainName + @"\\", "");
            user = user.Replace(System.Environment.UserDomainName + @"\", "");

            return user;
        }

        public static DataTable GetExcelData(string StrQuery, string strUrl)
        {
            System.Data.DataTable dt = new System.Data.DataTable();
            try
            {
                string fileName = @strUrl;
                string ConStr = string.Format("Provider=Microsoft.ACE.OLEDB.16.0;Data Source ={0};Extended Properties = \"Excel 12.0;HDR=Yes;IMEX=2\";", fileName);
                OleDbConnection con = new OleDbConnection(ConStr);
                con.Open();
                OleDbCommand ad = new OleDbCommand(StrQuery, con);
                OleDbDataReader dr = ad.ExecuteReader();
                dt.Load(dr);
                con.Close();
            }
            catch
            {
                string fileName = @strUrl;
                string ConStr = string.Format("Provider=Microsoft.ACE.OLEDB.12.0;Data Source ={0};Extended Properties = \"Excel 12.0;HDR=Yes;IMEX=2\";", fileName);
                OleDbConnection con = new OleDbConnection(ConStr);
                con.Open();
                OleDbCommand ad = new OleDbCommand(StrQuery, con);
                OleDbDataReader dr = ad.ExecuteReader();
                dt.Load(dr);
                con.Close();
            }
            return dt;
        }

        public static List<Bid> ReadExcel(string FilePath,string UnitNumber)
        {
            List<Bid> bid = new List<Bid>();
            string fileName = Path.GetFileNameWithoutExtension(FilePath);

            IWorkbook workbook = null;
            FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read);

            if (FilePath.IndexOf(".xlsx") > 0)
                workbook = new XSSFWorkbook(fs);

            else if (FilePath.IndexOf(".xls") > 0)
                workbook = new HSSFWorkbook(fs);

            else if (FilePath.IndexOf(".XLS") > 0)
                workbook = new HSSFWorkbook(fs);
            //First sheet

            ISheet sheet = workbook.GetSheet("Sheet1");

            if (sheet != null)
            {
                int rowCount = 25;
                for (int i = 2; i <= rowCount; i++)
                {
                    IRow curRow = sheet.GetRow(i);
                    IRow rr1 = sheet.GetRow(2);
                    IRow rr2 = sheet.GetRow(3);
                    IRow rr3 = sheet.GetRow(4);
                    IRow rr4 = sheet.GetRow(5);
                    IRow rr5 = sheet.GetRow(6);
                    //Date Column
                    int col = 1;
                    bid.Add(new Bid
                    {
                        Date = Convert.ToDateTime(GetCellDateValue(curRow, rowCount, col)),
                        Interval = Convert.ToInt16(GetCellValue(curRow, rowCount, col++)),
                        ResourceID = UnitNumber,
                        ProducType = "EN",

                        P1 = GetCellValue(curRow, rowCount, col++),
                        Q1 = GetCellValue(curRow, rowCount, col++),

                        P2 = GetCellValue(curRow, rowCount, col++),
                        Q2 = GetCellValue(curRow, rowCount, col++),

                        P3 = GetCellValue(curRow, rowCount, col++),
                        Q3 = GetCellValue(curRow, rowCount, col++),

                        P4 = GetCellValue(curRow, rowCount, col++),
                        Q4 = GetCellValue(curRow, rowCount, col++),

                        P5 = GetCellValue(curRow, rowCount, col++),
                        Q5 = GetCellValue(curRow, rowCount, col++),

                        P6 = GetCellValue(curRow, rowCount, col++),
                        Q6 = GetCellValue(curRow, rowCount, col++),

                        P7 = GetCellValue(curRow, rowCount, col++),
                        Q7 = GetCellValue(curRow, rowCount, col++),

                        P8 = GetCellValue(curRow, rowCount, col++),
                        Q8 = GetCellValue(curRow, rowCount, col++),

                        P9 = GetCellValue(curRow, rowCount, col++),
                        Q9 = GetCellValue(curRow, rowCount, col++),

                        P10 = GetCellValue(curRow, rowCount, col++),
                        Q10 = GetCellValue(curRow, rowCount, col++),

                        P11 = GetCellValue(curRow, rowCount, col++),
                        Q11 = GetCellValue(curRow, rowCount, col++),

                        AS_RU_Q1 = GetCellValue(curRow, rowCount, col++),
                        AS_RU_P1 = GetCellValue(curRow, rowCount, col++),

                        AS_RU_Q2 = GetCellValue(curRow, rowCount, col++),
                        AS_RU_P2 = GetCellValue(curRow, rowCount, col++),

                        AS_RU_Q3 = GetCellValue(curRow, rowCount, col++),
                        AS_RU_P3 = GetCellValue(curRow, rowCount, col++),

                        AS_RU_Q4 = GetCellValue(curRow, rowCount, col++),
                        AS_RU_P4 = GetCellValue(curRow, rowCount, col++),

                        AS_RU_Q5 = GetCellValue(curRow, rowCount, col++),
                        AS_RU_P5 = GetCellValue(curRow, rowCount, col++),

                        AS_RD_Q1 = GetCellValue(curRow, rowCount, col++),
                        AS_RD_P1 = GetCellValue(curRow, rowCount, col++),

                        AS_RD_Q2 = GetCellValue(curRow, rowCount, col++),
                        AS_RD_P2 = GetCellValue(curRow, rowCount, col++),

                        AS_RD_Q3 = GetCellValue(curRow, rowCount, col++),
                        AS_RD_P3 = GetCellValue(curRow, rowCount, col++),

                        AS_RD_Q4 = GetCellValue(curRow, rowCount, col++),
                        AS_RD_P4 = GetCellValue(curRow, rowCount, col++),

                        AS_RD_Q5 = GetCellValue(curRow, rowCount, col++),
                        AS_RD_P5 = GetCellValue(curRow, rowCount, col++),

                        AS_FR_Q1 = GetCellValue(curRow, rowCount, col++),
                        AS_FR_P1 = GetCellValue(curRow, rowCount, col++),

                        AS_FR_Q2 = GetCellValue(curRow, rowCount, col++),
                        AS_FR_P2 = GetCellValue(curRow, rowCount, col++),

                        AS_FR_Q3 = GetCellValue(curRow, rowCount, col++),
                        AS_FR_P3 = GetCellValue(curRow, rowCount, col++),

                        AS_FR_Q4 = GetCellValue(curRow, rowCount, col++),
                        AS_FR_P4 = GetCellValue(curRow, rowCount, col++),

                        AS_FR_Q5 = GetCellValue(curRow, rowCount, col++),
                        AS_FR_P5 = GetCellValue(curRow, rowCount, col++),

                        AS_DR_Q1 = GetCellValue(curRow, rowCount, col++),
                        AS_DR_P1 = GetCellValue(curRow, rowCount, col++),

                        AS_DR_Q2 = GetCellValue(curRow, rowCount, col++),
                        AS_DR_P2 = GetCellValue(curRow, rowCount, col++),

                        AS_DR_Q3 = GetCellValue(curRow, rowCount, col++),
                        AS_DR_P3 = GetCellValue(curRow, rowCount, col++),

                        AS_DR_Q4 = GetCellValue(curRow, rowCount, col++),
                        AS_DR_P4 = GetCellValue(curRow, rowCount, col++),

                        AS_DR_Q5 = GetCellValue(curRow, rowCount, col++),
                        AS_DR_P5 = GetCellValue(curRow, rowCount, col++),

                        RampQuantity1 = GetCellValue(rr1, rowCount, 3),
                        RRU1 = GetCellValue(rr1, rowCount, 3),
                        RRD1 = GetCellValue(rr1, rowCount, 3),

                        RampQuantity2 = GetCellValue(rr2, rowCount, 66),
                        RRU2 = GetCellValue(rr2, rowCount, 67),
                        RRD2 = GetCellValue(rr2, rowCount, 68),

                        RampQuantity3 = GetCellValue(rr3, rowCount, 66),
                        RRU3 = GetCellValue(rr3, rowCount, 67),
                        RRD3 = GetCellValue(rr3, rowCount, 68),

                        RampQuantity4 = GetCellValue(rr4, rowCount, 66),
                        RRU4 = GetCellValue(rr4, rowCount, 67),
                        RRD4 = GetCellValue(rr4, rowCount, 68),

                        RampQuantity5 = GetCellValue(rr5, rowCount, 66),
                        RRU5 = GetCellValue(rr5, rowCount, 67),
                        RRD5 = GetCellValue(rr5, rowCount, 68)
                    });

                }
            }


            return bid;
        }

        public static List<BidDataTable> ReadExcelForDataTable(string FilePath, string UnitNumber)
        {
            List<BidDataTable> bid = new List<BidDataTable>();
            string fileName = Path.GetFileNameWithoutExtension(FilePath);

            IWorkbook workbook = null;
            FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read);

            if (FilePath.IndexOf(".xlsx") > 0)
                workbook = new XSSFWorkbook(fs);

            else if (FilePath.IndexOf(".xls") > 0)
                workbook = new HSSFWorkbook(fs);

            else if (FilePath.IndexOf(".XLS") > 0)
                workbook = new HSSFWorkbook(fs);
            //First sheet

            ISheet sheet = workbook.GetSheet("Sheet1");

            if (sheet != null)
            {
                int rowCount = 25;
                for (int i = 2; i <= rowCount; i++)
                {
                    IRow curRow = sheet.GetRow(i);
                    //Date Column
                    int col = 1;
                    bid.Add(new BidDataTable
                    {
                        Date = Convert.ToDateTime(GetCellDateValue(curRow, rowCount, col)),
                        Hour = Convert.ToInt16(GetCellValue(curRow, rowCount, col++)),

                        P1 = GetCellValue(curRow, rowCount, col++),
                        Q1 = GetCellValue(curRow, rowCount, col++),

                        P2 = GetCellValue(curRow, rowCount, col++),
                        Q2 = GetCellValue(curRow, rowCount, col++),

                        P3 = GetCellValue(curRow, rowCount, col++),
                        Q3 = GetCellValue(curRow, rowCount, col++),

                        P4 = GetCellValue(curRow, rowCount, col++),
                        Q4 = GetCellValue(curRow, rowCount, col++),

                        P5 = GetCellValue(curRow, rowCount, col++),
                        Q5 = GetCellValue(curRow, rowCount, col++),

                        P6 = GetCellValue(curRow, rowCount, col++),
                        Q6 = GetCellValue(curRow, rowCount, col++),

                        P7 = GetCellValue(curRow, rowCount, col++),
                        Q7 = GetCellValue(curRow, rowCount, col++),

                        P8 = GetCellValue(curRow, rowCount, col++),
                        Q8 = GetCellValue(curRow, rowCount, col++),

                        P9 = GetCellValue(curRow, rowCount, col++),
                        Q9 = GetCellValue(curRow, rowCount, col++),

                        P10 = GetCellValue(curRow, rowCount, col++),
                        Q10 = GetCellValue(curRow, rowCount, col++),

                        P11 = GetCellValue(curRow, rowCount, col++),
                        Q11 = GetCellValue(curRow, rowCount, col++),

                        AS_RU_Q1 = GetCellValue(curRow, rowCount, col++),
                        AS_RU_P1 = GetCellValue(curRow, rowCount, col++),

                        AS_RU_Q2 = GetCellValue(curRow, rowCount, col++),
                        AS_RU_P2 = GetCellValue(curRow, rowCount, col++),

                        AS_RU_Q3 = GetCellValue(curRow, rowCount, col++),
                        AS_RU_P3 = GetCellValue(curRow, rowCount, col++),

                        AS_RU_Q4 = GetCellValue(curRow, rowCount, col++),
                        AS_RU_P4 = GetCellValue(curRow, rowCount, col++),

                        AS_RU_Q5 = GetCellValue(curRow, rowCount, col++),
                        AS_RU_P5 = GetCellValue(curRow, rowCount, col++),

                        AS_RD_Q1 = GetCellValue(curRow, rowCount, col++),
                        AS_RD_P1 = GetCellValue(curRow, rowCount, col++),

                        AS_RD_Q2 = GetCellValue(curRow, rowCount, col++),
                        AS_RD_P2 = GetCellValue(curRow, rowCount, col++),

                        AS_RD_Q3 = GetCellValue(curRow, rowCount, col++),
                        AS_RD_P3 = GetCellValue(curRow, rowCount, col++),

                        AS_RD_Q4 = GetCellValue(curRow, rowCount, col++),
                        AS_RD_P4 = GetCellValue(curRow, rowCount, col++),

                        AS_RD_Q5 = GetCellValue(curRow, rowCount, col++),
                        AS_RD_P5 = GetCellValue(curRow, rowCount, col++),

                        AS_FR_Q1 = GetCellValue(curRow, rowCount, col++),
                        AS_FR_P1 = GetCellValue(curRow, rowCount, col++),

                        AS_FR_Q2 = GetCellValue(curRow, rowCount, col++),
                        AS_FR_P2 = GetCellValue(curRow, rowCount, col++),

                        AS_FR_Q3 = GetCellValue(curRow, rowCount, col++),
                        AS_FR_P3 = GetCellValue(curRow, rowCount, col++),

                        AS_FR_Q4 = GetCellValue(curRow, rowCount, col++),
                        AS_FR_P4 = GetCellValue(curRow, rowCount, col++),

                        AS_FR_Q5 = GetCellValue(curRow, rowCount, col++),
                        AS_FR_P5 = GetCellValue(curRow, rowCount, col++),

                        AS_DR_Q1 = GetCellValue(curRow, rowCount, col++),
                        AS_DR_P1 = GetCellValue(curRow, rowCount, col++),

                        AS_DR_Q2 = GetCellValue(curRow, rowCount, col++),
                        AS_DR_P2 = GetCellValue(curRow, rowCount, col++),

                        AS_DR_Q3 = GetCellValue(curRow, rowCount, col++),
                        AS_DR_P3 = GetCellValue(curRow, rowCount, col++),

                        AS_DR_Q4 = GetCellValue(curRow, rowCount, col++),
                        AS_DR_P4 = GetCellValue(curRow, rowCount, col++),

                        AS_DR_Q5 = GetCellValue(curRow, rowCount, col++),
                        AS_DR_P5 = GetCellValue(curRow, rowCount, col++),

                        RampRate = "",
                        MW = GetCellValue(curRow, rowCount, 65),
                        RU = GetCellValue(curRow, rowCount, 66),

                        RD = GetCellValue(curRow, rowCount, 67),
                    });

                }
            }


            return bid;
        }

        public static string GetCellDateValue(IRow row, int rowCount, int colCount)
        {
            string value = "";
            if (row.GetCell(colCount).CellType == CellType.Numeric)
            {
                value = Convert.ToString(row.GetCell(colCount).NumericCellValue);
                value = value.Substring(4, 2) + "/" + value.Substring(6, 2) + "/" + value.Substring(0, 4);
            }

            return value;
        }

        public static Nullable<double> GetCellValue(IRow row, int rowCount, int colCount)
        {
            Nullable<double> value = null;
            if (row.GetCell(colCount).CellType == CellType.Numeric)
            {
                value = row.GetCell(colCount).NumericCellValue;
            }
            return value;
        }

        public static string CreateOfferXML(List<BidDataTable> bid, string Tpuser, string Path, string UnitNumber)
        {
            string message = "";
            try
            {
                DataTable dt = new DataTable();
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(bid);
                dt = JsonConvert.DeserializeObject<DataTable>(json);

                XmlTextWriter writer = new XmlTextWriter(Path, System.Text.Encoding.UTF8);
                writer.WriteStartDocument(true);
                writer.Formatting = System.Xml.Formatting.Indented;
                writer.Indentation = 2;

                writer.WriteStartElement("m:RawBidSet");
                writer.WriteAttributeString("xmlns:m", "http://pemc/soa/RawBidSet.xsd");
                writer.WriteAttributeString("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                writer.WriteAttributeString("xsi:schemaLocation", "http://pemc/soa/RawBidSet.xsd RawBidSet.xsd");
                //Start MessageHeader
                writer.WriteStartElement("m:MessageHeader");
                writer.WriteStartElement("m:TimeDate");
                writer.WriteString(DateTime.UtcNow.ToString("s") + "Z");
                writer.WriteEndElement();

                writer.WriteStartElement("m:Source");
                writer.WriteString("DEFAULT");
                writer.WriteEndElement();

                writer.WriteEndElement();
                //End MessageHeader

                //Start MessagePayload
                writer.WriteStartElement("m:MessagePayload");
                writer.WriteStartElement("m:GeneratingBid");

                writer.WriteStartElement("m:startTime");
                writer.WriteString(DateTime.Parse(dt.Rows[1][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                writer.WriteEndElement();

                writer.WriteStartElement("m:stopTime");
                writer.WriteString(DateTime.Parse(dt.Rows[1][0].ToString().Replace(" 12:00:00 AMT", "")).AddDays(1).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                writer.WriteEndElement();

                writer.WriteStartElement("m:RegisteredGenerator");
                writer.WriteStartElement("m:mrid");
                writer.WriteString(UnitNumber);
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("m:MarketParticipant");
                writer.WriteStartElement("m:mrid");
                writer.WriteString(Tpuser);
                writer.WriteEndElement();
                writer.WriteEndElement();
                //try
                //{

                //start ProductBid
                writer.WriteStartElement("m:ProductBid");

                writer.WriteStartElement("m:MarketProduct");
                writer.WriteStartElement("m:marketProductType");
                writer.WriteString("EN");
                writer.WriteEndElement();
                writer.WriteEndElement();

                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    //start BidSchedule
                    writer.WriteStartElement("m:BidSchedule");

                    writer.WriteStartElement("m:timeIntervalStart");
                    string timeFromStr;
                    if ((Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString().Length == 1)
                    {
                        timeFromStr = "T0" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                    }
                    else
                    {
                        timeFromStr = "T" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                    }
                    writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeFromStr + ":00:00.000+08:00");
                    writer.WriteEndElement();

                    string timeToStr;
                    writer.WriteStartElement("m:timeIntervalEnd");
                    if (dt.Rows[i][1].ToString() == "24")
                    {
                        writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).AddDays(1).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                    }
                    else
                    {
                        if (dt.Rows[i][1].ToString().Length == 1)
                        {
                            timeToStr = "T0" + dt.Rows[i][1].ToString();
                        }
                        else
                        {
                            timeToStr = "T" + dt.Rows[i][1].ToString();
                        }
                        writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeToStr + ":00:00.000+08:00");
                    }
                    writer.WriteEndElement();
                    //start BidPriceCurve
                    writer.WriteStartElement("m:BidPriceCurve");

                    for (int x = 2; x <= 22; x += 2)
                    {
                        if (dt.Rows[i][x].ToString() != "")
                        {
                            //start CurveSchedData
                            writer.WriteStartElement("m:CurveSchedData");

                            writer.WriteStartElement("m:xAxisData");
                            writer.WriteString(dt.Rows[i][x + 1].ToString());
                            writer.WriteEndElement();
                            writer.WriteStartElement("m:y1AxisData");
                            writer.WriteString(dt.Rows[i][x].ToString());
                            writer.WriteEndElement();

                            writer.WriteEndElement();
                            //End CurveSchedData
                        }
                    }

                    writer.WriteEndElement();
                    //end BidPriceCurve
                    writer.WriteEndElement();
                    //end BidSchedule
                }
                writer.WriteEndElement();
                //end ProductBid

                int ru = 0;
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][24].ToString() != "")
                    {
                        ru = ru + 1;
                    }
                }

                if (ru > 0)
                {
                    //start ProductBid
                    writer.WriteStartElement("m:ProductBid");

                    writer.WriteStartElement("m:MarketProduct");
                    writer.WriteStartElement("m:marketProductType");
                    writer.WriteString("RU");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                // AS Product Type
                // AS RU
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][24].ToString() != "")
                    {


                        //start BidSchedule
                        writer.WriteStartElement("m:BidSchedule");

                        writer.WriteStartElement("m:timeIntervalStart");
                        string timeFromStr;
                        if ((Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString().Length == 1)
                        {
                            timeFromStr = "T0" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        else
                        {
                            timeFromStr = "T" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeFromStr + ":00:00.000+08:00");
                        writer.WriteEndElement();

                        string timeToStr;
                        writer.WriteStartElement("m:timeIntervalEnd");
                        if (dt.Rows[i][1].ToString() == "24")
                        {
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).AddDays(1).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                        }
                        else
                        {
                            if (dt.Rows[i][1].ToString().Length == 1)
                            {
                                timeToStr = "T0" + dt.Rows[i][1].ToString();
                            }
                            else
                            {
                                timeToStr = "T" + dt.Rows[i][1].ToString();
                            }
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeToStr + ":00:00.000+08:00");
                        }
                        writer.WriteEndElement();
                        //start BidPriceCurve
                        writer.WriteStartElement("m:BidPriceCurve");

                        for (int x = 24; x <= 32; x += 2)
                        {
                            if (dt.Rows[i][x].ToString() != "")
                            {
                                //start CurveSchedData
                                writer.WriteStartElement("m:CurveSchedData");

                                writer.WriteStartElement("m:xAxisData");
                                writer.WriteString(dt.Rows[i][x + 1].ToString());
                                writer.WriteEndElement();

                                writer.WriteStartElement("m:y1AxisData");
                                writer.WriteString(dt.Rows[i][x].ToString());
                                writer.WriteEndElement();

                                writer.WriteEndElement();
                                //End CurveSchedData
                            }
                        }


                        writer.WriteEndElement();
                        //end BidPriceCurve
                        writer.WriteEndElement();
                        //end BidSchedule

                    }
                }

                if (ru > 0)
                {
                    writer.WriteEndElement();
                    //end ProductBid
                }



                int rd = 0;
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][34].ToString() != "")
                    {
                        rd = rd + 1;
                    }
                }

                if (rd > 0)
                {
                    //start ProductBid
                    writer.WriteStartElement("m:ProductBid");

                    writer.WriteStartElement("m:MarketProduct");
                    writer.WriteStartElement("m:marketProductType");
                    writer.WriteString("RD");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                // AS Product Type
                // AS RD
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][34].ToString() != "")
                    {

                        //start BidSchedule
                        writer.WriteStartElement("m:BidSchedule");

                        writer.WriteStartElement("m:timeIntervalStart");
                        string timeFromStr;
                        if ((Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString().Length == 1)
                        {
                            timeFromStr = "T0" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        else
                        {
                            timeFromStr = "T" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeFromStr + ":00:00.000+08:00");
                        writer.WriteEndElement();

                        string timeToStr;
                        writer.WriteStartElement("m:timeIntervalEnd");
                        if (dt.Rows[i][1].ToString() == "24")
                        {
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).AddDays(1).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                        }
                        else
                        {
                            if (dt.Rows[i][1].ToString().Length == 1)
                            {
                                timeToStr = "T0" + dt.Rows[i][1].ToString();
                            }
                            else
                            {
                                timeToStr = "T" + dt.Rows[i][1].ToString();
                            }
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeToStr + ":00:00.000+08:00");
                        }
                        writer.WriteEndElement();
                        //start BidPriceCurve
                        writer.WriteStartElement("m:BidPriceCurve");

                        for (int x = 34; x <= 42; x += 2)
                        {
                            if (dt.Rows[i][x].ToString() != "")
                            {
                                //start CurveSchedData
                                writer.WriteStartElement("m:CurveSchedData");

                                writer.WriteStartElement("m:xAxisData");
                                writer.WriteString(dt.Rows[i][x + 1].ToString());
                                writer.WriteEndElement();

                                writer.WriteStartElement("m:y1AxisData");
                                writer.WriteString(dt.Rows[i][x].ToString());
                                writer.WriteEndElement();

                                writer.WriteEndElement();
                                //End CurveSchedData
                            }
                        }


                        writer.WriteEndElement();
                        //end BidPriceCurve
                        writer.WriteEndElement();
                        //end BidSchedule
                    }
                }

                if (rd > 0)
                {
                    writer.WriteEndElement();
                    //end ProductBid
                }


                int fr = 0;
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][44].ToString() != "")
                    {
                        fr = fr + 1;
                    }
                }

                if (fr > 0)
                {
                    //start ProductBid
                    writer.WriteStartElement("m:ProductBid");

                    writer.WriteStartElement("m:MarketProduct");
                    writer.WriteStartElement("m:marketProductType");
                    writer.WriteString("FR");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }


                // AS Product Type
                // AS FR
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][44].ToString() != "")
                    {
                        //start BidSchedule
                        writer.WriteStartElement("m:BidSchedule");

                        writer.WriteStartElement("m:timeIntervalStart");
                        string timeFromStr;
                        if ((Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString().Length == 1)
                        {
                            timeFromStr = "T0" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        else
                        {
                            timeFromStr = "T" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeFromStr + ":00:00.000+08:00");
                        writer.WriteEndElement();

                        string timeToStr;
                        writer.WriteStartElement("m:timeIntervalEnd");
                        if (dt.Rows[i][1].ToString() == "24")
                        {
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).AddDays(1).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                        }
                        else
                        {
                            if (dt.Rows[i][1].ToString().Length == 1)
                            {
                                timeToStr = "T0" + dt.Rows[i][1].ToString();
                            }
                            else
                            {
                                timeToStr = "T" + dt.Rows[i][1].ToString();
                            }
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeToStr + ":00:00.000+08:00");
                        }
                        writer.WriteEndElement();
                        //start BidPriceCurve
                        writer.WriteStartElement("m:BidPriceCurve");

                        for (int x = 44; x <= 52; x += 2)
                        {
                            if (dt.Rows[i][x].ToString() != "")
                            {
                                //start CurveSchedData
                                writer.WriteStartElement("m:CurveSchedData");

                                writer.WriteStartElement("m:xAxisData");
                                writer.WriteString(dt.Rows[i][x + 1].ToString());
                                writer.WriteEndElement();

                                writer.WriteStartElement("m:y1AxisData");
                                writer.WriteString(dt.Rows[i][x].ToString());
                                writer.WriteEndElement();

                                writer.WriteEndElement();
                                //End CurveSchedData
                            }
                        }


                        writer.WriteEndElement();
                        //end BidPriceCurve
                        writer.WriteEndElement();
                        //end BidSchedule

                    }
                }

                if (fr > 0)
                {
                    writer.WriteEndElement();
                    //end ProductBid
                }


                int dr = 0;
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][54].ToString() != "")
                    {
                        dr = dr + 1;
                    }
                }

                if (dr > 0)
                {
                    //start ProductBid
                    writer.WriteStartElement("m:ProductBid");

                    writer.WriteStartElement("m:MarketProduct");
                    writer.WriteStartElement("m:marketProductType");
                    writer.WriteString("DR");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }


                // AS Product Type
                // AS DR
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][54].ToString() != "")
                    {
                        //start BidSchedule
                        writer.WriteStartElement("m:BidSchedule");

                        writer.WriteStartElement("m:timeIntervalStart");
                        string timeFromStr;
                        if ((Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString().Length == 1)
                        {
                            timeFromStr = "T0" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        else
                        {
                            timeFromStr = "T" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeFromStr + ":00:00.000+08:00");
                        writer.WriteEndElement();

                        string timeToStr;
                        writer.WriteStartElement("m:timeIntervalEnd");
                        if (dt.Rows[i][1].ToString() == "24")
                        {
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).AddDays(1).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                        }
                        else
                        {
                            if (dt.Rows[i][1].ToString().Length == 1)
                            {
                                timeToStr = "T0" + dt.Rows[i][1].ToString();
                            }
                            else
                            {
                                timeToStr = "T" + dt.Rows[i][1].ToString();
                            }
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeToStr + ":00:00.000+08:00");
                        }
                        writer.WriteEndElement();
                        //start BidPriceCurve
                        writer.WriteStartElement("m:BidPriceCurve");

                        for (int x = 54; x <= 62; x += 2)
                        {
                            if (dt.Rows[i][x].ToString() != "")
                            {
                                //start CurveSchedData
                                writer.WriteStartElement("m:CurveSchedData");

                                writer.WriteStartElement("m:xAxisData");
                                writer.WriteString(dt.Rows[i][x + 1].ToString());
                                writer.WriteEndElement();

                                writer.WriteStartElement("m:y1AxisData");
                                writer.WriteString(dt.Rows[i][x].ToString());
                                writer.WriteEndElement();

                                writer.WriteEndElement();
                                //End CurveSchedData
                            }
                        }


                        writer.WriteEndElement();
                        //end BidPriceCurve
                        writer.WriteEndElement();
                        //end BidSchedule

                    }
                }
                if (dr > 0)
                {
                    writer.WriteEndElement();
                    //end ProductBid
                }



                //start RampRateCurve
                writer.WriteStartElement("m:RampRateCurve");
                writer.WriteStartElement("m:description");
                writer.WriteString("RAMP_RATE");
                writer.WriteEndElement();

                if (dt.Rows[0][65].ToString() != "" && dt.Rows[0][66].ToString() != "" && dt.Rows[0][67].ToString() != "")
                {
                    writer.WriteStartElement("m:CurveData");
                    writer.WriteStartElement("m:xAxisData");
                    writer.WriteString(dt.Rows[0][65].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y1AxisData");
                    writer.WriteString(dt.Rows[0][66].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y2AxisData");
                    writer.WriteString(dt.Rows[0][67].ToString());
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                if (dt.Rows[1][65].ToString() != "" && dt.Rows[1][66].ToString() != "" && dt.Rows[1][67].ToString() != "")
                {
                    writer.WriteStartElement("m:CurveData");
                    writer.WriteStartElement("m:xAxisData");
                    writer.WriteString(dt.Rows[1][65].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y1AxisData");
                    writer.WriteString(dt.Rows[1][66].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y2AxisData");
                    writer.WriteString(dt.Rows[1][67].ToString());
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                if (dt.Rows[2][65].ToString() != "" && dt.Rows[2][66].ToString() != "" && dt.Rows[2][67].ToString() != "")
                {
                    writer.WriteStartElement("m:CurveData");
                    writer.WriteStartElement("m:xAxisData");
                    writer.WriteString(dt.Rows[2][65].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y1AxisData");
                    writer.WriteString(dt.Rows[2][66].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y2AxisData");
                    writer.WriteString(dt.Rows[2][67].ToString());
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                if (dt.Rows[3][65].ToString() != "" && dt.Rows[3][66].ToString() != "" && dt.Rows[3][67].ToString() != "")
                {
                    writer.WriteStartElement("m:CurveData");
                    writer.WriteStartElement("m:xAxisData");
                    writer.WriteString(dt.Rows[3][65].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y1AxisData");
                    writer.WriteString(dt.Rows[3][66].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y2AxisData");
                    writer.WriteString(dt.Rows[3][67].ToString());
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                if (dt.Rows[4][65].ToString() != "" && dt.Rows[4][66].ToString() != "" && dt.Rows[4][67].ToString() != "")
                {
                    writer.WriteStartElement("m:CurveData");
                    writer.WriteStartElement("m:xAxisData");
                    writer.WriteString(dt.Rows[4][65].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y1AxisData");
                    writer.WriteString(dt.Rows[4][66].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y2AxisData");
                    writer.WriteString(dt.Rows[4][67].ToString());
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                //end RampRateCurve


                writer.WriteEndElement();
                writer.WriteEndElement();
                //End MessagePayload
                writer.WriteEndElement();
                writer.WriteEndDocument();

                writer.Close();

                message = "xml successfully created";
            }
            catch (Exception e)
            {
                message = e.Message.ToString();
            }

            return message;
        }

        public static string CreateXMLNomination(DataTable dt, string Tpuser, string Path)
        {
            string message = "";

            try
            {
                XmlTextWriter writer = new XmlTextWriter(Path, System.Text.Encoding.UTF8);
                writer.WriteStartDocument(true);
                writer.Formatting = System.Xml.Formatting.Indented;
                writer.Indentation = 2;

                writer.WriteStartElement("m:RawBidSet");
                writer.WriteAttributeString("xmlns:m", "http://pemc/soa/RawBidSet.xsd");
                writer.WriteAttributeString("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                writer.WriteAttributeString("xsi:schemaLocation", "http://pemc/soa/RawBidSet.xsd RawBidSet.xsd");
                //Start MessageHeader
                writer.WriteStartElement("m:MessageHeader");
                writer.WriteStartElement("m:TimeDate");
                writer.WriteString(DateTime.UtcNow.ToString("s") + "Z");
                writer.WriteEndElement();

                writer.WriteStartElement("m:Source");
                writer.WriteString("DEFAULT");
                writer.WriteEndElement();

                writer.WriteEndElement();
                //End MessageHeader

                //Start MessagePayload
                writer.WriteStartElement("m:MessagePayload");
                writer.WriteStartElement("m:GeneratingBid");

                writer.WriteStartElement("m:name");
                writer.WriteString(dt.Rows[1][3].ToString());
                writer.WriteEndElement();

                writer.WriteStartElement("m:startTime");
                writer.WriteString(DateTime.Parse(dt.Rows[1][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                writer.WriteEndElement();

                writer.WriteStartElement("m:stopTime");
                writer.WriteString(DateTime.Parse(dt.Rows[1][0].ToString().Replace(" 12:00:00 AMT", "")).AddDays(1).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                writer.WriteEndElement();

                writer.WriteStartElement("m:RegisteredGenerator");
                writer.WriteStartElement("m:mrid");
                writer.WriteString(dt.Rows[1][3].ToString());
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("m:MarketParticipant");
                writer.WriteStartElement("m:mrid");
                writer.WriteString(Tpuser);
                writer.WriteEndElement();
                writer.WriteEndElement();


                string lastInterval = "";
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (lastInterval != dt.Rows[i][1].ToString())
                    {
                        //start ProductBid
                        writer.WriteStartElement("m:ProductBid");

                        //start Nomination
                        writer.WriteStartElement("m:Nomination");

                        writer.WriteStartElement("m:timeIntervalStart");
                        string timeFromStr;
                        if ((Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString().Length == 1)
                        {
                            timeFromStr = "T0" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        else
                        {
                            timeFromStr = "T" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeFromStr + ":00:00.000+08:00");
                        writer.WriteEndElement();

                        writer.WriteStartElement("m:timeIntervalEnd");
                        string timeToStr;
                        if (dt.Rows[i][1].ToString() == "24")
                        {
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).AddDays(1).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                        }
                        else
                        {
                            if (dt.Rows[i][1].ToString().Length == 1)
                            {
                                timeToStr = "T0" + dt.Rows[i][1].ToString();
                            }
                            else
                            {
                                timeToStr = "T" + dt.Rows[i][1].ToString();
                            }
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeToStr + ":00:00.000+08:00");
                        }
                        writer.WriteEndElement();

                        DataView dv = new DataView(dt);
                        dv.RowFilter = "Interval = '" + dt.Rows[i][1].ToString() + "'";
                        for (int x = 0; x <= dv.Count - 1; x++)
                        {
                            //start MinuteMW
                            writer.WriteStartElement("m:minuteMW");

                            writer.WriteStartElement("m:minuteOfHour");
                            writer.WriteString(dv[x][2].ToString());
                            writer.WriteEndElement();
                            writer.WriteStartElement("m:quantity");
                            writer.WriteString(dv[x][4].ToString());
                            writer.WriteEndElement();

                            writer.WriteEndElement();
                            //end MinuteMW
                        }

                        writer.WriteEndElement();
                        //end Nomination
                        writer.WriteEndElement();
                        //end ProductBid
                        lastInterval = dt.Rows[i][1].ToString();
                    }
                }

                writer.WriteEndElement();
                writer.WriteEndElement();
                //End MessagePayload
                writer.WriteEndElement();
                writer.WriteEndDocument();

                writer.Close();

                message = "xml successfully created";
            }
            catch (Exception e)
            {
                message = e.Message.ToString();
            }

            return message;
        }

        public static string SaveFile(string BidType, string UnitID)
        {
            string result = null;
            var httpRequest = HttpContext.Current.Request;
            if (httpRequest.Files.Count > 0)
            {
                var docfiles = new List<string>();
                foreach (string file in httpRequest.Files)
                {
                    var postedFile = httpRequest.Files[file];
                    var filePath = "";
                    if (BidType == "Offer")
                    {
                        filePath = HttpContext.Current.Server.MapPath(@"~\Files\BidOffer" + UnitID + ".xlsx");
                    }
                    else
                    {
                        filePath = HttpContext.Current.Server.MapPath(@"~\Files\BidNomination" + UnitID + ".xlsx");
                    }

                    using (FileStream fs = File.Create(filePath))
                    {

                    }
                    postedFile.SaveAs(filePath);
                    docfiles.Add(filePath);
                }
                result = "Success";
            }
            else
            {
                result = "Failed";
            }

            return result;
        }

        public static void ExecuteNonQuery(string query, params SqlParameter[] parameters)
        {
            using (var db = new TradingEntities())
            {
                var conn = db.Database.Connection;
                var cmd = conn.CreateCommand();
                cmd.CommandText = query;
                cmd.CommandType = CommandType.Text;

                if (parameters != null)
                {
                    foreach (var p in parameters)
                    {
                        cmd.Parameters.Add(p);
                    }
                }

                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        public static DataTable DataReader(string query, params SqlParameter[] parameter)
        {

            using (var db = new TradingEntities())
            {
                var dt = new DataTable();
                var conn = db.Database.Connection;
                var cmd = conn.CreateCommand();
                var str = query;
                cmd.CommandText = str;
                cmd.CommandType = CommandType.Text;

                if (parameter != null)
                {
                    foreach (var q in parameter)
                    {
                        cmd.Parameters.Add(q);
                    }
                }

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    dt.Load(reader);
                }
                conn.Close();

                return dt;
            }

        }

        public static void Log(
        string logType,
        string action,
        string tableName = null,
        string recordId = null,
        string oldValues = null,
        string newValues = null,
        string message = null,
        string userName = null)
        {
            try
            {
                var query = @"
                INSERT INTO SystemLogs
                (
                    LogType,
                    Action,
                    TableName,
                    RecordId,
                    OldValues,
                    NewValues,
                    Message,
                    UserName,
                    IPAddress,
                    Endpoint,
                    LogDate
                )
                VALUES
                (
                    @LogType,
                    @Action,
                    @TableName,
                    @RecordId,
                    @OldValues,
                    @NewValues,
                    @Message,
                    @UserName,
                    @IPAddress,
                    @Endpoint,
                    @LogDate
                )";

                ExecuteNonQuery(query,
                    new SqlParameter("@LogType", (object)logType ?? DBNull.Value),
                    new SqlParameter("@Action", (object)action ?? DBNull.Value),
                    new SqlParameter("@TableName", (object)tableName ?? DBNull.Value),
                    new SqlParameter("@RecordId", (object)recordId ?? DBNull.Value),
                    new SqlParameter("@OldValues", (object)oldValues ?? DBNull.Value),
                    new SqlParameter("@NewValues", (object)newValues ?? DBNull.Value),
                    new SqlParameter("@Message", (object)message ?? DBNull.Value),
                    new SqlParameter("@UserName", (object)userName ?? DBNull.Value),
                    new SqlParameter("@IPAddress", (object)GetIpAddress() ?? DBNull.Value),
                    new SqlParameter("@Endpoint", (object)GetEndpoint() ?? DBNull.Value),
                    new SqlParameter("@LogDate", DateTime.Now)
                );
            }
            catch
            {
                // IMPORTANT:
                // Never allow logging failure to crash your API
            }
        }

        private static string GetCurrentUser()
        {
            var context = HttpContext.Current;

            if (context?.User?.Identity?.IsAuthenticated == true)
                return context.User.Identity.Name;

            return "Anonymous";
        }

        private static string GetIpAddress()
        {
            return HttpContext.Current?.Request?.UserHostAddress;
        }

        private static string GetEndpoint()
        {
            return HttpContext.Current?.Request?.RawUrl;
        }
    }
}
