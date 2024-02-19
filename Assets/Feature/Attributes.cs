using System;

public class AutoConstructOnInitializationAttribute : Attribute { }

public class DontDestroyOnReleaseAttribute : Attribute { }

public class FeatureAttribute : Attribute
{
    public string Name { get; private set; }
    public FeatureAttribute(string name)
    {
        this.Name = name;
    }
}
