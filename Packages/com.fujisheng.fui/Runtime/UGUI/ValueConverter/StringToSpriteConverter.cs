using UnityEngine;

namespace FUI.UGUI.ValueConverter
{
    /// <summary>
    /// 将string转换成Sprite
    /// </summary>
    public class StringToSpriteConverter : ValueConverter<string, Sprite, IAssetLoader>
    {
        public override Sprite Convert(string value, IAssetLoader assetLoader)
        {
            return assetLoader.Load<Sprite>(value);
        }
        public override string ConvertBack(Sprite value, IAssetLoader assetLoader)
        {
            return value.name;
        }
    }
}
