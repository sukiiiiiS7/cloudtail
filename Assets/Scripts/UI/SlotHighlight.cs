using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SlotHighlight : MonoBehaviour
{
    public Color normal   = Color.white;
    public Color hover    = new Color32(0xAA, 0xCF, 0xFF, 0xFF);
    public Color selected = new Color32(0x66, 0xB3, 0xFF, 0xFF);
    public Color pressed  = new Color32(0x33, 0x66, 0x99, 0xFF);

    SpriteRenderer sr;
    bool isSelected;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.color = normal;
    }

    public void SetHover(bool v)
    {
        if (isSelected) return;        
        sr.color = v ? hover : normal;
    }

    public void SetPressed(bool v)
    {
        if (isSelected) return;
        sr.color = v ? pressed : normal;
    }

    public void SetSelected(bool v)
    {
        isSelected = v;
        sr.color = v ? selected : normal;
    }

    public void ToggleSelected() => SetSelected(!isSelected);
}
