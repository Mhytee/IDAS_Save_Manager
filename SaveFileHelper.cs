using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace IDAS_Save_System
{
    public static class SaveFileHelper
    {
        public static string TryExtractPlayerName(string filePath)
        {
            try
            {
                byte[] data = File.ReadAllBytes(filePath);
                int offset = 0xF0;
                int byteLength = 12;

                if (data.Length < offset + byteLength)
                    return null;

                // Decode using Shift-JIS
                string rawStr = Encoding.GetEncoding("shift_jis").GetString(data, offset, byteLength);

                // Normalize (Shift-JIS full-width to ASCII)
                string converted = rawStr.Normalize(NormalizationForm.FormKC);

                return converted;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error extracting name: {ex.Message}");
                return null;
            }
        }
    }
}



   
