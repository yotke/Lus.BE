namespace Lus.Application.Common.Extensions
{
    public static class ModelExtensions
    {
        private static List<string> commonListOfPropertiesToIgnore = new List<string> { "Id", "DeletedOn", "DeletedById", "CreatedOn", "CreatedById", "UpdatedOn", "UpdatedById" };

        public static T ConvertEnum<T>(this string value)
        {
            if (typeof(T).IsEnum)
                return (T)Enum.Parse(typeof(T), value);
            return (T)Convert.ChangeType(value, typeof(T));
        }

        public static string HidePhoneNumber(this string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                phoneNumber = RandomDigits(10);
            }

            var hidedPhone = $"{new string('*', phoneNumber.Length - 2)}{phoneNumber.Substring(phoneNumber.Length - 2, 2)}";

            return hidedPhone;
        }

        private static string RandomDigits(int length)
        {
            var random = new Random();
            string s = string.Empty;
            for (int i = 0; i < length; i++)
                s = String.Concat(s, random.Next(10).ToString());
            return s;
        }

        public static void CopyIfDifferent<Target, Source>(this Target target, Source source, List<string> listOfPropertiesToIgnore)
            where Target : EntityBase<int>
        {
            foreach (var prop in target.GetType().GetProperties())
            {
                if (!listOfPropertiesToIgnore.Contains(prop.Name) && !commonListOfPropertiesToIgnore.Contains(prop.Name))
                {
                    var targetValue = GetPropValue(target, prop.Name);
                    var sourceValue = GetPropValue(source, prop.Name);
                    if (targetValue != null && !targetValue.Equals(sourceValue))
                    {
                        SetPropertyValue(target, prop.Name, sourceValue);
                    }
                    else if (targetValue == null && sourceValue != null)
                    {
                        SetPropertyValue(target, prop.Name, sourceValue);
                    }
                }
            }
        }

        private static object GetPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName)?.GetValue(src, null);
        }

        private static void SetPropertyValue(object obj, string propName, object value)
        {
            obj.GetType().GetProperty(propName)?.SetValue(obj, value, null);
        }

    }
}
