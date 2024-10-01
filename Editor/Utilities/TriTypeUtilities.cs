using System;
using System.Collections.Generic;

namespace TriInspector.Utilities
{
    public static class TriTypeUtilities
    {
        private static readonly Dictionary<Type, string> TypeNiceNames = new Dictionary<Type, string>();

        public static string GetTypeNiceName(Type type)
        {
            if (TypeNiceNames.TryGetValue(type, out var niceName))
            {
                return niceName;
            }

            niceName = type.Name;

            while (type.DeclaringType != null)
            {
                niceName = type.DeclaringType.Name + "." + niceName;

                type = type.DeclaringType;
            }

            #region カスタマイズ: Description属性に対応

            var descriptionAttribute = System.Reflection.CustomAttributeExtensions
                .GetCustomAttribute<System.ComponentModel.DescriptionAttribute>(type);
            if (descriptionAttribute != null)
            {
                niceName += " (" + descriptionAttribute.Description + ")";
            }

            #endregion

            TypeNiceNames[type] = niceName;

            return niceName;
        }
    }
}