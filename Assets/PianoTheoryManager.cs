using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PianoTheoryManager : MonoBehaviour
{
    [Header("Configurações da Lição")]
    public string tituloDaLicao = "Acorde de Fá Maior (Tema da Lôrna)";
    public List<string> notasNecessarias = new List<string> { "F", "A", "C" }; 
    private List<string> notasPressionadasAgora = new List<string>();

    [Header("Atributos de Campanha")]
    public float progressoTecnicoGanhado = 0.2f;
    // Referência estática para o Ping Pong ler (ou você pode usar PlayerPrefs)
    public static float TecnicaGlobal = 1.0f;

    [Header("UI Feedback")]
    public Text displayLicao;
    public Text displayNotas;
    public Image flashFeedback;

    [Header("Áudio")]
    public AudioSource audioSource;
    public AudioClip[] notasAudioClips; // Arraste os sons das notas aqui na ordem (C, C#, D...)

    private Dictionary<KeyCode, string> mapeamentoPC = new Dictionary<KeyCode, string>()
    {
        { KeyCode.A, "C" }, { KeyCode.W, "C#" }, { KeyCode.S, "D" }, { KeyCode.E, "D#" },
        { KeyCode.D, "E" }, { KeyCode.F, "F" }, { KeyCode.T, "F#" }, { KeyCode.G, "G" },
        { KeyCode.Y, "G#" }, { KeyCode.H, "A" }, { KeyCode.U, "A#" }, { KeyCode.J, "B" }
    };

    void Start() {
        if (displayLicao) displayLicao.text = tituloDaLicao;
        AtualizarTextoNotas();
        
        // Carrega progresso anterior se existir
        TecnicaGlobal = PlayerPrefs.GetFloat("LornaTecnica", 1.0f);
    }

    void Update() {
        // Apenas para testes rápidos no PC
        if (Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer) {
            DetectarTecladoPC();
        }
    }

    #region Entrada de Dados (PC & Android)
    void DetectarTecladoPC() {
        foreach (var par in mapeamentoPC) {
            if (Input.GetKeyDown(par.Key)) PressionarNota(par.Value);
            if (Input.GetKeyUp(par.Key)) SoltarNota(par.Value);
        }
    }

    // Método que os botões da UI (teclas do piano) vão chamar no Android
    public void PressionarNota(string nomeNota) {
        if (!notasPressionadasAgora.Contains(nomeNota)) {
            notasPressionadasAgora.Add(nomeNota);
            TocarSomDaNota(nomeNota);
            VerificarAcerto();
            AtualizarTextoNotas();
        }
    }

    public void SoltarNota(string nomeNota) {
        if (notasPressionadasAgora.Contains(nomeNota)) {
            notasPressionadasAgora.Remove(nomeNota);
            AtualizarTextoNotas();
        }
    }
    #endregion

    #region Lógica de Teoria Musical
    void VerificarAcerto() {
        int notasCertas = 0;
        foreach (string notaAlvo in notasNecessarias) {
            if (notasPressionadasAgora.Contains(notaAlvo)) notasCertas++;
        }

        // Se o jogador estiver segurando todas as notas da lição ao mesmo tempo
        if (notasCertas >= notasNecessarias.Count) {
            ConcluirLicao();
        }
    }

    void ConcluirLicao() {
        Debug.Log("<color=green>Lição Completa!</color>");
        TecnicaGlobal += progressoTecnicoGanhado;
        
        // Salva para o Ping Pong ler depois
        PlayerPrefs.SetFloat("LornaTecnica", TecnicaGlobal);
        PlayerPrefs.Save();

        StartCoroutine(EfeitoSucesso());
    }

    IEnumerator EfeitoSucesso() {
        if (flashFeedback) flashFeedback.color = Color.green;
        if (displayLicao) displayLicao.text = "PERFEITO! Técnica Aumentada!";
        yield return new WaitForSeconds(2f);
        if (flashFeedback) flashFeedback.color = new Color(0,0,0,0);
        if (displayLicao) displayLicao.text = tituloDaLicao;
    }
    #endregion

    #region Auxiliares
    void TocarSomDaNota(string nomeNota) {
        // Aqui você tocaria o áudio correspondente. 
        // Exemplo: se for "C", toca notasAudioClips[0]
        Debug.Log("Som da nota: " + nomeNota);
    }

    void AtualizarTextoNotas() {
        if (displayNotas) {
            displayNotas.text = "Notas Atuais: " + string.Join(" - ", notasPressionadasAgora);
        }
    }
    #endregion
}