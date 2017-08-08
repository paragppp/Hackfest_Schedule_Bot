using System;
using System.Linq;

namespace SampleAADv2Bot.Extensions
{
    public static class ExpressionCheckingHelper
    {
        public static bool IsNaturalNumber(this string argument)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(argument,
                @"^[1-9][0-9]*$");
        }

        public static bool IsEmailAddress(this string argument)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(argument,
                @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$");
        }

        public static bool IsEmailAddressList(this string argument)
        {
            //remove space
            var spaceRemovedArgument = argument.Replace(" ", "").Replace("　", "");
            var separatedEmailAddress = spaceRemovedArgument.Split(',');
            foreach (var i in separatedEmailAddress)
                Console.WriteLine(i);
            return separatedEmailAddress.All(s => System.Text.RegularExpressions.Regex.IsMatch(s, @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$"));
        }

        public static bool IsDatatime(this string argument)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(argument,
                @"^[0-9]{4}-[0-9]{2}-[0-9]{2}$");
        }

    }
}