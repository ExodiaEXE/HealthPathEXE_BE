using System;
using System.Text;
using System.Text.RegularExpressions;

namespace HealthPath.API.Common;

public static class SlugHelper
{
    public static string GenerateSlug(string phrase)
    {
        if (string.IsNullOrEmpty(phrase))
        {
            return string.Empty;
        }

        string str = RemoveSign4VietnameseString(phrase).ToLower();

        // Invalid chars
        str = Regex.Replace(str, @"[^a-z0-9\s-]", "");

        // Convert multiple spaces into one
        str = Regex.Replace(str, @"\s+", " ").Trim();

        // Cut and trim
        str = str.Substring(0, str.Length <= 100 ? str.Length : 100).Trim();

        // Spaces to hyphens
        str = Regex.Replace(str, @"\s", "-");

        return str;
    }

    private static string RemoveSign4VietnameseString(string str)
    {
        string[] arr1 = new string[] { "á", "à", "ả", "ã", "ạ", "â", "ấ", "ầ", "ẩ", "ẫ", "ậ", "ă", "ắ", "ằ", "ẳ", "ẵ", "ặ",
            "đ",
            "é","è","ẻ","ẽ","ẹ","ê","ế","ề","ể","ễ","ệ",
            "í","ì","ỉ","ĩ","ị",
            "ó","ò","ỏ","õ","ọ","ô","ố","ồ","ổ","ỗ","ộ","ơ","ớ","ờ","ở","ỡ","ợ",
            "ú","ù","ủ","ũ","ụ","ư","ứ","ừ","ử","ữ","ự",
            "ý","ỳ","ỷ","ỹ","ỵ",};

        string[] arr2 = new string[] { "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a",
            "d",
            "e","e","e","e","e","e","e","e","e","e","e",
            "i","i","i","i","i",
            "o","o","o","o","o","o","o","o","o","o","o","o","o","o","o","o","o",
            "u","u","u","u","u","u","u","u","u","u","u",
            "y","y","y","y","y",};

        for (int i = 0; i < arr1.Length; i++)
        {
            str = str.Replace(arr1[i], arr2[i]);
            str = str.Replace(arr1[i].ToUpper(), arr2[i].ToUpper());
        }

        return str;
    }
}
