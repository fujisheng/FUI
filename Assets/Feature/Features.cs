using FUI;

using System.Collections.Generic;
using System.Reflection;

internal class Features
{
    const string publicFeatureName = "Public";
    Feature publicFeature;
    Dictionary<string, Feature> features;

    Feature GetOrCreateFeature(string featureName)
    {
        if (string.IsNullOrEmpty(featureName))
        {
            featureName = publicFeatureName;
        }

        if(featureName == publicFeatureName)
        {
            if(publicFeature == null)
            {
                publicFeature = new Feature(publicFeatureName);
                features.Add(publicFeatureName, publicFeature);
            }
            return publicFeature;
        }

        if (features.TryGetValue(featureName, out var feature))
        {
            return feature;
        }
        feature = new Feature(featureName);
        features.Add(featureName, feature);
        return feature;
    }

    public void AddData(string featureName, IData data)
    {
        var feature = GetOrCreateFeature(featureName);
        feature.AddData(data);
    }

    public void AddData(IData data)
    {
        var dataType = data.GetType();
        var featureAttribute = dataType.GetCustomAttribute<FeatureAttribute>();
        if (featureAttribute == null)
        {
            AddData(publicFeatureName, data);
            return;
        }

        AddData(featureAttribute.Name, data);
    }

    public void AddPresentation(string featureName, IPresentation presentation)
    {
        var feature = GetOrCreateFeature(featureName);
        feature.AddPresentation(presentation);
    }

    public void Destroy(string featureName)
    {

    }

    public void DestroyAll()
    {
    }
}
