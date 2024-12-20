using System.Collections.Generic;

namespace FUI
{
    /// <summary>
    /// Ҫ�󶨵�������Ϣ
    /// </summary>
    public class PropertyInfo
    {
        /// <summary>
        /// ������
        /// </summary>
        public string name;

        /// <summary>
        /// ��������
        /// </summary>
        public string type;

        /// <summary>
        /// �Ƿ����б�����
        /// </summary>
        public bool isList;

        /// <summary>
        /// λ����Ϣ
        /// </summary>
        public LocationInfo location;
    }

    /// <summary>
    /// λ����Ϣ
    /// </summary>
    public class LocationInfo
    {
        /// <summary>
        /// �ļ�·��
        /// </summary>
        public string path;

        /// <summary>
        /// ����
        /// </summary>
        public int line;

        /// <summary>
        /// ��
        /// </summary>
        public int column;
    }

    /// <summary>
    /// Ҫ�󶨵�ת������Ϣ
    /// </summary>
    public class ConverterInfo
    {
        /// <summary>
        /// ת��������
        /// </summary>
        public string type;

        /// <summary>
        /// ת����Դ����
        /// </summary>
        public string sourceType;

        /// <summary>
        /// ת����Ŀ������
        /// </summary>
        public string targetType;

        /// <summary>
        /// λ����Ϣ
        /// </summary>
        public LocationInfo location;
    }

    /// <summary>
    /// Ҫ�󶨵�Ŀ����Ϣ
    /// </summary>
    public class TargetInfo
    {
        /// <summary>
        /// Ŀ��·��
        /// </summary>
        public string path;

        /// <summary>
        /// Ŀ������
        /// </summary>
        public string type;

        /// <summary>
        /// Ŀ����������
        /// </summary>
        public string propertyType;

        /// <summary>
        /// Ŀ����������
        /// </summary>
        public string propertyName;

        /// <summary>
        /// Ŀ������ֵ����
        /// </summary>
        public string propertyValueType;
    }

    public class CommandInfo
    {
        /// <summary>
        /// ����
        /// </summary>
        public string name;

        /// <summary>
        /// ����
        /// </summary>
        public List<string> parameters;

        /// <summary>
        /// �Ƿ���event
        /// </summary>
        public bool isEvent;

        /// <summary>
        /// ��Դ�ļ��е�λ����Ϣ
        /// </summary>
        public LocationInfo location;
    }

    public class CommandTargetInfo
    {
        /// <summary>
        /// Ŀ��·��
        /// </summary>
        public string path;

        /// <summary>
        /// ����
        /// </summary>
        public string type;

        /// <summary>
        /// Ŀ����������
        /// </summary>
        public string propertyName;

        /// <summary>
        /// ��������
        /// </summary>
        public List<string> parameters;
    }

    /// <summary>
    /// ���԰���Ϣ
    /// </summary>
    public class PropertyBindingInfo
    {
        public PropertyInfo propertyInfo;
        public ConverterInfo converterInfo;
        public TargetInfo targetInfo;

        public BindingMode bindingMode;
    }

    /// <summary>
    /// �������Ϣ
    /// </summary>
    public class CommandBindingInfo
    {
        public CommandInfo commandInfo;
        public CommandTargetInfo targetInfo;
    }

    /// <summary>
    /// �����İ���Ϣ
    /// </summary>
    public class ContextBindingInfo
    {
        public string viewName = string.Empty;
        public string viewModelType;
        public List<PropertyBindingInfo> properties = new List<PropertyBindingInfo>();
        public List<CommandBindingInfo> commands = new List<CommandBindingInfo>();
    }
}
