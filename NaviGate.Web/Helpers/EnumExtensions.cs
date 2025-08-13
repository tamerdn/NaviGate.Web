using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

// Projenizin ana namespace'i ile uyumlu hale getirin
namespace NaviGate.Web.Helpers
{
    public static class EnumExtensions
    {
        // Bu metot, herhangi bir enum değerinin [Display(Name="...")] etiketini okur.
        public static string GetDisplayName(this Enum enumValue)
        {
            var displayName = enumValue.GetType()
                .GetMember(enumValue.ToString())
                .First()
                .GetCustomAttribute<DisplayAttribute>()?
                .GetName();

            // Eğer bir DisplayAttribute tanımlanmamışsa, standart ismini döndür.
            return displayName ?? enumValue.ToString();
        }
    }
}