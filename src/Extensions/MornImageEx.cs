using UnityEngine.UI;

namespace MornLib
{
    public static class MornImageEx
    {
        public static void SetAlpha(this Image image, float alpha)
        {
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }

        public static float GetAlpha(this Image image)
        {
            return image.color.a;
        }
    }
}