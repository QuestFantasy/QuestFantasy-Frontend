public enum ElementsTypes { Normal, Earth, Water, Fire, Wind }

public class Element
{
    public ElementsTypes ElementType { get; set; }
    public float AgainstGet(Element targetElement)
    {
        // Calculate effectiveness against another element (input is the Defender's element)

        if (ElementType == ElementsTypes.Normal) return 1f;
        if (ElementType == ElementsTypes.Earth)
        {
            if (targetElement.ElementType == ElementsTypes.Wind) return 0.5f;
            if (targetElement.ElementType == ElementsTypes.Water) return 2f;
        }
        if (ElementType == ElementsTypes.Water)
        {
            if (targetElement.ElementType == ElementsTypes.Earth) return 0.5f;
            if (targetElement.ElementType == ElementsTypes.Fire) return 2f;
        }
        if (ElementType == ElementsTypes.Fire)
        {
            if (targetElement.ElementType == ElementsTypes.Water) return 0.5f;
            if (targetElement.ElementType == ElementsTypes.Wind) return 2f;
        }
        if (ElementType == ElementsTypes.Wind)
        {
            if (targetElement.ElementType == ElementsTypes.Fire) return 0.5f;
            if (targetElement.ElementType == ElementsTypes.Earth) return 2f;
        }
        return 1f;
    }
}