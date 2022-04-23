namespace VkArchiveParser.Utils
{
    public static class StringUtils
    {
        public static readonly char BASE64_PAD_CHARACTER = '=';
        public static string Base64FixPadding(string base64)
        {
            int iRemainderValue = 0;

            //if the length isn't already a multiple of 4, determine the number of characters needed to make it so
            if (0 != (base64.Length % 4))
                iRemainderValue = 4 - (base64.Length % 4);

            return base64.PadRight(base64.Length + iRemainderValue, BASE64_PAD_CHARACTER);
        }
    }
}