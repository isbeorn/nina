#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Markup;

namespace NINA.Utility {

    public class EnumBindingSourceExtension : MarkupExtension {
        private Type _enumType;

        public Type EnumType {
            get { return this._enumType; }
            set {
                if (value != this._enumType) {
                    if (null != value) {
                        Type enumType = Nullable.GetUnderlyingType(value) ?? value;
                        if (!enumType.IsEnum)
                            throw new ArgumentException("Type must be for an Enum.");
                    }

                    this._enumType = value;
                }
            }
        }

        public EnumBindingSourceExtension() {
        }

        public EnumBindingSourceExtension(Type enumType) {
            this.EnumType = enumType;
        }

        public override object ProvideValue(IServiceProvider serviceProvider) {
            if (null == this._enumType)
                throw new InvalidOperationException("The EnumType must be specified.");

            Type actualEnumType = Nullable.GetUnderlyingType(this._enumType) ?? this._enumType;
            Array enumValues = System.Enum.GetValues(actualEnumType);

            if (actualEnumType == this._enumType)
                return enumValues;

            Array tempArray = Array.CreateInstance(actualEnumType, enumValues.Length + 1);
            enumValues.CopyTo(tempArray, 1);
            return tempArray;
        }
    }

    public class EnumDescriptionTypeConverter : EnumConverter {

        public EnumDescriptionTypeConverter(Type type)
            : base(type) {
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) {
            if (destinationType == typeof(string)) {
                FieldInfo fi = value?.GetType().GetField(value.ToString());
                if (fi != null) {
                    var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
                    var label = ((attributes.Length > 0) && (!String.IsNullOrEmpty(attributes[0].Description))) ? attributes[0].Description : value.ToString();
                    var s = Locale.Loc.Instance[label];
                    return s;
                }

                return string.Empty;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}