using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlazmaGames.Attribute
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public abstract class MonoScriptAttribute : PropertyAttribute
    {
        public System.Type type;
    }

    public class MonoBehaviourAttribute : MonoScriptAttribute
    {

    }

    public class ViewAttribute : MonoScriptAttribute
    {

    }
}
