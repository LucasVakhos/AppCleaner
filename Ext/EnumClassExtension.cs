#nullable enable
namespace AppCleaner
{
    public static class EnumClassExtension
    {
        // Считает количество значений у любого enum TEnum
        public static int Count<TEnum>() where TEnum : Enum
        {
            // Можно использовать Enum.GetNames или Enum.GetValues — оба дают одно и то же по количеству
            return Enum.GetNames(typeof(TEnum)).Length;
            // альтернативно: return Enum.GetValues(typeof(TEnum)).Length;
        }
        // Расширение для Type: считать значения по runtime-типу
        public static int Count(this Type enumType)
        {
            if (enumType == null) throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum) throw new ArgumentException("Type must be an enum.", nameof(enumType));
            return Enum.GetNames(enumType).Length;
        }
        public static TAttribute? GetAttribute<TAttribute>(this Enum value)
            where TAttribute : Attribute
        {
            var field = value.GetType().GetField(value.ToString());
            return field?
                .GetCustomAttributes(typeof(TAttribute), false)
                .Cast<TAttribute>()
                .FirstOrDefault();
        }
        public static TValue GetEnumValueByDescription<TEnum, TValue>(
            this TEnum enumValue,
            Func<TEnum, string> descriptionSelector)
        {
            var type = typeof(TEnum);
            if (!type.IsEnum)
                throw new ArgumentException("TEnum must be an enumerated type");
            var enumValues = Enum.GetValues(type);
            foreach (var value in enumValues)
            {
                if (value.ToString() == enumValue.ToString())
                    return (TValue)value;
            }
            return default;
        }
    }
}
