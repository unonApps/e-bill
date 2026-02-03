using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TAB.Web.Models
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            var member = enumValue.GetType()
                .GetMember(enumValue.ToString())
                .FirstOrDefault();

            if (member == null)
            {
                return enumValue.ToString();
            }

            var displayAttribute = member.GetCustomAttribute<DisplayAttribute>();

            return displayAttribute?.Name ?? enumValue.ToString();
        }
    }
} 