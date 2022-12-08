using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SEE.Game.City;
using SEE.Game.UI.RuntimeConfigMenu;
using UnityEngine;

public static class RuntimeAttributeUtilities
{
    public static void OutputAttributes()
    {
        Debug.Log("OutputAttributes");
        // Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        // TODO: is this the current way to get the see assembly
        // Does that work for the finished build?
        // TODO: Is there a more robust way to get the name of the SEE module?
        // (even if the SEE module name shouldn't change for a while)
        Assembly assembly = Assembly.Load(new AssemblyName(nameof(SEE)));
        IEnumerable<Type> cityTypes = assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(AbstractSEECity)));
        foreach (Type type in cityTypes)
        {
            Debug.Log(type.FullName);
            const BindingFlags flags = BindingFlags.Default;
            foreach (MemberInfo memberInfo in type.GetMembers(flags))
            {
                foreach (CustomAttributeData attribute in memberInfo.CustomAttributes)
                {
                }
            }
        }
    }
}
