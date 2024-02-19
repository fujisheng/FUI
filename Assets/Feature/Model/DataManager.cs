using FUI;

using System;
using System.Collections.Generic;
using System.Reflection;

public class DataManager
{
    List<IData> datas = new List<IData>();

    public void Initialize()
    {
        foreach(var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach(var type in asm.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(AutoConstructOnInitializationAttribute), false).Length > 0 
                    && typeof(IData).IsAssignableFrom(type))
                {
                    InitData(type);
                }
            }
        }
    }

    void InitData<T>() where T : IData
    {
        InitData(typeof(T));
    }

    void InitData(Type type)
    {
        var existIndex = datas.FindIndex((item) => item.GetType() == type);
        if (existIndex >= 0)
        {
            return;
        }
        var data = Activator.CreateInstance(type) as IData;
        data.Initialize();
        datas.Add(data);
    }

    public T GetData<T>() where T : IData
    {
        var existIndex = datas.FindIndex((item) => item.GetType() == typeof(T));
        if (existIndex >= 0)
        {
            return (T)datas[existIndex];
        }

        return default;
    }

    public T Modify<T>() where T : IData
    {
        var data = GetData<T>();
        if(data != null)
        {
            FireModifyEvent(data);
        }

        return data;
    }

    void FireModifyEvent(IData data)
    {
    }
}
