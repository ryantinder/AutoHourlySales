using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Configuration;
namespace AutoHourlySales
{
    public class DES
    {
        public static string decrypt(string target)
        {
            //Configuration > Section > Class > Text
            System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConnectionStringsSection csSection = config.ConnectionStrings;
            var settings = ConfigurationManager.ConnectionStrings[target];
            string connection = csSection.ConnectionStrings[target].ConnectionString;

            byte[] inputArray = Convert.FromBase64String(connection);
            TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider();
            tripleDES.Key = UTF8Encoding.UTF8.GetBytes("sblw-3hn8-sqoy19");
            tripleDES.Mode = CipherMode.ECB;
            tripleDES.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = tripleDES.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);
            tripleDES.Clear();

            string connectionstring = Encoding.UTF8.GetString(resultArray);

            return connectionstring;
        }
    }
}
