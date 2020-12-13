#if !UNITY_2018_1_OR_NEWER

using System;

namespace UnityEngine.Scripting
{
    // Use this attribute in Unity to avoid stripping (de)serializer classes
    // from builds if 'Project Settings/Player/Other/Managed Stripping Level'
    // is set to Medium or higher
    public class PreserveAttribute : Attribute
    {
    }
}

#endif