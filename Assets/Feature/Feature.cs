using FUI;

using System.Collections.Generic;

/// <summary>
/// 业务功能
/// </summary>
internal class Feature
{
    /// <summary>
    /// 功能名
    /// </summary>
    internal string Name { get; private set; }

    /// <summary>
    /// 所有这个功能相关的数据
    /// </summary>
    List<IData> datas;

    /// <summary>
    /// 所有这个功能相关的表现层
    /// </summary>
    List<IPresentation> presentations;

    internal Feature(string name)
    {
        this.Name = name;
        this.datas = new List<IData>();
        this.presentations = new List<IPresentation>();
    }

    internal Feature(string name, List<IData> datas, List<IPresentation> presentations)
    {
        this.Name = name;
        this.datas = datas;
        this.presentations = presentations;
    }

    internal void AddData(IData data)
    {

    }

    internal void DestroyData(IData data)
    {

    }

    internal void AddPresentation(IPresentation presentation)
    {
    }

    internal void DestroyPresentation(IPresentation presentation)
    {
    }

    internal void Destroy()
    {

    }
}
