using System;
using System.IO;
using System.Linq;
using System.Text;

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

                // The card field is padded to 12 bytes with NULs/spaces; strip the
                // padding or the name always fails the invalid-filename-char check.
                converted = new string(converted.Where(c => !char.IsControl(c)).ToArray()).Trim();

                return string.IsNullOrWhiteSpace(converted) ? null : converted;
            }
            catch
            {
                // Unreadable card: the caller falls back to a timestamp name.
                return null;
            }
        }
    }
}
