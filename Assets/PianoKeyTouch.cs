using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Essencial para o Touch no Android

public class PianoKeyTouch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public string notaMusical; // Ex: "C", "C#", "D"
    public PianoTheoryManager manager;
    private Image imagemTecla;
    public Color corPress = Color.gray;
    private Color corOriginal;

    void Start() {
        imagemTecla = GetComponent<Image>();
        corOriginal = imagemTecla.color;
    }

    // Detecta o toque (Multi-touch amigável)
    public void OnPointerDown(PointerEventData eventData) {
        imagemTecla.color = corPress;
        manager.PressionarNota(notaMusical);
    }

    // Detecta quando o dedo sai da tecla
    public void OnPointerUp(PointerEventData eventData) {
        imagemTecla.color = corOriginal;
        manager.SoltarNota(notaMusical);
    }
}