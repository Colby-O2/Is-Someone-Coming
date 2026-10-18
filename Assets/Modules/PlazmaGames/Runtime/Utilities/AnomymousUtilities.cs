using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PlazmaGames.Core.Utils
{
    public static class AnonymousExtension
    {
        /// <summary>
        /// VOLATILE: Anonymous class are iMMUTABLE. 
        /// Therefore this is just a hack and likely won't work in 
        /// future version of C# or Unity if the property backing 
        /// format changes
        /// </summary>
        public static void SetProperty<T>(
            this object obj,
            string propertyName,
            T newValue
        )
        {
        BindingFlags FieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;
            string[] BackingFieldFormats = { "<{0}>i__Field", "<{0}>" };

            var backingFieldNames = BackingFieldFormats.Select(x => string.Format(x, propertyName)).ToList();
            var fi = obj.GetType()
                .GetFields(FieldFlags)
                .FirstOrDefault(f => backingFieldNames.Contains(f.Name));

            if (fi == null)
            {
                Debug.LogError($"Cannot find backing field for {propertyName} and was not set.");
                return;
            }

            fi.SetValue(obj, newValue);
        }

        public static object GetProperty(this object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName).GetValue(obj, null);
        }

        public static T GetProperty<T>(this object obj, string propertyName)
        {
            return (T)obj.GetType().GetProperty(propertyName).GetValue(obj, null);
        }

        public static T Anonymize<T>(this object value, T targetType)
        {
            return (T)value;
        }
    }
}
