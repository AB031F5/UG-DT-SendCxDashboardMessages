using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UG_DT_SendCxSms.Models;

namespace UG_DT_SendCxSms.Logic
{
    public class Helpers
    {
        public static string generateUniqueID()
        {
            Random random = new Random();
            byte[] data = new byte[5];
            random.NextBytes(data);
            BigInteger myBigInt = new BigInteger(data);
            return myBigInt > 100000 ? myBigInt.ToString() : generateUniqueID();
        }

        public void writeToFile(ArrayList textContent)
        {
            Directory.CreateDirectory(LogConstants.Errorlogs);
            string fileUrl = LogConstants.Errorlogs + DateTime.Now.ToString("yyyMMdd") + ".txt";
            try
            {
                FileStream stream = new FileStream(@fileUrl, FileMode.Append, FileAccess.Write);
                StreamWriter sw = new StreamWriter(stream);
                if (textContent.Count == 0)
                {
                    sw.Close();
                    createFile(fileUrl);
                }
                else
                {
                    for (int i = 0; i < textContent.Count; i++)
                    {
                        sw.WriteLine(textContent[i].ToString() + " " + DateTime.Now.ToString());
                    }
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                //Do nothing
            }
        }
        internal void createFile(string fileUrl)
        {
            try
            {
                FileStream fs = new FileStream(@fileUrl, FileMode.Append, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);
                sw.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
