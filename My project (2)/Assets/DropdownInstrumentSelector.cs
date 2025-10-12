using UnityEngine;
using UnityEngine.UI;

public class DropdownInstrumentSelector : MonoBehaviour
{
    public Dropdown instrumentDropdown; // arraste o Dropdown aqui no Inspector
    public GameObject grandPiano;
    public GameObject steelDrum;

    void Start()
    {
        instrumentDropdown.onValueChanged.AddListener(OnDropdownChanged);
        OnDropdownChanged(instrumentDropdown.value); // ativa o inicial
    }

    void OnDropdownChanged(int index)
    {
        switch (index)
        {
            case 0:
                grandPiano.SetActive(true);
                steelDrum.SetActive(false);
                break;
            case 1:
                grandPiano.SetActive(false);
                steelDrum.SetActive(true);
                break;
            default:
                grandPiano.SetActive(false);
                steelDrum.SetActive(false);
                break;
        }
    }
}
