using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Events; 
using UnityEngine.EventSystems; 
using UnityEngine.Networking;
using System.IO;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using SimpleFileBrowser; 
using UnityEngine.InputSystem; // OBRIGATÓRIO PARA O MINIS
using Minis; // NOVO REI DO MIDI: MINIS
// using GooglePlayGames;
// using GooglePlayGames.BasicApi;

public class PingPongVideoManual : MonoBehaviour 
{
    public enum Language { Portugues, English, Espanol, Italiano }
    public enum EstadoJogo { AguardandoPedido, LendoInfo, TurnoJogador1, TurnoJogador2, ReproduzindoVideo, PausaEntreRounds, FimDeJogo, EventoCinematico, ConfrontoDireto }
    
    public enum TipoCenario { SalaEscolar, NapolesPublico, CelaIsolamento }
    public enum EmocaoMusical { Neutra, VitoriaMaior, TensaoMenor, PerigoDiminuto, MagiaAumentada }

    [SerializeField] private EstadoJogo estadoAtual = EstadoJogo.AguardandoPedido;

    [Header("Configuração de Idioma")]
    public Language idiomaAtual = Language.Portugues;

    [Header("Atributos Básicos")]
    public float tecnicaEspectral = 1.0f; 
    public float focoEscolar = 1.0f;

    [Header("Mecânicas da Temporada 2")]
    public bool isSeason2 = false; 
    public int vidasLorna = 3; 

    [Header("Sistema de Combate e Combos")]
    public int comboAtual = 0;
    public int comboMaximo = 0;
    public int toquesNoConfronto = 0;
    public Text textoCombo;
    public Text textoClassificacaoHit; 
    public CanvasGroup painelConfronto; 

    private float janelaPerfect = 0.10f; 
    private float janelaGreat = 0.25f;
    private float janelaGood = 0.40f;

    [Header("Dificuldade")]
    public AnimationCurve curvaMultiplicadorAdrenalina = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1)); 
    [Range(0f, 1f)] public float erroMaximoIA = 0.30f; 
    [Range(0f, 1f)] public float erroMinimoIA = 0.01f;
    private float janelaPerfeitaAtual; 
    private bool botaoMovelAtivo = false;
    private Coroutine rotinaMoverBotao;
    private Vector2 posicaoAlvoBotao;
    private float velocidadeMovimentoBotao = 10f; 
    private bool deveMoverBotaoSuave = false;

    [Header("Modo História (Piano Solo)")]
    public bool modoPianoSoloAtivo = false;
    public RawImage fundoEstaticoImagem; 
    public GameObject painelTecladoVisual88; 
    [HideInInspector] public Image[] teclasDoPianoUI; 
    public int[] musicaObrigatoriaMidi;
    private int progressoMusicaObrigatoria = 0; 

    [Header("Seleção de Cenários")]
    public TipoCenario cenarioAtual = TipoCenario.SalaEscolar;
    public GameObject painelSelecaoCenario; 
    public Button btnCenarioEscola, btnCenarioNapoles, btnCenarioCela; 
    public GameObject ambienteEscola, ambienteNapoles, ambienteCela; 
    public Text textoTituloCenario; 

    [Header("UI & Canvas Principal")]
    public GameObject painelPedido, containerInterfaceJogo; 
    public RectTransform painelPlacar; 
    public Button botaoAceitar, botaoJogador1, botaoTutorialMidi, botaoImportarMidi, botaoLeaderboard;  
    public Text textoPontuacao, textoLabelEnergia, textoFeedback, textoNotificacaoDesafio;
    public Text textoNewsPerfil, textoNewsLegenda, textoBotaoAceitar, textoBotaoRecusar, textoAvisoFight; 
    public Image barraDeTempoVisual, barraEnergiaUI; 
    public CanvasGroup flashGiovanna;
    public Image overlayCorCenario;
    public GameObject painelOverlayTV, painelNoticiasTikTok;
    
    [Header("Sistema Online de Posts")]
    public GameObject painelFeedSocial;
    public Transform containerPostsUI;
    public GameObject prefabPostItem;
    public string urlJsonFeed = "COLOQUE_SEU_LINK_DO_GITHUB_GIST_AQUI.json";
    
    [Header("Motor Invocação Teclado 88 (Easter Egg)")]
    public int splitNoteMidiLimit = 60; 
    public AudioClip sfxQuebraExpectativa; 

    private bool easterEggApagaoAtivo = false;
    private int sequenceStep = 0; 

    [Header("Efeitos Visuais UI")]
    public RectTransform painelNotasCaindo; 
    public Image brilhoPlacar; 
    public float forcaBrilhoBase = 0.5f;

    private bool isPortrait = false;

    [Header("Sistema OTG (Plug & Play)")]
    public CanvasGroup painelAvisoMidi;
    public Text textoAvisoMidi;
    private Coroutine animacaoAvisoMidi;

    [Header("Ensino de Piano Indireto")]
    public Text textoMissaoMusical; 
    private EmocaoMusical missaoHarmonicaAtual = EmocaoMusical.Neutra;
    private string[] nomesDasNotas = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    [Header("Google Play Games Integração")]
    public bool ativarPlayGames = true;
    public string idLeaderboardPontuacao = "COLOQUE_SEU_ID_LEADERBOARD_AQUI";
    public string idConquista30Pontos = "ID_CONQUISTA_30";
    public string idConquista60Pontos = "ID_CONQUISTA_60";
    public string idConquista100Pontos = "ID_CONQUISTA_100";

    [Header("Sistema de Importação MIDI (.mid)")]
    public string nomeArquivoMidiEnsino = "tutorial_tema.mid"; 
    [Range(-0.5f, 0.5f)] public float compensacaoAudioVisual = 0f; 
    private Coroutine rotinaReproducaoMidi;
    private bool reproduzindoMidiArquivo = false;
    private float tempoAtualTutorial = 0f;

    [Header("Gravador SpeedArt")]
    public bool gravandoSpeedArt = false;
    public float velocidadeSpeedArt = 0.5f; 
    public AudioClip musicaTemaSpeedArt; 
    private int contadorFramesSpeedArt = 0;

    [System.Serializable]
    public struct NotaMidiImportada {
        public int notaMidi;
        public float tempoInicioMilisegundos;
        public float duracaoMilisegundos;
        public float velocity;
    }
    public List<NotaMidiImportada> sequenciaAprendizado = new List<NotaMidiImportada>();

    [Header("Configurações do Placar")]
    public float posicaoYEscondido = 600f, posicaoYVisivel = -100f; 
    public float velocidadeAnimacaoPlacar = 8f, tempoExibicaoPlacar = 1.2f;

    [Header("Vídeo & Componentes")]
    public VideoPlayer vp1, vp2;
    public RawImage telaUnica;
    public RenderTexture textureA, textureB;
    private VideoPlayer vpAtivo, vpEmEspera;
    private RenderTexture texAtiva, texEspera;

    [Header("Clipes de Vídeo (Temporada 1)")]
    public VideoClip introDesafio, inicioPartidaBase; 
    public VideoClip[] jogador1ClipesBase, jogador2ClipesBase;
    public VideoClip jogador1PerdeuBase, jogador2PerdeuBase, goldenChordVideoBase, videoEventoHeadshotBase; 
    public VideoClip[] jogador1ClipesClimaxBase, jogador1ClipesTristesBase; 

    [Header("Clipes de Vídeo (Temporada 1 - Nápoles)")]
    public VideoClip inicioPartidaNapoles; 
    public VideoClip[] jogador1ClipesNapoles, jogador2ClipesNapoles;
    public VideoClip jogador1PerdeuNapoles, jogador2PerdeuNapoles, goldenChordVideoNapoles, videoEventoHeadshotNapoles; 
    public VideoClip[] jogador1ClipesClimaxNapoles, jogador1ClipesTristesNapoles;

    [Header("Clipes de Vídeo (Temporada 2 - Cela)")]
    public VideoClip inicioPartidaCela; 
    public VideoClip[] jogador1ClipesCela, jogador2ClipesCela;
    public VideoClip jogador1PerdeuCela, jogador2PerdeuCela, goldenChordVideoCela, videoEventoHeadshotCela; 
    public VideoClip[] jogador1ClipesClimaxCela, jogador1ClipesTristesCela;
    public VideoClip lornaDesintegrandoVideo, bossDesintegrandoVideo;

    private VideoClip inicioPartida, jogador1Perdeu, jogador2Perdeu, goldenChordVideo, videoEventoHeadshot;
    private VideoClip[] jogador1Clipes, jogador2Clipes, jogador1ClipesClimax, jogador1ClipesTristes;

    [Header("Áudio & SFX")]
    public bool usarTemaEspecial = false;
    public AudioSource audioMenuSuno, audioCoverEspecial, audioMusicaFundo, audioMusicaEpica, audioSfx;
    public AudioClip somMudaMuda, sfxMenuConfirm, sfxRound1, sfxFight, sfxHitNormal, sfxHitGolden, sfxPontoGanhado, sfxPontoPerdido, sfxEnergiaCheia, sfxKO, sfxDanoSofrido, sfxDesintegracao;

    [Header("Motor de Áudio Externo (Pro)")]
    public bool usarMotorDeAudioExterno = false;
    public UnityEvent<int, float> OnTocarNotaExterna;
    public UnityEvent<int> OnPararNotaExterna;

    [System.Serializable]
    public struct AmostraPiano {
        public string nomeDaNota; public int notaMidiBase; public AudioClip clipeDeAudio;
    }

    [Header("Piano Nativo - Fallback")]
    public AudioClip somPianoBaseC4; 
    public AmostraPiano[] bancoDePianoMultisample;

    [Header("Sincronia & Combate")]
    public float tempoDoSoloPiano = 53.0f, margemPerfeito = 0.25f; 
    public Vector2 limiteAleatorioX = new Vector2(-400, 400), limiteAleatorioY = new Vector2(-300, 100); 
    public int pontosObjetivoFinal = 100;
    public float tempoReacaoBase = 1.8f; 
    [Range(0f, 1f)] public float chanceErroIA = 0.15f; 
    public RectTransform[] publicoFase;

    public bool controleMidiAtivo = true;
    public EmocaoMusical emocaoAtual = EmocaoMusical.Neutra; 
    
    private List<int> teclasPressionadas = new List<int>(), cordasVibrando = new List<int>(); 

    private bool aguardandoCalculoAcorde = false, midiDisparouNormal = false, midiDisparouGolden = false;
    private string ultimoAcordeDetectado = "Aguardando...";
    private float densidadeAcaoMidi = 0f, timerJanelaAcorde = 0f, tempoUltimaNotaTocadaMidi = 0f, volumeAlvoMusica = 1f;
    private bool fazendoCrossfade = false;

    [Header("Debug & Testes")]
    public int pontosJogador1 = 0;
    private int pontosJogador2, proximoAlvoFase = 15;
    
    private float tempoDificuldadeAtual, tempoLimiteAbsoluto, energiaAtual, multiplicadorAdrenalina = 1f;
    private bool estouAguardandoSaque = true, processandoVideo = false, musicaEpicaAtiva = false, energiaSomTocado = false;
    private string forcaDoUltimoGolpe = "NORMAL", statusRealtime = "";
    private int tamanhoFonteOriginal;
    private Coroutine timerTurnoCoroutine, animacaoPlacarCoroutine, feedbackCoroutine;

    private int poolSize = 64; 
    private AudioSource[] poolDePianos;
    private int[] poolNotaMidi;
    private bool[] poolTeclaPressionada;
    private Coroutine[] poolFadeCoroutines;
    private int indicePoolPiano = 0;
    private int ultimoAcordeFisico = -1; 

    // ==========================================
    // NOVOS EFEITOS ESPECIAIS (VFX ANIME / FIGHTER)
    // ==========================================
    [Header("Efeitos Avançados de Combate")]
    public bool habilitarEfeitosEspeciais = true;
    private bool tempoParado = false;
    private float tempoUltimoRastro = 0f;

    IEnumerator EfeitoTimeStop() {
        tempoParado = true;
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0.05f; 
        if (overlayCorCenario) overlayCorCenario.color = new Color(1f, 1f, 1f, 0.9f);
        yield return new WaitForSecondsRealtime(0.15f); 
        Time.timeScale = originalTimeScale;
        tempoParado = false;
        if (overlayCorCenario) overlayCorCenario.color = new Color(0,0,0,0);
    }

    void GerarTextoFlutuante(string mensagem, Color cor, Vector2 posicaoOrigem) {
        if (containerInterfaceJogo == null) return;
        GameObject objTxt = new GameObject("TextoFlutuante_" + mensagem);
        objTxt.transform.SetParent(containerInterfaceJogo.transform, false);
        objTxt.transform.SetAsLastSibling();
        
        Text txt = objTxt.AddComponent<Text>();
        txt.raycastTarget = false; 
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = mensagem; txt.color = cor; txt.fontSize = isSeason2 ? 45 : 35;
        txt.fontStyle = FontStyle.BoldAndItalic; txt.alignment = TextAnchor.MiddleCenter;
        
        Outline contorno = objTxt.AddComponent<Outline>();
        contorno.effectColor = Color.black; contorno.effectDistance = new Vector2(2, -2);
        
        RectTransform rt = objTxt.GetComponent<RectTransform>();
        rt.anchoredPosition = posicaoOrigem + new Vector2(Random.Range(-80f, 80f), Random.Range(30f, 60f));
        StartCoroutine(AnimarTextoFlutuante(rt, txt));
    }

    IEnumerator AnimarTextoFlutuante(RectTransform rt, Text txt) {
        float t = 0; Vector2 posInicial = rt.anchoredPosition;
        while (t < 0.6f && rt != null) {
            t += Time.unscaledDeltaTime; 
            rt.anchoredPosition = posInicial + new Vector2(0, t * 150f); 
            rt.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.2f, t / 0.2f); 
            if (t > 0.3f) { Color c = txt.color; c.a = Mathf.Lerp(1f, 0f, (t - 0.3f) / 0.3f); txt.color = c; }
            yield return null;
        }
        if (rt != null) Destroy(rt.gameObject);
    }

    void GerarRastroFantasma() {
        if (botaoJogador1 == null || containerInterfaceJogo == null) return;
        GameObject rastro = new GameObject("RastroFantasma");
        rastro.transform.SetParent(containerInterfaceJogo.transform, false);
        rastro.transform.SetSiblingIndex(botaoJogador1.transform.GetSiblingIndex()); 
        
        Image imgRastro = rastro.AddComponent<Image>();
        imgRastro.raycastTarget = false; 
        
        Image imgBotao = botaoJogador1.GetComponent<Image>();
        if (imgBotao != null) { imgRastro.sprite = imgBotao.sprite; imgRastro.type = imgBotao.type; }
        
        Color corFantasma = isSeason2 ? Color.cyan : new Color(1f, 0.5f, 0f); corFantasma.a = 0.5f; imgRastro.color = corFantasma;
        
        RectTransform rtRastro = rastro.GetComponent<RectTransform>(); RectTransform rtBotao = botaoJogador1.GetComponent<RectTransform>();
        rtRastro.anchoredPosition = rtBotao.anchoredPosition; rtRastro.sizeDelta = rtBotao.sizeDelta; rtRastro.localScale = rtBotao.localScale; rtRastro.localRotation = rtBotao.localRotation;
        StartCoroutine(AnimarFadeRastro(rtRastro, imgRastro));
    }

    IEnumerator AnimarFadeRastro(RectTransform rt, Image img) {
        float t = 0;
        while(t < 0.3f && rt != null) {
            t += Time.unscaledDeltaTime; Color c = img.color; c.a = Mathf.Lerp(0.5f, 0f, t / 0.3f); img.color = c;
            rt.localScale = Vector3.Lerp(rt.localScale, Vector3.one * 0.8f, t / 0.3f); yield return null;
        }
        if (rt != null) Destroy(rt.gameObject);
    }

    IEnumerator EfeitoGlitchDanoVisual() {
        if (overlayCorCenario) {
            for(int i = 0; i < 3; i++) { 
                overlayCorCenario.color = new Color(1f, 0f, 0f, 0.7f); yield return new WaitForSecondsRealtime(0.04f);
                overlayCorCenario.color = new Color(0f, 0f, 0f, 0.8f); yield return new WaitForSecondsRealtime(0.04f);
            }
            overlayCorCenario.color = new Color(0,0,0,0);
        }
    }
    // ==========================================

    string PegarNomeDaNota(int midiCode) {
        int oitava = (midiCode / 12) - 1;
        int notaIndex = midiCode % 12;
        return nomesDasNotas[notaIndex] + oitava;
    }

    bool IsTeclaPreta(int midiNote) {
        int n = midiNote % 12;
        return n == 1 || n == 3 || n == 6 || n == 8 || n == 10;
    }

    void GerarTecladoVisualProcedural() {
        if (painelTecladoVisual88 == null) return;
        foreach (Transform child in painelTecladoVisual88.transform) Destroy(child.gameObject);
        teclasDoPianoUI = new Image[88];
        
        float passoX = 1f / 52f;
        int indiceBranca = 0;

        for (int i = 0; i < 88; i++) {
            int notaMidi = i + 21; 
            if (!IsTeclaPreta(notaMidi)) {
                GameObject obj = new GameObject("Branca_" + notaMidi);
                obj.transform.SetParent(painelTecladoVisual88.transform, false);
                Image img = obj.AddComponent<Image>();
                img.raycastTarget = true;
                img.color = Color.white;
                
                Outline contorno = obj.AddComponent<Outline>();
                contorno.effectColor = Color.black;
                contorno.effectDistance = new Vector2(1, -1);
                
                RectTransform rt = obj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(indiceBranca * passoX, 0f);
                rt.anchorMax = new Vector2((indiceBranca + 1) * passoX, 1f);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; 
                teclasDoPianoUI[i] = img; indiceBranca++;

                AdicionarSuporteTouch(obj, notaMidi);
            }
        }

        indiceBranca = 0;
        for (int i = 0; i < 88; i++) {
            int notaMidi = i + 21;
            if (IsTeclaPreta(notaMidi)) {
                GameObject obj = new GameObject("Preta_" + notaMidi);
                obj.transform.SetParent(painelTecladoVisual88.transform, false);
                Image img = obj.AddComponent<Image>();
                img.raycastTarget = true;
                img.color = Color.black;
                
                RectTransform rt = obj.GetComponent<RectTransform>();
                float centroX = indiceBranca * passoX;
                float larguraPreta = passoX * 0.6f; 
                
                rt.anchorMin = new Vector2(centroX - larguraPreta / 2f, 0.4f); 
                rt.anchorMax = new Vector2(centroX + larguraPreta / 2f, 1f);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                teclasDoPianoUI[i] = img;

                AdicionarSuporteTouch(obj, notaMidi);
            } else { indiceBranca++; }
        }
    }

    void AdicionarSuporteTouch(GameObject teclaObj, int nota) {
        EventTrigger trigger = teclaObj.AddComponent<EventTrigger>();
        int capturedNota = nota;

        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener((data) => { ProcessarNotaOn(capturedNota, 0.8f, false); });
        trigger.triggers.Add(entryDown);

        EventTrigger.Entry entryUp = new EventTrigger.Entry();
        entryUp.eventID = EventTriggerType.PointerUp;
        entryUp.callback.AddListener((data) => { ProcessarNotaOff(capturedNota, false); });
        trigger.triggers.Add(entryUp);
    }

    void InicializarPoolDeAudio() {
        poolDePianos = new AudioSource[poolSize]; 
        poolNotaMidi = new int[poolSize];
        poolTeclaPressionada = new bool[poolSize];
        poolFadeCoroutines = new Coroutine[poolSize];

        for (int i = 0; i < poolSize; i++) {
            GameObject objAudio = new GameObject("AudioPoolPiano_" + i);
            objAudio.transform.SetParent(this.transform); 
            AudioSource src = objAudio.AddComponent<AudioSource>();
            src.playOnAwake = false; src.spatialBlend = 0f; 
            src.bypassReverbZones = true; src.bypassEffects = true; 
            poolDePianos[i] = src;
        }

        if (painelAvisoMidi) painelAvisoMidi.alpha = 0f;
        if (textoMissaoMusical) textoMissaoMusical.text = "";
    }

    // ==========================================
    // MINIS (KEIJIRO) - NEW INPUT SYSTEM INTEGRATION
    // ==========================================
    void OnEnable() {
        try {
            InputSystem.onDeviceChange += OnDeviceChange;
            // Registra dispositivos que já estão conectados ao iniciar
            foreach (var device in InputSystem.devices) {
                if (device is MidiDevice midiDevice) RegisterDevice(midiDevice);
            }
        } catch (System.Exception e) {
            Debug.LogWarning("Minis/InputSystem falhou ou não suportado: " + e.Message);
            controleMidiAtivo = false;
        }
    }

    void OnDisable() {
        try {
            InputSystem.onDeviceChange -= OnDeviceChange;
            foreach (var device in InputSystem.devices) {
                if (device is MidiDevice midiDevice) UnregisterDevice(midiDevice);
            }
        } catch { }
    }

    void OnDestroy() {
        try {
            InputSystem.onDeviceChange -= OnDeviceChange;
            foreach (var device in InputSystem.devices) {
                if (device is MidiDevice midiDevice) UnregisterDevice(midiDevice);
            }
        } catch { }
    }

    void OnDeviceChange(InputDevice device, InputDeviceChange change) {
        if (device is MidiDevice midiDevice) {
            if (change == InputDeviceChange.Added) {
                RegisterDevice(midiDevice);
            } else if (change == InputDeviceChange.Removed) {
                UnregisterDevice(midiDevice);
            }
        }
    }

    void RegisterDevice(MidiDevice device) {
        device.onWillNoteOn += OnMinisNoteOn;
        device.onWillNoteOff += OnMinisNoteOff;
    }

    void UnregisterDevice(MidiDevice device) {
        device.onWillNoteOn -= OnMinisNoteOn;
        device.onWillNoteOff -= OnMinisNoteOff;
    }

    void OnMinisNoteOn(MidiNoteControl noteControl, float velocity) {
        if (!controleMidiAtivo) return;
        int note = noteControl.noteNumber;
        
        if (velocity <= 0.01f) { ProcessarNotaOff(note, false); return; }
        ProcessarNotaOn(note, velocity, false);
        
        if (painelAvisoMidi != null && painelAvisoMidi.alpha == 0f) {
            DispararAvisoOTG("Teclado Conectado via Minis", Color.cyan);
        }
    }

    void OnMinisNoteOff(MidiNoteControl noteControl) {
        ProcessarNotaOff(noteControl.noteNumber, false);
    }
    // ==========================================

    public void AlternarModoPianoSolo(bool ativar) {
        if (easterEggApagaoAtivo) return; 

        modoPianoSoloAtivo = ativar;
        if (ativar) {
            if (telaUnica) telaUnica.gameObject.SetActive(!isPortrait); 
            if (fundoEstaticoImagem) fundoEstaticoImagem.gameObject.SetActive(false); 
            if (painelTecladoVisual88) painelTecladoVisual88.SetActive(true);
            if (botaoJogador1) botaoJogador1.gameObject.SetActive(false);
            progressoMusicaObrigatoria = 0; 
            if (estadoAtual != EstadoJogo.AguardandoPedido && !reproduzindoMidiArquivo) ShowFeedback("RECONSTRUA A MELODIA", Color.cyan);
        } else {
            if (telaUnica) telaUnica.gameObject.SetActive(!isPortrait);
            if (fundoEstaticoImagem) fundoEstaticoImagem.gameObject.SetActive(false);
            if (painelTecladoVisual88) painelTecladoVisual88.SetActive(false);
            
            if (estadoAtual != EstadoJogo.AguardandoPedido) {
                ShowFeedback(isSeason2 ? "SURVIVE!" : "FIGHT!", Color.red);
                ProximoRound(true);
            }
            
            if (reproduzindoMidiArquivo) PararArquivoMidi();
        }
    }

    void DispararAvisoOTG(string mensagem, Color cor) {
        if (painelAvisoMidi == null || textoAvisoMidi == null) return;
        if (animacaoAvisoMidi != null) StopCoroutine(animacaoAvisoMidi);
        animacaoAvisoMidi = StartCoroutine(AnimarAvisoMidi(mensagem, cor));
    }

    IEnumerator AnimarAvisoMidi(string mensagem, Color cor) {
        textoAvisoMidi.text = mensagem; textoAvisoMidi.color = cor;
        float t = 0;
        while (t < 0.3f) { t += Time.unscaledDeltaTime; painelAvisoMidi.alpha = Mathf.Lerp(0f, 1f, t / 0.3f); yield return null; }
        yield return new WaitForSecondsRealtime(3.5f);
        t = 0;
        while (t < 0.5f) { t += Time.unscaledDeltaTime; painelAvisoMidi.alpha = Mathf.Lerp(1f, 0f, t / 0.5f); yield return null; }
    }

    public void AbrirFeedOnline() {
        if (painelFeedSocial) painelFeedSocial.SetActive(true);
        StartCoroutine(BaixarFeedOnline());
    }

    public void FecharFeedOnline() {
        if (painelFeedSocial) painelFeedSocial.SetActive(false);
    }

    IEnumerator BaixarFeedOnline() {
        if (containerPostsUI != null) {
            foreach (Transform child in containerPostsUI) { Destroy(child.gameObject); }
        }

        if (!easterEggApagaoAtivo) ShowFeedback("CONECTANDO...", Color.yellow);
        yield return new WaitForSeconds(1.5f);

        using (UnityWebRequest webRequest = UnityWebRequest.Get(urlJsonFeed)) {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError) {
                if (!easterEggApagaoAtivo) ShowFeedback("MUNDO OFFLINE", Color.red);
            } else {
                if (prefabPostItem != null && containerPostsUI != null) {
                    GameObject novoPost1 = Instantiate(prefabPostItem, containerPostsUI);
                    Text[] textos1 = novoPost1.GetComponentsInChildren<Text>();
                    if (textos1.Length >= 2) { textos1[0].text = "Anônimo01"; textos1[1].text = "Dica: No Easter Egg de invocação, bata os acordes secos, ignore a mão esquerda arpejada, ou o sensor não pega!"; }

                    GameObject novoPost2 = Instantiate(prefabPostItem, containerPostsUI);
                    Text[] textos2 = novoPost2.GetComponentsInChildren<Text>();
                    if (textos2.Length >= 2) { textos2[0].text = "Anônimo02"; textos2[1].text = "Review: A Season 2 na Cela tá insana! O tempo de reação caiu muito."; }
                }
                if (!easterEggApagaoAtivo) ShowFeedback("FEED ATUALIZADO", Color.cyan);
            }
        }
    }

    public void InicializarGooglePlayGames() { 
        if (!ativarPlayGames) return; 
        
        try {
            // PlayGamesPlatform.DebugLogEnabled = true;
            // PlayGamesPlatform.Activate();

            Social.localUser.Authenticate((bool success) => {
                if(success) {
                    Debug.Log("Google Play Games: Autenticado com sucesso!");
                } else {
                    Debug.LogWarning("Google Play Games: Falha na autenticação.");
                }
            });
        } catch (System.Exception e) {
            Debug.LogWarning("Erro fatal ao iniciar GPG (O jogo não vai quebrar por causa disso): " + e.Message);
        }
    }
    
    public void ReportarPontuacaoGooglePlay(int score) { 
        if (!ativarPlayGames || !Social.localUser.authenticated) return; 

        Social.ReportScore(score, idLeaderboardPontuacao, (bool success) => {
            if (success) Debug.Log("Pontuação salva na nuvem!");
        });
        
        AvaliarConquistas(score);
    }
    
    public void MostrarLeaderboard() { 
        if (!ativarPlayGames) return;

        if (Social.localUser.authenticated) {
            Social.ShowLeaderboardUI();
        } else {
            Social.localUser.Authenticate((bool success) => {
                if (success) Social.ShowLeaderboardUI();
                else ShowFeedback("FALHA AO CONECTAR GPG", Color.red);
            });
        }
    }

    private void AvaliarConquistas(int score) {
        if (!ativarPlayGames || !Social.localUser.authenticated) return;

        if (score >= 30 && !string.IsNullOrEmpty(idConquista30Pontos)) ReportarConquista(idConquista30Pontos);
        if (score >= 60 && !string.IsNullOrEmpty(idConquista60Pontos)) ReportarConquista(idConquista60Pontos);
        if (score >= 100 && !string.IsNullOrEmpty(idConquista100Pontos)) ReportarConquista(idConquista100Pontos);
    }

    private void ReportarConquista(string id) {
        Social.ReportProgress(id, 100.0f, (bool success) => {
            if(success) Debug.Log("Conquista desbloqueada na nuvem: " + id);
        });
    }

    public void AbrirExploradorDeArquivos() {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Midi Files", ".mid", ".midi"));
        FileBrowser.SetDefaultFilter(".mid");
        FileBrowser.ShowLoadDialog((paths) => {
            if (paths != null && paths.Length > 0) CarregarETocarArquivoMidiParaEstudo(paths[0]); 
        }, 
        () => { ShowFeedback("IMPORTAÇÃO CANCELADA", Color.red); }, 
        FileBrowser.PickMode.Files, false, null, null, "Selecione a Música MIDI", "Importar");
    }

    public void CarregarETocarArquivoMidiParaEstudo(string caminhoAbsoluto = "") {
        string caminhoDoArquivo = string.IsNullOrEmpty(caminhoAbsoluto) ? Path.Combine(Application.streamingAssetsPath, nomeArquivoMidiEnsino) : caminhoAbsoluto;
        if (!File.Exists(caminhoDoArquivo)) { ShowFeedback("ARQUIVO .MID NÃO ENCONTRADO", Color.red); return; }

        if (rotinaReproducaoMidi != null) StopCoroutine(rotinaReproducaoMidi);
        
        PararTodasAsMusicas(); 
        AlternarModoPianoSolo(true); 
        ShowFeedback(string.IsNullOrEmpty(caminhoAbsoluto) ? "LENDO PARTITURA..." : "IMPORTANDO MÚSICA...", Color.yellow);
        
        rotinaReproducaoMidi = StartCoroutine(ProcessarEReproduzirMidi(caminhoDoArquivo));
        StartCoroutine(GerenciarVideosDoTutorial());
    }

    public void PararArquivoMidi() {
        reproduzindoMidiArquivo = false;
        if (rotinaReproducaoMidi != null) StopCoroutine(rotinaReproducaoMidi);
        
        if (painelNotasCaindo != null) { foreach (Transform child in painelNotasCaindo) Destroy(child.gameObject); }
        teclasPressionadas.Clear(); cordasVibrando.Clear();
        
        if (teclasDoPianoUI != null) {
            for (int i = 0; i < teclasDoPianoUI.Length; i++) {
                if (teclasDoPianoUI[i] != null) teclasDoPianoUI[i].color = IsTeclaPreta(i + 21) ? Color.black : Color.white;
            }
        }
    }

    IEnumerator ProcessarEReproduzirMidi(string caminho) {
        reproduzindoMidiArquivo = true;
        sequenciaAprendizado.Clear();

        try {
            var midiFile = MidiFile.Read(caminho);
            var tempoMap = midiFile.GetTempoMap();
            var notes = midiFile.GetNotes();

            foreach (var note in notes) {
                NotaMidiImportada novaNota = new NotaMidiImportada();
                novaNota.notaMidi = note.NoteNumber;
                novaNota.velocity = note.Velocity / 127f;
                var metricTime = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap);
                novaNota.tempoInicioMilisegundos = (float)metricTime.TotalMicroseconds / 1000f;
                var durTime = TimeConverter.ConvertTo<MetricTimeSpan>(note.Length, tempoMap);
                novaNota.duracaoMilisegundos = (float)durTime.TotalMicroseconds / 1000f;
                sequenciaAprendizado.Add(novaNota);
            }
            sequenciaAprendizado.Sort((a, b) => a.tempoInicioMilisegundos.CompareTo(b.tempoInicioMilisegundos));
        }
        catch (System.Exception) { yield break; }

        if (!easterEggApagaoAtivo) ShowFeedback("TUTORIAL INICIADO!", Color.cyan);
        
        float tempoPreAviso = 1.5f; 
        float tempoInicioTutorial = Time.time + tempoPreAviso;
        int indexVisual = 0; int indexAudio = 0;

        while (indexAudio < sequenciaAprendizado.Count) {
            if (!reproduzindoMidiArquivo) yield break;

            tempoAtualTutorial = Time.time - tempoInicioTutorial;
            tempoUltimaNotaTocadaMidi = Time.time; 

            while (indexVisual < sequenciaAprendizado.Count && tempoAtualTutorial >= (sequenciaAprendizado[indexVisual].tempoInicioMilisegundos / 1000f) - tempoPreAviso) {
                SpawnTutorialFallingNote(sequenciaAprendizado[indexVisual], tempoPreAviso);
                indexVisual++;
            }

            while (indexAudio < sequenciaAprendizado.Count && tempoAtualTutorial >= (sequenciaAprendizado[indexAudio].tempoInicioMilisegundos / 1000f) + compensacaoAudioVisual) {
                var nota = sequenciaAprendizado[indexAudio];
                ProcessarNotaOn(nota.notaMidi, nota.velocity, true); 
                
                if (teclasDoPianoUI != null && teclasDoPianoUI.Length > 0) {
                    int indexTecla = nota.notaMidi - 21; 
                    if (indexTecla >= 0 && indexTecla < teclasDoPianoUI.Length && teclasDoPianoUI[indexTecla] != null) {
                        teclasDoPianoUI[indexTecla].color = Color.magenta; 
                        StartCoroutine(ApagarTeclaEstudoAposDuracao(indexTecla, nota.notaMidi, nota.duracaoMilisegundos / 1000f));
                    }
                }
                indexAudio++;
            }
            yield return null;
        }
        yield return new WaitForSeconds(2.5f); 
        if (!easterEggApagaoAtivo) ShowFeedback("TUTORIAL CONCLUÍDO", Color.green);
        reproduzindoMidiArquivo = false; AlternarModoPianoSolo(false); 
    }

    IEnumerator GerenciarVideosDoTutorial() {
        while (reproduzindoMidiArquivo) {
            VideoClip clipeEscolhido = null;

            if (emocaoAtual == EmocaoMusical.VitoriaMaior || emocaoAtual == EmocaoMusical.MagiaAumentada) {
                if (jogador1ClipesClimax != null && jogador1ClipesClimax.Length > 0) clipeEscolhido = jogador1ClipesClimax[Random.Range(0, jogador1ClipesClimax.Length)];
            } else if (emocaoAtual == EmocaoMusical.TensaoMenor || emocaoAtual == EmocaoMusical.PerigoDiminuto) {
                if (jogador1ClipesTristes != null && jogador1ClipesTristes.Length > 0) clipeEscolhido = jogador1ClipesTristes[Random.Range(0, jogador1ClipesTristes.Length)];
            } else {
                if (jogador1Clipes != null && jogador1Clipes.Length > 0) clipeEscolhido = jogador1Clipes[Random.Range(0, jogador1Clipes.Length)];
            }
            if (clipeEscolhido == null && jogador1Clipes != null && jogador1Clipes.Length > 0) clipeEscolhido = jogador1Clipes[0]; 

            if (clipeEscolhido != null) {
                bool videoRodando = true;
                ReproduzirAcao(clipeEscolhido, () => { videoRodando = false; });
                while (videoRodando && reproduzindoMidiArquivo) yield return null;
            } else { yield return new WaitForSeconds(1f); }
        }
    }

    IEnumerator ApagarTeclaEstudoAposDuracao(int indexTecla, int notaMidi, float duracaoSegundos) {
        yield return new WaitForSeconds(duracaoSegundos);
        ProcessarNotaOff(notaMidi, true); 
        if (teclasDoPianoUI != null && indexTecla < teclasDoPianoUI.Length && teclasDoPianoUI[indexTecla] != null) {
            if (!teclasPressionadas.Contains(notaMidi)) teclasDoPianoUI[indexTecla].color = IsTeclaPreta(notaMidi) ? Color.black : Color.white;
        }
    }

    void SpawnTutorialFallingNote(NotaMidiImportada nota, float tempoQueda) {
        if (painelNotasCaindo == null || teclasDoPianoUI == null) return;
        
        int notaMidi = nota.notaMidi; int indexTecla = notaMidi - 21;
        if (indexTecla < 0 || indexTecla >= 88 || teclasDoPianoUI[indexTecla] == null) return;

        GameObject blocoObj = new GameObject("BlocoTutorial_" + notaMidi);
        blocoObj.transform.SetParent(painelNotasCaindo, false);
        Image img = blocoObj.AddComponent<Image>(); 
        img.raycastTarget = false; 
        img.color = new Color(1f, 0f, 1f, 0.8f); 

        RectTransform rt = blocoObj.GetComponent<RectTransform>();
        RectTransform rtTeclaOrigem = teclasDoPianoUI[indexTecla].rectTransform;
        
        rt.anchorMin = new Vector2(rtTeclaOrigem.anchorMin.x, 0f); rt.anchorMax = new Vector2(rtTeclaOrigem.anchorMax.x, 0f);
        rt.pivot = new Vector2(0.5f, 0f); 
        float altura = (nota.duracaoMilisegundos / 1000f) * 1200f; 
        rt.sizeDelta = new Vector2(0, Mathf.Max(25f, altura)); 
        
        StartCoroutine(AnimarQuedaTutorial(rt, img, nota.tempoInicioMilisegundos / 1000f, tempoQueda));
    }

    IEnumerator AnimarQuedaTutorial(RectTransform rt, Image img, float tempoAlvo, float tempoQueda) {
        float posYInicio = 1100f; float posYFim = -220f;    
        while (rt != null && tempoAtualTutorial < tempoAlvo) {
            float diff = tempoAlvo - tempoAtualTutorial; 
            float t = 1f - (diff / tempoQueda); 
            t = Mathf.Clamp01(t);
            rt.anchoredPosition = new Vector2(0, Mathf.Lerp(posYInicio, posYFim, t));
            yield return null;
        }
        if (rt != null) { 
            rt.anchoredPosition = new Vector2(0, posYFim);
            SpawnParticulaImpacto(rt.position, img.color); 
            float tFade = 0;
            while(tFade < 0.15f && rt != null) {
                tFade += Time.unscaledDeltaTime;
                Color c = img.color; c.a = Mathf.Lerp(0.8f, 0f, tFade / 0.15f); img.color = c;
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject); 
        }
    }

    public void ProcessarNotaOn(int notaReal, float velocity, bool ehDoTutorial = false) {
        tempoUltimaNotaTocadaMidi = Time.time; 
        
        if (usarMotorDeAudioExterno && OnTocarNotaExterna != null) OnTocarNotaExterna.Invoke(notaReal, velocity);
        else TocarSomPianoSintetizado(notaReal, velocity);
        
        if (!ehDoTutorial) EfeitoVisualNota(notaReal);
        
        if (modoPianoSoloAtivo || easterEggApagaoAtivo) {
            if (teclasDoPianoUI != null && teclasDoPianoUI.Length > 0) {
                int indexTecla = notaReal - 21; 
                if (indexTecla >= 0 && indexTecla < teclasDoPianoUI.Length && teclasDoPianoUI[indexTecla] != null) {
                    if (!ehDoTutorial) teclasDoPianoUI[indexTecla].color = Color.cyan; 
                }
            }
        }
        if (velocity > 0.8f) forcaBrilhoBase = 1.0f; else forcaBrilhoBase = 0.5f;

        if (!teclasPressionadas.Contains(notaReal)) teclasPressionadas.Add(notaReal);
        if (!cordasVibrando.Contains(notaReal)) cordasVibrando.Add(notaReal);

        if (modoPianoSoloAtivo) {
            if (!ehDoTutorial) LidarComMusicaObrigatoria(notaReal);
            else { aguardandoCalculoAcorde = true; timerJanelaAcorde = Time.time + 0.08f; }
        } else {
            if (!botaoJogador1) return; 
            
            densidadeAcaoMidi = Mathf.Clamp(densidadeAcaoMidi + 0.15f, 0f, 1.5f); 
            aguardandoCalculoAcorde = true; 
            timerJanelaAcorde = Time.time + 0.08f; 
            
            if (missaoHarmonicaAtual == EmocaoMusical.Neutra) {
                if (pontosJogador1 >= 30 && (emocaoAtual == EmocaoMusical.VitoriaMaior || emocaoAtual == EmocaoMusical.MagiaAumentada)) midiDisparouGolden = true;
                else midiDisparouNormal = true;
            }
        }
    }

    void AnalisarEmocaoDaHarmoniaInstantanea() {
        List<int> fisicas = new List<int>();
        foreach (int nota in teclasPressionadas) { fisicas.Add(nota % 12); }

        bool isE   = fisicas.Contains(4) && fisicas.Contains(8) && fisicas.Contains(11); 
        bool isCsm = fisicas.Contains(1) && fisicas.Contains(4) && fisicas.Contains(8);  
        bool isFs  = fisicas.Contains(6) && fisicas.Contains(10) && fisicas.Contains(1); 
        bool isBm  = fisicas.Contains(11) && fisicas.Contains(2) && fisicas.Contains(6); 

        if (!easterEggApagaoAtivo) {
            if (isE) LógicaDeInvocaçãoSustainedDream(4, "E Maj");
        } 
        else {
            if (isCsm) LógicaDeInvocaçãoSustainedDream(1, "C#m");
            else if (isFs) LógicaDeInvocaçãoSustainedDream(6, "F# Maj");
            else if (isBm) LógicaDeInvocaçãoSustainedDream(11, "Bm");
            else if (isE) LógicaDeInvocaçãoSustainedDream(4, "E Maj");
            else if (fisicas.Count >= 3) { 
                ultimoAcordeFisico = -1;
                RestaurarJogoDoApagãoEasterEgg(); 
            }
        }
        
        if (fisicas.Count == 0) ultimoAcordeFisico = -1; 

        if (cordasVibrando.Count < 2) { 
            MudarEmocao(EmocaoMusical.Neutra); 
            if (!easterEggApagaoAtivo) ultimoAcordeDetectado = "Aguardando Acorde..."; 
            return; 
        }

        List<int> acordesParaAnalisar = new List<int>(cordasVibrando);
        acordesParaAnalisar.Sort();
        int tonica = acordesParaAnalisar[0]; 

        bool tem3 = false, tem4 = false, tem6 = false, tem8 = false;
        for (int i = 0; i < acordesParaAnalisar.Count; i++) {
            int intervalo = (acordesParaAnalisar[i] - tonica) % 12;
            if (intervalo == 3) tem3 = true; if (intervalo == 4) tem4 = true;
            if (intervalo == 6) tem6 = true; if (intervalo == 8) tem8 = true;
        }

        EmocaoMusical novaEmocao = EmocaoMusical.Neutra;
        if (tem3 && tem6) { novaEmocao = EmocaoMusical.PerigoDiminuto; if (!easterEggApagaoAtivo) ultimoAcordeDetectado = "Acorde Diminuto"; }
        else if (tem4 && tem8) { novaEmocao = EmocaoMusical.MagiaAumentada; if (!easterEggApagaoAtivo) ultimoAcordeDetectado = "Acorde Aumentado"; midiDisparouGolden = true; }
        else if (tem3) { novaEmocao = EmocaoMusical.TensaoMenor; if (!easterEggApagaoAtivo) ultimoAcordeDetectado = "Acorde Menor"; }
        else if (tem4) { novaEmocao = EmocaoMusical.VitoriaMaior; if (!easterEggApagaoAtivo) ultimoAcordeDetectado = "Acorde Maior"; midiDisparouGolden = true; }

        if (novaEmocao != emocaoAtual && novaEmocao != EmocaoMusical.Neutra) MudarEmocao(novaEmocao);
        
        if (!modoPianoSoloAtivo) {
            if (missaoHarmonicaAtual != EmocaoMusical.Neutra && emocaoAtual == missaoHarmonicaAtual) {
                midiDisparouGolden = true; if (textoMissaoMusical) textoMissaoMusical.color = Color.green;
            } else { midiDisparouNormal = true; }
        }
    }

    void LógicaDeInvocaçãoSustainedDream(int tônicaMod12, string acordeCompleto) {
        if (tônicaMod12 == ultimoAcordeFisico) return; 
        ultimoAcordeFisico = tônicaMod12;

        if (tônicaMod12 == 4) { 
            ultimoAcordeDetectado = "E Maj (INVOCAÇÃO!)";
            if (!easterEggApagaoAtivo) StartCoroutine(AtivarApagãoMusicalEasterEgg());
            sequenceStep = 1; 
            return;
        }

        if (easterEggApagaoAtivo) {
            bool correctChord = false;
            if (sequenceStep == 1 && tônicaMod12 == 1) { correctChord = true; ShowFeedback("C#m ✓", Color.yellow); sequenceStep = 2; }
            else if (sequenceStep == 2 && tônicaMod12 == 6) { correctChord = true; ShowFeedback("F# Maj ✓", Color.yellow); sequenceStep = 3; }
            else if (sequenceStep == 3 && tônicaMod12 == 11) { correctChord = true; ShowFeedback("Bm ✓", Color.yellow); sequenceStep = 0; } 

            if (!correctChord) { 
                ultimoAcordeDetectado = acordeCompleto + " (Quebra!)";
                ultimoAcordeFisico = -1;
                RestaurarJogoDoApagãoEasterEgg();
            } else {
                ultimoAcordeDetectado = acordeCompleto + " (Sustained)";
            }
        }
    }

    public void ProcessarNotaOff(int notaReal, bool ehDoTutorial = false) {
        teclasPressionadas.Remove(notaReal);

        if (modoPianoSoloAtivo || easterEggApagaoAtivo) {
            if (teclasDoPianoUI != null && teclasDoPianoUI.Length > 0) {
                int indexTecla = notaReal - 21; 
                if (indexTecla >= 0 && indexTecla < teclasDoPianoUI.Length && teclasDoPianoUI[indexTecla] != null) {
                    teclasDoPianoUI[indexTecla].color = IsTeclaPreta(notaReal) ? Color.black : Color.white;
                }
            }
        }
        
        if (usarMotorDeAudioExterno && OnPararNotaExterna != null) OnPararNotaExterna.Invoke(notaReal);
        cordasVibrando.Remove(notaReal);

        if (!usarMotorDeAudioExterno) {
            for (int i = 0; i < poolSize; i++) {
                if (poolNotaMidi[i] == notaReal && poolTeclaPressionada[i]) {
                    poolTeclaPressionada[i] = false; 
                    if (poolFadeCoroutines[i] != null) StopCoroutine(poolFadeCoroutines[i]);
                    poolFadeCoroutines[i] = StartCoroutine(FadeOutCorda(i));
                }
            }
        }
    }

    IEnumerator FadeOutCorda(int index) {
        AudioSource src = poolDePianos[index];
        float startVol = src.volume; float t = 0; float duration = 0.15f; 
        while (t < duration && src.isPlaying) { t += Time.unscaledDeltaTime; src.volume = Mathf.Lerp(startVol, 0f, t / duration); yield return null; }
        src.Stop(); src.volume = startVol; 
    }

    void UpdateVFX() {
        if (brilhoPlacar == null || painelPlacar == null) return;
        float oscilacao = Mathf.PingPong(Time.unscaledTime * 2f, 0.3f);
        float intensidade = forcaBrilhoBase + oscilacao;
        if (emocaoAtual == EmocaoMusical.VitoriaMaior || emocaoAtual == EmocaoMusical.MagiaAumentada) {
            brilhoPlacar.color = Color.Lerp(brilhoPlacar.color, new Color(0, 1, 1, intensidade), Time.unscaledDeltaTime * 5f);
            painelPlacar.localScale = Vector3.one * (1f + oscilacao * 0.2f); 
        } else if (emocaoAtual == EmocaoMusical.TensaoMenor || emocaoAtual == EmocaoMusical.PerigoDiminuto) {
            brilhoPlacar.color = Color.Lerp(brilhoPlacar.color, new Color(0.5f, 0, 1, intensidade), Time.unscaledDeltaTime * 5f);
            painelPlacar.localScale = Vector3.Lerp(painelPlacar.localScale, Vector3.one, Time.unscaledDeltaTime * 2f);
        } else {
            brilhoPlacar.color = Color.Lerp(brilhoPlacar.color, new Color(1, 1, 1, 0.2f), Time.unscaledDeltaTime * 2f);
            painelPlacar.localScale = Vector3.Lerp(painelPlacar.localScale, Vector3.one, Time.unscaledDeltaTime * 2f);
        }
    }

    void SpawnParticulaImpacto(Vector3 posicaoMundo, Color cor) {
        if (containerInterfaceJogo == null) return;
        for (int i = 0; i < 6; i++) { 
            GameObject pObj = new GameObject(isSeason2 ? "SparkGhost" : "Spark");
            pObj.transform.SetParent(containerInterfaceJogo.transform, false);
            Image img = pObj.AddComponent<Image>(); 
            img.raycastTarget = false; 
            img.color = cor;
            RectTransform rt = pObj.GetComponent<RectTransform>();
            rt.position = posicaoMundo; rt.sizeDelta = new Vector2(20, 20);
            StartCoroutine(AnimarParticula(rt, img));
        }
    }

    IEnumerator AnimarParticula(RectTransform rt, Image img) {
        Vector2 direcao = Random.insideUnitCircle.normalized * Random.Range(150f, 500f);
        float t = 0; Color c = img.color;
        while (t < 0.5f) {
            t += Time.unscaledDeltaTime; 
            if (rt == null) yield break;
            rt.position += (Vector3)direcao * Time.unscaledDeltaTime;
            rt.Rotate(0, 0, 800f * Time.unscaledDeltaTime);
            c.a = 1f - (t / 0.5f); img.color = c;
            rt.localScale = Vector3.one * (1f - (t / 0.5f));
            yield return null;
        }
        if (rt != null) Destroy(rt.gameObject);
    }

    IEnumerator EfeitoStandAura(Color corAura) {
        if (isPortrait || easterEggApagaoAtivo) {
            GameObject flash = new GameObject("PortraitAura");
            flash.transform.SetParent(containerInterfaceJogo.transform, false);
            flash.transform.SetAsFirstSibling();
            Image img = flash.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = new Color(corAura.r, corAura.g, corAura.b, 0.8f);
            
            RectTransform rt = flash.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            float t = 0;
            while(t < 0.35f) {
                t += Time.unscaledDeltaTime;
                Color c = img.color; c.a = Mathf.Lerp(0.8f, 0f, t/0.35f); img.color = c;
                yield return null;
            }
            Destroy(flash);
        } else {
            if(telaUnica == null) yield break;
            GameObject fantasma = new GameObject("StandAura");
            fantasma.transform.SetParent(telaUnica.transform.parent, false);
            fantasma.transform.SetSiblingIndex(telaUnica.transform.GetSiblingIndex());
            RawImage img = fantasma.AddComponent<RawImage>();
            img.raycastTarget = false;
            img.texture = telaUnica.texture;
            img.color = new Color(corAura.r, corAura.g, corAura.b, 0.6f);
            RectTransform rt = fantasma.GetComponent<RectTransform>();
            rt.anchorMin = telaUnica.rectTransform.anchorMin; rt.anchorMax = telaUnica.rectTransform.anchorMax;
            rt.pivot = telaUnica.rectTransform.pivot; rt.sizeDelta = telaUnica.rectTransform.sizeDelta;
            rt.anchoredPosition = telaUnica.rectTransform.anchoredPosition; rt.localRotation = telaUnica.rectTransform.localRotation;

            float t = 0;
            while(t < 0.35f) {
                t += Time.unscaledDeltaTime;
                rt.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.15f, t/0.35f);
                Color c = img.color; c.a = Mathf.Lerp(0.6f, 0f, t/0.35f); img.color = c;
                yield return null;
            }
            Destroy(fantasma);
        }
    }

    IEnumerator ZoomDramatico() {
        if(containerInterfaceJogo == null) yield break;
        RectTransform rt = containerInterfaceJogo.GetComponent<RectTransform>();
        float t = 0;
        while(t < 0.08f) { 
            t += Time.unscaledDeltaTime; 
            rt.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.05f, t/0.08f); 
            yield return null; 
        }
        t = 0;
        while(t < 0.15f) { 
            t += Time.unscaledDeltaTime; 
            rt.localScale = Vector3.Lerp(Vector3.one * 1.05f, Vector3.one, t/0.15f); 
            yield return null; 
        }
        rt.localScale = Vector3.one;
    }

    void EfeitoVisualNota(int notaMidi) {
        if (painelNotasCaindo == null) return;
        
        if (modoPianoSoloAtivo || easterEggApagaoAtivo) {
            if (teclasDoPianoUI == null || teclasDoPianoUI.Length == 0) return;
            int indexTecla = notaMidi - 21;
            if (indexTecla < 0 || indexTecla >= 88) return;

            Color corBase = GetCorPelaVibe(); 
            corBase.a = 0.8f; 

            RectTransform rtTeclaOrigem = teclasDoPianoUI[indexTecla].rectTransform;
            SpawnParticulaImpacto(rtTeclaOrigem.position, corBase); 

        } else {
            GameObject blocoObj = new GameObject("Bloco_" + notaMidi);
            blocoObj.transform.SetParent(painelNotasCaindo, false);
            Image img = blocoObj.AddComponent<Image>();
            img.raycastTarget = false; 
            img.color = GetCorPelaVibe();
            RectTransform rt = blocoObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(isSeason2 ? 35f : 25f, Random.Range(100f, 200f)); 
            
            float limiteLargura = (painelNotasCaindo.rect.width / 2f) - 40f; 
            float posMapeadaX = Mathf.Lerp(-limiteLargura, limiteLargura, Mathf.Clamp01((notaMidi - 21f) / 87f));
            
            rt.anchoredPosition = new Vector2(posMapeadaX, 700f); 
            
            if (isSeason2) {
                GameObject textoObj = new GameObject("TextoNota");
                textoObj.transform.SetParent(blocoObj.transform, false);
                Text txt = textoObj.AddComponent<Text>();
                txt.raycastTarget = false;
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.text = PegarNomeDaNota(notaMidi); txt.fontSize = 20; 
                txt.alignment = TextAnchor.LowerCenter; txt.color = new Color(0f, 0f, 0f, 0.8f); 
                RectTransform rtTxt = textoObj.GetComponent<RectTransform>();
                rtTxt.sizeDelta = new Vector2(40f, 30f); rtTxt.anchoredPosition = new Vector2(0, 20f); 
            }
            StartCoroutine(AnimarQuedaSimples(rt, img));
        }
    }

    IEnumerator AnimarQuedaSimples(RectTransform rt, Image img) {
        float t = 0; Vector2 posInicial = rt.anchoredPosition;
        while (t < 1.0f && rt != null) {
            t += Time.deltaTime; rt.anchoredPosition = Vector2.Lerp(posInicial, new Vector2(posInicial.x, -600f), t);
            if (t > 0.7f) { Color c = img.color; c.a = Mathf.Lerp(c.a, 0f, Time.deltaTime * 10f); img.color = c; }
            yield return null;
        }
        if (rt != null) { SpawnParticulaImpacto(rt.position, img.color); Destroy(rt.gameObject); } 
    }

    Color GetCorPelaVibe() {
        if (isSeason2) {
            if (emocaoAtual == EmocaoMusical.VitoriaMaior) return new Color(1f, 0.9f, 0.1f, 0.8f);
            if (emocaoAtual == EmocaoMusical.TensaoMenor) return new Color(0.6f, 0f, 1f, 0.8f);
            if (emocaoAtual == EmocaoMusical.PerigoDiminuto) return new Color(1f, 0.1f, 0.1f, 0.8f);
            if (emocaoAtual == EmocaoMusical.MagiaAumentada) return new Color(0f, 0.9f, 1f, 0.8f);
            return new Color(0.3f, 0.9f, 1f, 0.6f); 
        } else {
            if (emocaoAtual == EmocaoMusical.VitoriaMaior) return new Color(1f, 0.9f, 0.1f, 0.8f);
            if (emocaoAtual == EmocaoMusical.TensaoMenor) return new Color(0.7f, 0.2f, 1f, 0.8f);
            if (emocaoAtual == EmocaoMusical.PerigoDiminuto) return new Color(1f, 0.2f, 0.2f, 0.8f);
            if (emocaoAtual == EmocaoMusical.MagiaAumentada) return new Color(0f, 0.9f, 1f, 0.8f);
            return new Color(1f, 1f, 1f, 0.8f); 
        }
    }

    void LidarComMusicaObrigatoria(int notaMidi) {
        if (musicaObrigatoriaMidi == null || musicaObrigatoriaMidi.Length == 0) return;
        if (notaMidi == musicaObrigatoriaMidi[progressoMusicaObrigatoria]) {
            progressoMusicaObrigatoria++;
            ShowFeedback("PERFECT NOTE!", Color.green);
            if (progressoMusicaObrigatoria >= musicaObrigatoriaMidi.Length) {
                ShowFeedback(isSeason2 ? "MEMÓRIA RESTAURADA!" : "MELODIA COMPLETA!", Color.yellow);
                AlternarModoPianoSolo(false); 
            }
        } else {
            progressoMusicaObrigatoria = 0; 
            ShowFeedback("Errou! Recomece a Melodia.", Color.red);
        }
    }

    void TocarSomPianoSintetizado(int notaMidi, float forcaFisica) {
        if (poolDePianos == null) return;
        AudioClip clipeEscolhido = somPianoBaseC4;
        int notaBase = 60; 

        if (bancoDePianoMultisample != null && bancoDePianoMultisample.Length > 0) {
            int menorDistancia = int.MaxValue;
            bool encontrouAmostraValida = false;
            foreach (var amostra in bancoDePianoMultisample) {
                if (amostra.clipeDeAudio == null || amostra.notaMidiBase <= 20) continue; 
                int distancia = Mathf.Abs(notaMidi - amostra.notaMidiBase);
                if (distancia < menorDistancia) {
                    menorDistancia = distancia; clipeEscolhido = amostra.clipeDeAudio;
                    notaBase = amostra.notaMidiBase; encontrouAmostraValida = true;
                }
            }
            if (!encontrouAmostraValida) { clipeEscolhido = somPianoBaseC4; notaBase = 60; }
        }

        if (clipeEscolhido == null) return;

        int idx = indicePoolPiano;
        AudioSource fonte = poolDePianos[idx];
        if (poolFadeCoroutines[idx] != null) StopCoroutine(poolFadeCoroutines[idx]);
        
        fonte.clip = clipeEscolhido;
        fonte.pitch = Mathf.Pow(2f, (notaMidi - (float)notaBase) / 12f);
        fonte.volume = forcaFisica * 0.8f;
        fonte.Play();

        poolNotaMidi[idx] = notaMidi; poolTeclaPressionada[idx] = true;
        indicePoolPiano++; if (indicePoolPiano >= poolSize) indicePoolPiano = 0; 
    }

    IEnumerator AtivarApagãoMusicalEasterEgg() {
        easterEggApagaoAtivo = true;
        ShowFeedback("SUSTAINED DREAM...", Color.magenta);
        if (audioSfx && sfxQuebraExpectativa) audioSfx.PlayOneShot(sfxQuebraExpectativa);
        
        if (telaUnica) telaUnica.gameObject.SetActive(false);
        if (painelNoticiasTikTok) painelNoticiasTikTok.SetActive(false);
        if (painelOverlayTV) painelOverlayTV.gameObject.SetActive(false);
        
        if (fundoEstaticoImagem) {
            fundoEstaticoImagem.gameObject.SetActive(true);
            fundoEstaticoImagem.color = Color.black; 
        }
        
        if (painelTecladoVisual88) painelTecladoVisual88.SetActive(true);
        if (overlayCorCenario) overlayCorCenario.color = new Color(0.8f, 0f, 1f, 0.4f); 

        yield break;
    }

    void RestaurarJogoDoApagãoEasterEgg() {
        if (!easterEggApagaoAtivo) return;
        ShowFeedback("WAKE UP!", Color.cyan);
        
        easterEggApagaoAtivo = false;
        sequenceStep = 0;

        AdaptarParaOrientacao(isPortrait); 
        if (fundoEstaticoImagem) fundoEstaticoImagem.gameObject.SetActive(modoPianoSoloAtivo);
        if (painelTecladoVisual88) painelTecladoVisual88.SetActive(modoPianoSoloAtivo);
        
        if (overlayCorCenario) overlayCorCenario.color = new Color(0f, 0f, 0f, 0f);
    }
    
    void MudarEmocao(EmocaoMusical novaEmocao) {
        emocaoAtual = novaEmocao;
        if (overlayCorCenario == null) return;

        StopCoroutine("EfeitoTransicaoCor");
        Color corAlvo = new Color(0, 0, 0, 0);

        if (isSeason2) {
            switch (emocaoAtual) {
                case EmocaoMusical.TensaoMenor: corAlvo = new Color(0.4f, 0f, 0.4f, 0.6f); break;
                case EmocaoMusical.PerigoDiminuto: corAlvo = new Color(0.8f, 0f, 0f, 0.7f); break;
                case EmocaoMusical.VitoriaMaior: corAlvo = new Color(0.8f, 0.8f, 0f, 0.4f); break;
                case EmocaoMusical.MagiaAumentada: corAlvo = new Color(0f, 0.8f, 1f, 0.5f); StartCoroutine(TriggerFlashGiovanna()); break;
                default: corAlvo = new Color(0f, 0f, 0f, 0f); break;
            }
        } else {
            switch (emocaoAtual) {
                case EmocaoMusical.TensaoMenor: corAlvo = new Color(0.2f, 0f, 0.4f, 0.5f); break;
                case EmocaoMusical.PerigoDiminuto: corAlvo = new Color(0.7f, 0f, 0f, 0.6f); break;
                case EmocaoMusical.VitoriaMaior: corAlvo = new Color(1f, 0.8f, 0f, 0.35f); break;
                case EmocaoMusical.MagiaAumentada: corAlvo = new Color(0f, 0.8f, 1f, 0.45f); StartCoroutine(TriggerFlashGiovanna()); break;
                default: corAlvo = new Color(0f, 0f, 0f, 0f); break;
            }
        }
        StartCoroutine("EfeitoTransicaoCor", corAlvo);
    }

    IEnumerator EfeitoTransicaoCor(Color alvo) {
        if (!overlayCorCenario) yield break;
        Color atual = overlayCorCenario.color;
        float t = 0;
        
        if (isPortrait && alvo.a > 0.1f) alvo.a += 0.3f; 
        
        while(t < 1f) { t += Time.unscaledDeltaTime * 4f; overlayCorCenario.color = Color.Lerp(atual, alvo, t); yield return null; }
    }

    void Start() {
        if (containerInterfaceJogo) containerInterfaceJogo.SetActive(false);
        if (painelSelecaoCenario) painelSelecaoCenario.SetActive(false);
        if (painelPlacar) { painelPlacar.gameObject.SetActive(false); painelPlacar.anchoredPosition = new Vector2(painelPlacar.anchoredPosition.x, posicaoYEscondido); }
        if (textoAvisoFight) textoAvisoFight.gameObject.SetActive(false);
        if (painelFeedSocial) painelFeedSocial.SetActive(false);
        if (botaoJogador1 != null) botaoJogador1.gameObject.SetActive(false);
        foreach (var p in publicoFase) if(p) p.gameObject.SetActive(false);

        InicializarGooglePlayGames();

        vpAtivo = vp1; vpEmEspera = vp2; texAtiva = textureA; texEspera = textureB;
        if (telaUnica != null) telaUnica.texture = texAtiva;
        DetectarIdiomaDoCelular(); 
        janelaPerfeitaAtual = margemPerfeito;
        
        vp1.audioOutputMode = VideoAudioOutputMode.None; 
        vp2.audioOutputMode = VideoAudioOutputMode.None;
        vp1.playbackSpeed = 1f; vp2.playbackSpeed = 1f;
        volumeAlvoMusica = 1f;

        InicializarPoolDeAudio(); 
        GerarTecladoVisualProcedural();
        
        isPortrait = Screen.height > Screen.width;
        AdaptarParaOrientacao(isPortrait);

        botaoAceitar.onClick.AddListener(() => { 
            if (audioSfx && sfxMenuConfirm) audioSfx.PlayOneShot(sfxMenuConfirm);
            if (painelPedido) painelPedido.SetActive(false);
            if (painelNoticiasTikTok) painelNoticiasTikTok.SetActive(false);
            if (painelSelecaoCenario && !isPortrait) painelSelecaoCenario.SetActive(true);
            else EscolherCenario(TipoCenario.SalaEscolar); 
        });

        if (btnCenarioEscola) btnCenarioEscola.onClick.AddListener(() => EscolherCenario(TipoCenario.SalaEscolar));
        if (btnCenarioNapoles) btnCenarioNapoles.onClick.AddListener(() => EscolherCenario(TipoCenario.NapolesPublico));
        if (btnCenarioCela) btnCenarioCela.onClick.AddListener(() => EscolherCenario(TipoCenario.CelaIsolamento));
        
        botaoJogador1.onClick.AddListener(() => RegistrarCliqueJogador(""));
        
        if (botaoTutorialMidi) {
            botaoTutorialMidi.onClick.RemoveAllListeners();
            botaoTutorialMidi.onClick.AddListener(() => CarregarETocarArquivoMidiParaEstudo(""));
        }

        if (botaoImportarMidi) {
            botaoImportarMidi.onClick.RemoveAllListeners();
            botaoImportarMidi.onClick.AddListener(() => AbrirExploradorDeArquivos());
        }

        if (botaoLeaderboard) {
            botaoLeaderboard.onClick.RemoveAllListeners();
            botaoLeaderboard.onClick.AddListener(() => MostrarLeaderboard());
        }

        if (textoPontuacao != null) tamanhoFonteOriginal = textoPontuacao.fontSize;
        
        PararTodasAsMusicas();
        if (audioMenuSuno) audioMenuSuno.Play(); 
        
        AlternarModoPianoSolo(false);
        ConfigurarTextosDaLuta(); 

        if (introDesafio) TocarVideoSemCallback(introDesafio);
    }
    
    void AdaptarParaOrientacao(bool portrait) {
        if (easterEggApagaoAtivo) return; 

        if (telaUnica) telaUnica.gameObject.SetActive(!portrait && !modoPianoSoloAtivo); 
        
        if (botaoTutorialMidi) botaoTutorialMidi.gameObject.SetActive(!portrait);
        if (botaoImportarMidi) botaoImportarMidi.gameObject.SetActive(!portrait);
        if (botaoLeaderboard) botaoLeaderboard.gameObject.SetActive(!portrait);
        
        if (overlayCorCenario) {
            Color c = overlayCorCenario.color;
            if(c.a > 0.1f) c.a = portrait ? Mathf.Clamp01(c.a + 0.3f) : Mathf.Clamp01(c.a - 0.3f);
            overlayCorCenario.color = c;
        }
    }

    void ConfigurarTextosDaLuta() {
        if (isSeason2) {
            switch (idiomaAtual) {
                case Language.English:
                    if(textoNotificacaoDesafio) textoNotificacaoDesafio.text = "Inmate Mikalle. Report to the Arena.";
                    if(textoBotaoAceitar) textoBotaoAceitar.text = "ENTER"; if(textoBotaoRecusar) textoBotaoRecusar.text = "STAY IN CELL";
                    if(textoLabelEnergia) textoLabelEnergia.text = "SOUL GAUGE"; if(textoNewsPerfil) textoNewsPerfil.text = "Sustained Dream: Requiem";
                    if(textoNewsLegenda) textoNewsLegenda.text = "Rumors of a phantom piano in cell block 4...";
                    if(textoTituloCenario) textoTituloCenario.text = "CHOOSE BATTLEGROUND";
                    break;
                default:
                    if(textoNotificacaoDesafio) textoNotificacaoDesafio.text = "Detenta Mikalle. Compareça à Arena.";
                    if(textoBotaoAceitar) textoBotaoAceitar.text = "ENTRAR"; if(textoBotaoRecusar) textoBotaoRecusar.text = "FICAR NA CELA";
                    if(textoLabelEnergia) textoLabelEnergia.text = "ALMA"; if(textoNewsPerfil) textoNewsPerfil.text = "Sustained Dream: Requiem";
                    if(textoNewsLegenda) textoNewsLegenda.text = "O piano fantasma ecoa no bloco 4...";
                    if(textoTituloCenario) textoTituloCenario.text = "LOCAL DO DUELO";
                    break;
            }
        } else {
            switch (idiomaAtual) {
                case Language.English:
                    if(textoNotificacaoDesafio) textoNotificacaoDesafio.text = "Lôrna, darling! Style Showcase?";
                    if(textoBotaoAceitar) textoBotaoAceitar.text = "FIGHT"; if(textoBotaoRecusar) textoBotaoRecusar.text = "REFUSE";
                    if(textoLabelEnergia) textoLabelEnergia.text = "GAUGE"; if(textoNewsPerfil) textoNewsPerfil.text = "Sustained Dream";
                    if(textoNewsLegenda) textoNewsLegenda.text = "Lôrna Mikalle crushing it at the Ping-Pong Club!";
                    if(textoTituloCenario) textoTituloCenario.text = "CHOOSE STAGE";
                    break;
                default:
                    if(textoNotificacaoDesafio) textoNotificacaoDesafio.text = "Lôrna, amada! Style Showcase?";
                    if(textoBotaoAceitar) textoBotaoAceitar.text = "COMBATER"; if(textoBotaoRecusar) textoBotaoRecusar.text = "FUGIR";
                    if(textoLabelEnergia) textoLabelEnergia.text = "ENERGIA"; if(textoNewsPerfil) textoNewsPerfil.text = "Sustained Dream";
                    if(textoNewsLegenda) textoNewsLegenda.text = "Lôrna Mikalle arrasando no Ping-Pong Clube!";
                    if(textoTituloCenario) textoTituloCenario.text = "ESCOLHA O CENÁRIO";
                    break;
            }
        }
    }

    string GetTexto(string chave) {
        if (isSeason2) {
            switch (idiomaAtual) {
                case Language.English: return chave == "R1" ? "MATCH 1..." : "SURVIVE!";
                case Language.Espanol: return chave == "R1" ? "DUELO 1..." : "¡SOBREVIVE!";
                case Language.Italiano: return chave == "R1" ? "PARTITA 1..." : "SOPRAVVIVI!";
                default: return chave == "R1" ? "DUELO 1..." : "SOBREVIVA!";
            }
        } else {
            switch (idiomaAtual) {
                case Language.English: return chave == "R1" ? "ROUND 1..." : "FIGHT!";
                case Language.Espanol: return chave == "R1" ? "ROUND 1..." : "¡A PELEAR!";
                case Language.Italiano: return chave == "R1" ? "PRIMO ROUND..." : "COMBATTE!";
                default: return chave == "R1" ? "ROUND 1..." : "COMBATER!";
            }
        }
    }

    public void EscolherCenario(TipoCenario cenarioEscolhido) {
        if (audioSfx && sfxMenuConfirm) audioSfx.PlayOneShot(sfxMenuConfirm);
        cenarioAtual = cenarioEscolhido;
        
        isSeason2 = (cenarioAtual == TipoCenario.CelaIsolamento); 
        ConfigurarTextosDaLuta(); 

        if (painelSelecaoCenario) painelSelecaoCenario.SetActive(false);
        if (ambienteEscola) ambienteEscola.SetActive(cenarioAtual == TipoCenario.SalaEscolar);
        if (ambienteNapoles) ambienteNapoles.SetActive(cenarioAtual == TipoCenario.NapolesPublico);
        if (ambienteCela) ambienteCela.SetActive(cenarioAtual == TipoCenario.CelaIsolamento);

        if (cenarioAtual == TipoCenario.CelaIsolamento) {
            if (inicioPartidaCela) inicioPartida = inicioPartidaCela;
            if (jogador1ClipesCela != null && jogador1ClipesCela.Length > 0) jogador1Clipes = jogador1ClipesCela;
            if (jogador2ClipesCela != null && jogador2ClipesCela.Length > 0) jogador2Clipes = jogador2ClipesCela;
            if (jogador1PerdeuCela) jogador1Perdeu = jogador1PerdeuCela;
            if (jogador2PerdeuCela) jogador2Perdeu = jogador2PerdeuCela;
            if (goldenChordVideoCela) goldenChordVideo = goldenChordVideoCela;
            if (videoEventoHeadshotCela) videoEventoHeadshot = videoEventoHeadshotCela;
            if (jogador1ClipesClimaxCela != null && jogador1ClipesClimaxCela.Length > 0) jogador1ClipesClimax = jogador1ClipesClimaxCela;
            if (jogador1ClipesTristesCela != null && jogador1ClipesTristesCela.Length > 0) jogador1ClipesTristes = jogador1ClipesTristesCela;
        } 
        else if (cenarioAtual == TipoCenario.NapolesPublico) {
            if (inicioPartidaNapoles) inicioPartida = inicioPartidaNapoles;
            if (jogador1ClipesNapoles != null && jogador1ClipesNapoles.Length > 0) jogador1Clipes = jogador1ClipesNapoles;
            if (jogador2ClipesNapoles != null && jogador2ClipesNapoles.Length > 0) jogador2Clipes = jogador2ClipesNapoles;
            if (jogador1PerdeuNapoles) jogador1Perdeu = jogador1PerdeuNapoles;
            if (jogador2PerdeuNapoles) jogador2Perdeu = jogador2PerdeuNapoles;
            if (goldenChordVideoNapoles) goldenChordVideo = goldenChordVideoNapoles;
            if (videoEventoHeadshotNapoles) videoEventoHeadshot = videoEventoHeadshotNapoles;
            if (jogador1ClipesClimaxNapoles != null && jogador1ClipesClimaxNapoles.Length > 0) jogador1ClipesClimax = jogador1ClipesClimaxNapoles;
            if (jogador1ClipesTristesNapoles != null && jogador1ClipesTristesNapoles.Length > 0) jogador1ClipesTristes = jogador1ClipesTristesNapoles;
        } 
        else { 
            if (inicioPartidaBase) inicioPartida = inicioPartidaBase;
            if (jogador1ClipesBase != null && jogador1ClipesBase.Length > 0) jogador1Clipes = jogador1ClipesBase;
            if (jogador2ClipesBase != null && jogador2ClipesBase.Length > 0) jogador2Clipes = jogador2ClipesBase;
            if (jogador1PerdeuBase) jogador1Perdeu = jogador1PerdeuBase;
            if (jogador2PerdeuBase) jogador2Perdeu = jogador2PerdeuBase;
            if (goldenChordVideoBase) goldenChordVideo = goldenChordVideoBase;
            if (videoEventoHeadshotBase) videoEventoHeadshot = videoEventoHeadshotBase;
            if (jogador1ClipesClimaxBase != null && jogador1ClipesClimaxBase.Length > 0) jogador1ClipesClimax = jogador1ClipesClimaxBase;
            if (jogador1ClipesTristesBase != null && jogador1ClipesTristesBase.Length > 0) jogador1ClipesTristes = jogador1ClipesTristesBase;
        }

        ReproduzirAcao(inicioPartida, () => StartCoroutine(SequenciaIntroLuta())); 
    }

    void IniciarPingPong() {
        if (pontosJogador1 == 0) pontosJogador1 = PlayerPrefs.GetInt(isSeason2 ? "Mikalle_S2_PontoFinal" : "Mikalle_PontoFinal", 0);
        
        pontosJogador2 = 0; proximoAlvoFase = ((pontosJogador1 / 15) + 1) * 15;
        multiplicadorAdrenalina = 1.0f + (pontosJogador1 * 0.005f); energiaAtual = 0;
        vidasLorna = 3;
        
        botaoMovelAtivo = false; deveMoverBotaoSuave = false;
        janelaPerfeitaAtual = margemPerfeito;
        if (botaoJogador1) posicaoAlvoBotao = botaoJogador1.GetComponent<RectTransform>().anchoredPosition;

        if (rotinaMoverBotao != null) StopCoroutine(rotinaMoverBotao);

        PararTodasAsMusicas(); 
        if (usarTemaEspecial && audioCoverEspecial) audioCoverEspecial.Play();
        else if (audioMusicaFundo) { audioMusicaFundo.loop = true; audioMusicaFundo.Play(); }
        
        if (botaoJogador1) botaoJogador1.gameObject.SetActive(true);
        AtualizarPontuacao(); AtualizarCenarioFase();
        ProximoRound(false); 
    }

    void AtualizarLogicaDificuldade() {
        float progressoJogo = Mathf.Clamp01((float)pontosJogador1 / pontosObjetivoFinal);
        multiplicadorAdrenalina = 1.0f + curvaMultiplicadorAdrenalina.Evaluate(progressoJogo) * (isSeason2 ? 3.0f : 2.5f); 
        chanceErroIA = Mathf.Lerp(erroMaximoIA, erroMinimoIA, progressoJogo);
        janelaPerfeitaAtual = Mathf.Lerp(margemPerfeito, margemPerfeito * (isSeason2 ? 0.2f : 0.3f), progressoJogo);

        if (pontosJogador1 < 30) { botaoMovelAtivo = false; deveMoverBotaoSuave = false; }
        else if (pontosJogador1 >= 30 && pontosJogador1 < 60) { 
            botaoMovelAtivo = false; deveMoverBotaoSuave = false;
            if (Random.value < 0.4f) StartCoroutine(TriggerFlashGiovanna()); 
        }
        else if (pontosJogador1 >= 60 && pontosJogador1 < 85) {
            deveMoverBotaoSuave = true; velocidadeMovimentoBotao = isSeason2 ? 7f : 5f; 
            if (!botaoMovelAtivo) {
                botaoMovelAtivo = true;
                if (rotinaMoverBotao != null) StopCoroutine(rotinaMoverBotao);
                rotinaMoverBotao = StartCoroutine(DefinirNovoAlvoBotao(1.0f)); 
            }
        }
        else if (pontosJogador1 >= 85) {
            deveMoverBotaoSuave = true; velocidadeMovimentoBotao = isSeason2 ? 15f : 12f; 
            if (rotinaMoverBotao != null) StopCoroutine(rotinaMoverBotao);
            rotinaMoverBotao = StartCoroutine(DefinirNovoAlvoBotao(0.4f)); 
        }
    }

    IEnumerator DefinirNovoAlvoBotao(float intervalo) {
        while (botaoMovelAtivo && estadoAtual == EstadoJogo.TurnoJogador1) {
            yield return new WaitForSeconds(intervalo);
            if (!estouAguardandoSaque && botaoJogador1.interactable) {
                posicaoAlvoBotao = new Vector2(Random.Range(limiteAleatorioX.x, limiteAleatorioX.y), Random.Range(limiteAleatorioY.x, limiteAleatorioY.y));
                if (audioSfx && sfxHitNormal) audioSfx.PlayOneShot(sfxHitNormal, 0.15f);
                StartCoroutine(PulsarBotaoMovel());
            }
        }
    }

    IEnumerator PulsarBotaoMovel() {
        if (!botaoJogador1) yield break;
        float t = 0;
        while (t < 0.1f) { 
            if (!botaoJogador1) yield break;
            t += Time.unscaledDeltaTime; 
            botaoJogador1.transform.localScale = Vector3.Lerp(Vector3.one * 1.3f, Vector3.one, t / 0.1f); 
            yield return null; 
        }
        if (botaoJogador1) botaoJogador1.transform.localScale = Vector3.one; 
    }

    IEnumerator SequenciaIntroLuta() {
        if (containerInterfaceJogo) containerInterfaceJogo.SetActive(true);
        if (textoAvisoFight) {
            textoAvisoFight.gameObject.SetActive(true);
            textoAvisoFight.transform.localScale = Vector3.one * 5f;
            if (audioSfx && sfxRound1) audioSfx.PlayOneShot(sfxRound1);
            textoAvisoFight.text = GetTexto("R1");
            float t = 0; while(t < 0.5f) { t += Time.unscaledDeltaTime; textoAvisoFight.transform.localScale = Vector3.Lerp(Vector3.one * 5f, Vector3.one, t/0.5f); yield return null; }
            yield return new WaitForSeconds(0.5f);
            if (audioSfx && sfxFight) audioSfx.PlayOneShot(sfxFight);
            textoAvisoFight.text = GetTexto("Fight");
            textoAvisoFight.color = Color.red;
            StartCoroutine(EfeitoImpactoShake(0.3f, 20f));
            yield return new WaitForSeconds(0.8f);
            textoAvisoFight.gameObject.SetActive(false);
        }
        IniciarPingPong();
    }

    void Update() {
        bool currentPortrait = Screen.height > Screen.width;
        if (currentPortrait != isPortrait) {
            isPortrait = currentPortrait;
            AdaptarParaOrientacao(isPortrait);
        }

        UpdateVFX(); 

        if (aguardandoCalculoAcorde && Time.time >= timerJanelaAcorde) {
            aguardandoCalculoAcorde = false;
            AnalisarEmocaoDaHarmoniaInstantanea();
        }

        if (!modoPianoSoloAtivo) {
            if (midiDisparouGolden) {
                midiDisparouGolden = false; 
                if (estadoAtual == EstadoJogo.TurnoJogador1 && !processandoVideo) RegistrarCliqueJogador("GOLDEN"); 
            }
            if (midiDisparouNormal) {
                midiDisparouNormal = false; 
                if (estadoAtual == EstadoJogo.TurnoJogador1 && !processandoVideo) RegistrarCliqueJogador("NORMAL"); 
            }
            
            densidadeAcaoMidi = Mathf.Max(0, densidadeAcaoMidi - Time.deltaTime * 0.5f);

            if (pontosJogador1 >= 30 && estadoAtual == EstadoJogo.TurnoJogador1) {
                if (emocaoAtual == EmocaoMusical.VitoriaMaior) multiplicadorAdrenalina += Time.deltaTime * 0.005f;
                else if (emocaoAtual == EmocaoMusical.TensaoMenor) multiplicadorAdrenalina -= Time.deltaTime * 0.005f;
                multiplicadorAdrenalina = Mathf.Max(1f, multiplicadorAdrenalina); 
            }

            if (controleMidiAtivo || reproduzindoMidiArquivo) {
                if (!reproduzindoMidiArquivo && Time.time - tempoUltimaNotaTocadaMidi > 1.5f && cordasVibrando.Count == 0) {
                    volumeAlvoMusica = 1f; 
                    if (emocaoAtual != EmocaoMusical.Neutra && !easterEggApagaoAtivo) MudarEmocao(EmocaoMusical.Neutra);
                } else {
                    volumeAlvoMusica = 0f; 
                }
                
                float velocidadeFade = (volumeAlvoMusica == 0f) ? 15f : 3f; 

                if (!fazendoCrossfade && !easterEggApagaoAtivo) { 
                    if (audioMusicaFundo && audioMusicaFundo.isPlaying) 
                        audioMusicaFundo.volume = Mathf.Lerp(audioMusicaFundo.volume, volumeAlvoMusica, Time.deltaTime * velocidadeFade);
                    if (audioMusicaEpica && audioMusicaEpica.isPlaying) 
                        audioMusicaEpica.volume = Mathf.Lerp(audioMusicaEpica.volume, volumeAlvoMusica, Time.deltaTime * velocidadeFade);
                    if (audioCoverEspecial && audioCoverEspecial.isPlaying) 
                        audioCoverEspecial.volume = Mathf.Lerp(audioCoverEspecial.volume, volumeAlvoMusica, Time.deltaTime * velocidadeFade);
                }
            }

            AnimarInterfaceFighter();
            
            if (habilitarEfeitosEspeciais && multiplicadorAdrenalina >= 1.5f && botaoJogador1 != null) {
                if (Time.time - tempoUltimoRastro > 0.08f && !tempoParado) { 
                    GerarRastroFantasma();
                    tempoUltimoRastro = Time.time;
                }
            }

            if (estadoAtual == EstadoJogo.TurnoJogador1 && !estouAguardandoSaque) {
                float tRem = Mathf.Max(0, tempoLimiteAbsoluto - Time.time);
                if (barraDeTempoVisual != null) barraDeTempoVisual.fillAmount = tRem / tempoDificuldadeAtual;
            }
            if (barraEnergiaUI) {
                barraEnergiaUI.fillAmount = Mathf.Lerp(barraEnergiaUI.fillAmount, energiaAtual, Time.deltaTime * 5f);
                if (energiaAtual >= 0.98f) {
                    if (!energiaSomTocado) { 
                        if (audioSfx && sfxEnergiaCheia) audioSfx.PlayOneShot(sfxEnergiaCheia); 
                        energiaSomTocado = true; 
                    }
                    barraEnergiaUI.color = Color.Lerp(isSeason2 ? Color.cyan : Color.yellow, isSeason2 ? Color.white : Color.red, Mathf.PingPong(Time.unscaledTime * 4, 1)); 
                } else { energiaSomTocado = false; barraEnergiaUI.color = isSeason2 ? new Color(0.2f, 0.6f, 1f) : Color.cyan; }
            }
        }

        AudioSource m = UsarMusicaAtiva();
        if (m != null && m.isPlaying && estadoAtual != EstadoJogo.EventoCinematico) {
            float t = m.time;
            float solo = usarTemaEspecial ? 60f : tempoDoSoloPiano;
            if (pontosJogador1 >= 80 && !musicaEpicaAtiva && !usarTemaEspecial && !easterEggApagaoAtivo) StartCoroutine(CrossfadeParaEpico());
            statusRealtime = GetStatusRitmico(t, solo);
        }
    }

    void RegistrarCliqueJogador(string tipoPoderMidi = "") 
    {
        if (estadoAtual == EstadoJogo.EventoCinematico || processandoVideo) return;

        if (estadoAtual == EstadoJogo.ConfrontoDireto) {
            toquesNoConfronto++;
            if (audioSfx && sfxHitNormal) audioSfx.PlayOneShot(sfxHitNormal, 0.4f);
            if (habilitarEfeitosEspeciais) GerarTextoFlutuante("HIT!", Color.yellow, botaoJogador1.GetComponent<RectTransform>().anchoredPosition);
            return;
        }

        if (estadoAtual != EstadoJogo.TurnoJogador1) return;

        estouAguardandoSaque = false; 
        if (timerTurnoCoroutine != null) StopCoroutine(timerTurnoCoroutine);
        
        string classificacao = "GOOD";
        Color corHit = Color.white;
        
        float tempoRestante = Mathf.Max(0, tempoLimiteAbsoluto - Time.time);

        if (tempoRestante <= 0.20f || tipoPoderMidi == "GOLDEN") {
            classificacao = "PERFECT!";
            corHit = Color.cyan;
            energiaAtual = Mathf.Clamp01(energiaAtual + 0.15f);
            comboAtual++;
            pontosJogador1 += 2;
            forcaDoUltimoGolpe = "GOLDEN"; 
            if (audioSfx && sfxHitGolden) audioSfx.PlayOneShot(sfxHitGolden);
            if (habilitarEfeitosEspeciais) GerarTextoFlutuante("PERFECT!", corHit, botaoJogador1.GetComponent<RectTransform>().anchoredPosition);

        } else if (tempoRestante <= 0.40f || tipoPoderMidi == "NORMAL") {
            classificacao = "GREAT!";
            corHit = Color.yellow;
            energiaAtual = Mathf.Clamp01(energiaAtual + 0.08f);
            comboAtual++;
            pontosJogador1 += 1;
            forcaDoUltimoGolpe = "NORMAL";
            if (audioSfx && sfxHitNormal) audioSfx.PlayOneShot(sfxHitNormal);
            if (habilitarEfeitosEspeciais) GerarTextoFlutuante("GREAT!", corHit, botaoJogador1.GetComponent<RectTransform>().anchoredPosition);

        } else {
            classificacao = "GOOD";
            corHit = Color.white;
            energiaAtual = Mathf.Clamp01(energiaAtual + 0.02f);
            comboAtual++;
            pontosJogador1 += 1;
            forcaDoUltimoGolpe = "NORMAL";
            if (audioSfx && sfxHitNormal) audioSfx.PlayOneShot(sfxHitNormal, 0.5f);
            if (habilitarEfeitosEspeciais) GerarTextoFlutuante("GOOD", corHit, botaoJogador1.GetComponent<RectTransform>().anchoredPosition);
        }

        AudioSource m = UsarMusicaAtiva();
        float solo = usarTemaEspecial ? 60f : tempoDoSoloPiano;
        float diff = Mathf.Abs(m.time - solo);
        float tecnicaAtual = isSeason2 ? tecnicaEspectral : focoEscolar;

        if (diff <= (janelaPerfeitaAtual * tecnicaAtual)) {
            classificacao = "MUSIC PERFECT!";
            corHit = Color.magenta;
            energiaAtual = Mathf.Clamp01(energiaAtual + 0.35f); 
            forcaDoUltimoGolpe = "GOLDEN";
            if (audioSfx && sfxHitGolden) audioSfx.PlayOneShot(sfxHitGolden);
            if (habilitarEfeitosEspeciais && !tempoParado) StartCoroutine(EfeitoTimeStop());
            if (habilitarEfeitosEspeciais) GerarTextoFlutuante("MUSIC PERFECT!!", corHit, botaoJogador1.GetComponent<RectTransform>().anchoredPosition);
        }

        if (comboAtual > comboMaximo) comboMaximo = comboAtual;
        AtualizarUICombo(classificacao, corHit);

        if (energiaAtual >= 0.98f || tipoPoderMidi == "GOLDEN") {
            if (missaoHarmonicaAtual != EmocaoMusical.Neutra) {
                if (!easterEggApagaoAtivo) ShowFeedback(isSeason2 ? $"RESSONÂNCIA!\n{ultimoAcordeDetectado}" : $"MISSÃO CUMPRIDA!\n{ultimoAcordeDetectado}", Color.cyan);
                missaoHarmonicaAtual = EmocaoMusical.Neutra; 
                if (textoMissaoMusical) textoMissaoMusical.text = "";
            }
            StartCoroutine(IniciarConfrontoDePoder());
            return;
        }

        if (botaoJogador1) botaoJogador1.interactable = false;
        StartCoroutine(ProcessarClique());
    }

    IEnumerator IniciarConfrontoDePoder() 
    {
        estadoAtual = EstadoJogo.ConfrontoDireto;
        toquesNoConfronto = 0;
        energiaAtual = 0f;

        if (painelConfronto) painelConfronto.alpha = 1f;
        ShowFeedback("CLASH!", Color.magenta);
        StartCoroutine(EfeitoImpactoShake(2.5f, 5f)); 

        if (audioSfx) audioSfx.PlayOneShot(somMudaMuda);

        AudioSource m = UsarMusicaAtiva();
        float pitchOriginal = m.pitch;
        m.pitch = 0.8f;

        float tempoRestante = 2.5f;
        while (tempoRestante > 0) {
            tempoRestante -= Time.unscaledDeltaTime;
            if (textoCombo) textoCombo.text = toquesNoConfronto.ToString() + " HITS!";
            yield return null;
        }

        m.pitch = pitchOriginal;
        if (painelConfronto) painelConfronto.alpha = 0f;

        if (toquesNoConfronto >= 15) {
            ShowFeedback("OITAVADA DESTRUIDORA!", Color.cyan);
            pontosJogador1 += 5; 
            StartCoroutine(ZoomDramatico());
            StartCoroutine(EfeitoStandAura(isSeason2 ? Color.cyan : new Color(1f, 0.8f, 0f)));
            forcaDoUltimoGolpe = "GOLDEN";
            if (habilitarEfeitosEspeciais) GerarTextoFlutuante("OVERKILL!!", Color.cyan, botaoJogador1.GetComponent<RectTransform>().anchoredPosition);
        } else {
            ShowFeedback("ATAQUE FORTE!", Color.yellow);
            pontosJogador1 += 2;
            forcaDoUltimoGolpe = "NORMAL";
        }

        toquesNoConfronto = 0;
        if (botaoJogador1) botaoJogador1.interactable = false;
        StartCoroutine(ProcessarClique());
    }

    void QuebrarCombo() {
        comboAtual = 0;
        multiplicadorAdrenalina = Mathf.Max(1f, multiplicadorAdrenalina - 0.2f);
        ShowFeedback("MISS", Color.red);
        if (textoCombo) textoCombo.text = "";
        
        StartCoroutine(EfeitoImpactoShake(0.3f, 15f));
        
        if (habilitarEfeitosEspeciais) {
            StartCoroutine(EfeitoGlitchDanoVisual());
            GerarTextoFlutuante("MISS!", Color.red, botaoJogador1.GetComponent<RectTransform>().anchoredPosition);
        }

        if (audioSfx && sfxPontoPerdido) audioSfx.PlayOneShot(sfxPontoPerdido);
        
        if (botaoJogador1) botaoJogador1.interactable = false;
        ReproduzirAcao(jogador1Perdeu, () => StartCoroutine(EsperarEPontuar(2)));
    }

    void AtualizarUICombo(string classif, Color cor) {
        if (textoClassificacaoHit) {
            textoClassificacaoHit.text = classif;
            textoClassificacaoHit.color = cor;
        }
        if (textoCombo && comboAtual > 1) {
            textoCombo.text = comboAtual.ToString() + " COMBO";
            textoCombo.color = isSeason2 ? Color.cyan : Color.white;
        } else if (textoCombo) {
            textoCombo.text = "";
        }
    }

    IEnumerator ProcessarClique() {
        processandoVideo = true;
        estadoAtual = EstadoJogo.ReproduzindoVideo;
        bool esp = (energiaAtual >= 0.98f || forcaDoUltimoGolpe == "GOLDEN");
        
        VideoClip c = null;

        if (pontosJogador1 >= 30) {
            if (densidadeAcaoMidi >= 0.5f && (emocaoAtual == EmocaoMusical.VitoriaMaior || emocaoAtual == EmocaoMusical.MagiaAumentada)) {
                if (jogador1ClipesClimax != null && jogador1ClipesClimax.Length > 0) c = jogador1ClipesClimax[Random.Range(0, jogador1ClipesClimax.Length)];
            } 
            else if (densidadeAcaoMidi <= 0.6f && (emocaoAtual == EmocaoMusical.TensaoMenor || emocaoAtual == EmocaoMusical.PerigoDiminuto)) {
                if (jogador1ClipesTristes != null && jogador1ClipesTristes.Length > 0) c = jogador1ClipesTristes[Random.Range(0, jogador1ClipesTristes.Length)];
            }
        }
        
        if (c == null) {
            if (esp) c = goldenChordVideo;
            else c = jogador1Clipes[Random.Range(0, jogador1Clipes.Length)];
        }
        
        if (esp) { 
            audioSfx.PlayOneShot(somMudaMuda); 
            StartCoroutine(EfeitoImpactoShake(0.4f, 25f)); 
            if(energiaAtual >= 0.98f) energiaAtual = 0; 
        }
        
        ReproduzirAcao(c, () => StartCoroutine(ExecutarIA()));
        yield return null;
    }

    IEnumerator EsperarEPontuar(int quemGanhou) {
        estadoAtual = EstadoJogo.PausaEntreRounds;
        yield return new WaitForSeconds(0.15f);
        if (quemGanhou == 1) { 
            pontosJogador1++; multiplicadorAdrenalina += 0.008f;
            if (audioSfx && sfxPontoGanhado) audioSfx.PlayOneShot(sfxPontoGanhado);
        } else { 
            pontosJogador2++; energiaAtual = Mathf.Max(0, energiaAtual - 0.2f);
            
            if (isSeason2) {
                vidasLorna--; 
                if (audioSfx && sfxDanoSofrido) audioSfx.PlayOneShot(sfxDanoSofrido);
                StartCoroutine(EfeitoImpactoShake(0.5f, 30f)); 
                if (vidasLorna <= 0) {
                    estadoAtual = EstadoJogo.EventoCinematico;
                    ShowFeedback("ALMA DESTRUÍDA...", Color.red);
                    if (audioSfx && sfxDesintegracao) audioSfx.PlayOneShot(sfxDesintegracao);
                    
                    ReportarPontuacaoGooglePlay(pontosJogador1);
                    ReproduzirAcao(lornaDesintegrandoVideo, () => { SceneManager.LoadScene(SceneManager.GetActiveScene().name); });
                    yield break; 
                }
            } else {
                if (audioSfx && sfxPontoPerdido) audioSfx.PlayOneShot(sfxPontoPerdido);
            }
        }
        AtualizarPontuacao();
        Canvas.ForceUpdateCanvases();
        if (animacaoPlacarCoroutine != null) StopCoroutine(animacaoPlacarCoroutine);
        animacaoPlacarCoroutine = StartCoroutine(AnimarPlacar());
        
        if (pontosJogador1 >= proximoAlvoFase && !easterEggApagaoAtivo) {
            if (audioSfx && sfxKO) audioSfx.PlayOneShot(sfxKO);
            SalvarProgressoFase(); yield return StartCoroutine(SequenciaConquistaFase());
            proximoAlvoFase += 15; AtualizarCenarioFase();
            
            if (isSeason2 && (pontosJogador1 == 30 || pontosJogador1 == 60)) {
                AlternarModoPianoSolo(true);
                yield break; 
            }
        }
        
        if (pontosJogador1 >= pontosObjetivoFinal) FinalizarJogo();
        else ProximoRound(false); 
    }

    void AtualizarPontuacao() { 
        if (textoPontuacao) {
            if (isSeason2) textoPontuacao.text = $" {pontosJogador1}        {pontosJogador2}\nVIDAS: {vidasLorna}";
            else textoPontuacao.text = $" {pontosJogador1}        {pontosJogador2}"; 
        }
    }

    IEnumerator AnimarPlacar() {
        if (!painelPlacar || easterEggApagaoAtivo) yield break;
        painelPlacar.gameObject.SetActive(true); Vector2 pos = painelPlacar.anchoredPosition;
        while (pos.y > posicaoYVisivel + 1f) { pos.y = Mathf.Lerp(pos.y, posicaoYVisivel, Time.unscaledDeltaTime * velocidadeAnimacaoPlacar); painelPlacar.anchoredPosition = pos; yield return null; }
        yield return new WaitForSecondsRealtime(tempoExibicaoPlacar);
        while (pos.y < posicaoYEscondido - 1f) { pos.y = Mathf.Lerp(pos.y, posicaoYEscondido, Time.unscaledDeltaTime * velocidadeAnimacaoPlacar); painelPlacar.anchoredPosition = pos; yield return null; }
        painelPlacar.gameObject.SetActive(false);
    }

    void ShowFeedback(string msg, Color col) { 
        if (textoFeedback == null) return; 
        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine); 
        feedbackCoroutine = StartCoroutine(AnimaFeedbackTexto(msg, col)); 
    }

    IEnumerator AnimaFeedbackTexto(string msg, Color col) {
        textoFeedback.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        textoFeedback.text = msg; 
        textoFeedback.color = col; 
        float t = 0; 
        textoFeedback.transform.localScale = Vector3.one * 0.2f;
        textoFeedback.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));

        while(t < 0.15f) { 
            t += Time.unscaledDeltaTime; 
            textoFeedback.transform.localScale = Vector3.Lerp(Vector3.one * 0.2f, Vector3.one * 2.8f, t/0.15f); 
            yield return null; 
        }
        t = 0; 
        while(t < 0.1f) { 
            t += Time.unscaledDeltaTime; 
            textoFeedback.transform.localScale = Vector3.Lerp(Vector3.one * 2.8f, Vector3.one * 1.5f, t/0.1f); 
            yield return null; 
        }
        yield return new WaitForSecondsRealtime(0.6f); 
        textoFeedback.text = "";
    }

    void AnimarInterfaceFighter() {
        if (energiaAtual >= 0.98f && !easterEggApagaoAtivo) { if (telaUnica && !isPortrait) { float v = Mathf.Sin(Time.unscaledTime * 30f) * 3f; telaUnica.rectTransform.anchoredPosition = new Vector2(v, v); } }
        else if (telaUnica && !isPortrait && !easterEggApagaoAtivo) telaUnica.rectTransform.anchoredPosition = Vector2.zero;
        foreach (var p in publicoFase) { if (p && p.gameObject.activeInHierarchy) p.anchoredPosition = new Vector2(p.anchoredPosition.x, Mathf.Sin(Time.time * 5f + p.anchoredPosition.x) * 12f); }

        if (deveMoverBotaoSuave && botaoJogador1 != null && !easterEggApagaoAtivo) {
            RectTransform rt = botaoJogador1.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, posicaoAlvoBotao, Time.deltaTime * velocidadeMovimentoBotao);
        }
    }

    AudioSource UsarMusicaAtiva() { return usarTemaEspecial ? audioCoverEspecial : (musicaEpicaAtiva ? audioMusicaEpica : audioMusicaFundo); }
    string GetStatusRitmico(float t, float solo) { return (Mathf.Abs(t - solo) <= janelaPerfeitaAtual) ? "GOLDEN!" : "Playing..."; }
    
    void ProximoRound(bool contagem) {
        estadoAtual = EstadoJogo.TurnoJogador1; estouAguardandoSaque = !contagem;
        if (botaoJogador1) { RandomizarPosicaoBotao(); botaoJogador1.interactable = true; }
        
        AtualizarLogicaDificuldade(); 
        tempoDificuldadeAtual = Mathf.Max(isSeason2 ? 0.20f : 0.25f, (isSeason2 ? 1.6f : 1.8f) / multiplicadorAdrenalina); 

        if (pontosJogador1 >= 15 && Random.value > 0.6f && textoMissaoMusical != null && !easterEggApagaoAtivo) {
            missaoHarmonicaAtual = (EmocaoMusical)Random.Range(1, 4); 
            string nomeMissao = missaoHarmonicaAtual == EmocaoMusical.VitoriaMaior ? "Maior" : (missaoHarmonicaAtual == EmocaoMusical.TensaoMenor ? "Menor" : "Diminuto");
            textoMissaoMusical.text = (isSeason2 ? "ALVO: Rebata com Acorde " : "DICA: Rebata com Acorde ") + nomeMissao + "!";
            textoMissaoMusical.color = Color.white;
        } else {
            missaoHarmonicaAtual = EmocaoMusical.Neutra;
            if (textoMissaoMusical) textoMissaoMusical.text = "";
        }
        
        if (timerTurnoCoroutine != null) StopCoroutine(timerTurnoCoroutine);
        if (contagem) { tempoLimiteAbsoluto = Time.time + tempoDificuldadeAtual; timerTurnoCoroutine = StartCoroutine(ContagemRegressiva(tempoDificuldadeAtual)); }
    }

    IEnumerator ContagemRegressiva(float t) { yield return new WaitForSeconds(t); if(estadoAtual == EstadoJogo.TurnoJogador1 && !estouAguardandoSaque) ReproduzirAcao(null, () => StartCoroutine(EsperarEPontuar(2))); }
    
    IEnumerator ImpactFrame(float d) { 
        float os = Time.timeScale; 
        Time.timeScale = 0.05f; 
        yield return new WaitForSecondsRealtime(d); 
        Time.timeScale = os; 
    }
    
    void RandomizarPosicaoBotao() { 
        if (!botaoJogador1) return; 
        Vector2 novaPos = new Vector2(Random.Range(limiteAleatorioX.x, limiteAleatorioX.y), Random.Range(limiteAleatorioY.x, limiteAleatorioY.y));
        RectTransform rt = botaoJogador1.GetComponent<RectTransform>(); 
        if (deveMoverBotaoSuave) { posicaoAlvoBotao = novaPos; } else { rt.anchoredPosition = novaPos; posicaoAlvoBotao = novaPos; }
    }

    void PararTodasAsMusicas() { 
        if(audioMusicaFundo) audioMusicaFundo.Stop(); 
        if(audioMusicaEpica) audioMusicaEpica.Stop(); 
        if(audioCoverEspecial) audioCoverEspecial.Stop(); 
        if(audioMenuSuno) audioMenuSuno.Stop(); 
    }

    void TocarVideoSemCallback(VideoClip c) { ReproduzirAcao(c, null); }

    void ReproduzirAcao(VideoClip c, System.Action cb) { if (c == null) cb?.Invoke(); else StartCoroutine(Transicao(c, cb)); }

    IEnumerator Transicao(VideoClip c, System.Action cb) { 
        processandoVideo = true; 
        vpAtivo.Stop(); 
        vpEmEspera.clip = c; 
        vpEmEspera.Prepare(); 
        
        float tempoLimite = 2.5f;
        while (!vpEmEspera.isPrepared && tempoLimite > 0) { 
            tempoLimite -= Time.unscaledDeltaTime;
            yield return null; 
        } 
        
        vpEmEspera.Play(); 
        yield return new WaitForSecondsRealtime(0.05f); 
        telaUnica.texture = texEspera; 
        
        var v = vpAtivo; vpAtivo = vpEmEspera; vpEmEspera = v; 
        var t = texAtiva; texAtiva = texEspera; texEspera = t; 
        
        float dur = (float)vpAtivo.length; 
        if (dur <= 0.1f) dur = 2.0f; 
        
        float elap = 0; 
        while (vpAtivo.isPlaying && elap < dur) { elap += Time.deltaTime; yield return null; } 
        processandoVideo = false; 
        cb?.Invoke(); 
    }
    
    IEnumerator ExecutarIA() {
        estadoAtual = EstadoJogo.TurnoJogador2; 
        
        float tempoReacaoIA = 0.8f / multiplicadorAdrenalina;
        
        if (pontosJogador1 >= 40 && Random.value < 0.35f) {
            tempoReacaoIA *= 0.4f; 
            if (habilitarEfeitosEspeciais) GerarTextoFlutuante("COUNTER!!", Color.red, new Vector2(0, 150));
            if (audioSfx && sfxHitGolden) audioSfx.PlayOneShot(sfxHitGolden, 0.3f);
        }
        
        yield return new WaitForSeconds(tempoReacaoIA);
        
        float chanceErroLocal = chanceErroIA;
        if (pontosJogador1 >= 30) {
            if (densidadeAcaoMidi <= 0.4f && (emocaoAtual == EmocaoMusical.TensaoMenor || emocaoAtual == EmocaoMusical.PerigoDiminuto)) chanceErroLocal *= (isSeason2 ? 0.1f : 0.3f); 
            else if (densidadeAcaoMidi >= 0.7f && (emocaoAtual == EmocaoMusical.VitoriaMaior || emocaoAtual == EmocaoMusical.MagiaAumentada)) chanceErroLocal *= (isSeason2 ? 2.5f : 2.0f); 
        }

        if (Random.value < chanceErroLocal && !easterEggApagaoAtivo) ReproduzirAcao(jogador2Perdeu, () => StartCoroutine(EsperarEPontuar(1)));
        else { if(pontosJogador1 > 30 && Random.value < 0.2f && !easterEggApagaoAtivo) StartCoroutine(TriggerFlashGiovanna()); ReproduzirAcao(jogador2Clipes[Random.Range(0, jogador2Clipes.Length)], () => ProximoRound(true)); }
    }
    
    IEnumerator CrossfadeParaEpico() { 
        fazendoCrossfade = true;
        musicaEpicaAtiva = true; 
        audioMusicaEpica.time = audioMusicaFundo.time; 
        audioMusicaEpica.Play(); 
        float d = 1.2f, e = 0; 
        while (e < d) { 
            e += Time.deltaTime; 
            audioMusicaEpica.volume = (e / d) * volumeAlvoMusica; 
            audioMusicaFundo.volume = (1 - (e / d)) * volumeAlvoMusica; 
            yield return null; 
        } 
        audioMusicaFundo.Stop(); 
        fazendoCrossfade = false;
    }
    
    IEnumerator SequenciaConquistaFase() { estadoAtual = EstadoJogo.EventoCinematico; ShowFeedback("K.O.", Color.green); yield return new WaitForSeconds(2.0f); estadoAtual = EstadoJogo.PausaEntreRounds; }
    IEnumerator TriggerFlashGiovanna() { if(flashGiovanna){ flashGiovanna.alpha = 0.8f; while(flashGiovanna.alpha > 0){ flashGiovanna.alpha -= Time.unscaledDeltaTime * 4f; yield return null; } } }
    
    IEnumerator EfeitoImpactoShake(float dur, float forca) { 
        Transform alvoShake = isPortrait && containerInterfaceJogo != null ? containerInterfaceJogo.transform : telaUnica.transform;
        if(alvoShake == null) yield break;

        Vector3 orig = alvoShake.localPosition; 
        Quaternion origRot = alvoShake.localRotation;
        float elap = 0f; 
        while (elap < dur) { 
            alvoShake.localPosition = orig + (Vector3)Random.insideUnitCircle * forca; 
            alvoShake.localRotation = Quaternion.Euler(0, 0, Random.Range(-1.5f, 1.5f));
            elap += Time.unscaledDeltaTime; 
            yield return null; 
        } 
        alvoShake.localPosition = orig; 
        alvoShake.localRotation = origRot;
    }

    void SalvarProgressoFase() { 
        PlayerPrefs.SetInt(isSeason2 ? "Mikalle_S2_PontoFinal" : "Mikalle_PontoFinal", pontosJogador1); 
        PlayerPrefs.Save(); 
        ReportarPontuacaoGooglePlay(pontosJogador1);
    }

    void AtualizarCenarioFase() { int fases = pontosJogador1 / 15; for (int i = 0; i < fases; i++) if (i < publicoFase.Length && publicoFase[i]) publicoFase[i].gameObject.SetActive(true); if (pontosJogador1 >= 80 && overlayCorCenario && !easterEggApagaoAtivo) overlayCorCenario.color = new Color(1, 0, 0.8f, 0.15f); }
    
    void FinalizarJogo() { 
        estadoAtual = EstadoJogo.EventoCinematico;
        ReportarPontuacaoGooglePlay(pontosJogador1);

        PlayerPrefs.DeleteKey(isSeason2 ? "Mikalle_S2_PontoFinal" : "Mikalle_PontoFinal"); 
        
        StartCoroutine(RotinaRecompensaFinal());
    }

    IEnumerator RotinaRecompensaFinal() {
        if (audioSfx && sfxEnergiaCheia) audioSfx.PlayOneShot(sfxEnergiaCheia);
        ShowFeedback("SUPREME MASTER!!\n100 PONTOS", Color.yellow);
        
        if (brilhoPlacar) brilhoPlacar.color = Color.yellow;
        if (painelPlacar) painelPlacar.localScale = Vector3.one * 1.5f;

        for (int i = 0; i < 15; i++) {
            SpawnParticulaImpacto(new Vector3(Random.Range(-300, 300), Random.Range(-200, 200), 0), Color.yellow);
            yield return new WaitForSecondsRealtime(0.1f);
        }

        yield return new WaitForSecondsRealtime(2.0f);

        if (isSeason2) {
            if (audioSfx && sfxDesintegracao) audioSfx.PlayOneShot(sfxDesintegracao);
            ShowFeedback("REVENGE...", Color.cyan);
            ReproduzirAcao(bossDesintegrandoVideo, () => { SceneManager.LoadScene(SceneManager.GetActiveScene().name); });
        } else {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
        }
    }

    void DetectarIdiomaDoCelular() {
        SystemLanguage lang = Application.systemLanguage;
        if (lang == SystemLanguage.Portuguese) idiomaAtual = Language.Portugues;
        else if (lang == SystemLanguage.Italian) idiomaAtual = Language.Italiano;
        else if (lang == SystemLanguage.Spanish) idiomaAtual = Language.Espanol;
        else idiomaAtual = Language.English;
    }

    void OnGUI() {
        if (!Application.isEditor) return;
        GUI.backgroundColor = Color.black;
        GUI.Box(new Rect(10, 10, 350, 300), isSeason2 ? "LORNA MIKALLE: PHANTOM KEYS (S2)" : "LORNA MIKALLE - SUPREME SYSTEM");
        GUI.Label(new Rect(20, 30, 330, 20), $"Adrenalina: {multiplicadorAdrenalina:F2}x | Gauge: {energiaAtual*100:F0}%");
        GUI.Label(new Rect(20, 50, 330, 20), $"Vibe: {(pontosJogador1 >= 30 ? emocaoAtual.ToString() : "Bloqueada (<30pts)")}");
        if (isSeason2) GUI.Label(new Rect(20, 70, 330, 20), $"Pontos: {pontosJogador1}/{pontosObjetivoFinal} | Vidas: {vidasLorna}");
        else GUI.Label(new Rect(20, 70, 330, 20), $"Pontos: {pontosJogador1}/{pontosObjetivoFinal} | Status: {statusRealtime}");
        GUI.Label(new Rect(20, 90, 330, 20), $"MIDI: {(controleMidiAtivo ? "Ativo" : "Inativo")} | Tocou: {ultimoAcordeDetectado}");
        GUI.Label(new Rect(20, 110, 330, 20), $"Sequence Step (EasterEgg): {sequenceStep}");
        
        if (GUI.Button(new Rect(20, 140, 310, 30), "Forçar Modo Piano Solo")) {
            AlternarModoPianoSolo(!modoPianoSoloAtivo);
        }

        if (GUI.Button(new Rect(20, 180, 150, 30), "Carregar Tutorial")) {
            CarregarETocarArquivoMidiParaEstudo();
        }
        
        if (GUI.Button(new Rect(180, 180, 150, 30), "Abrir Feed Fake")) {
            AbrirFeedOnline();
        }

        if (GUI.Button(new Rect(20, 220, 310, 30), "Mostrar Leaderboard")) {
            MostrarLeaderboard();
        }
    }

    public void ToggleGravacaoSpeedArt() {
        if (!gravandoSpeedArt) {
            gravandoSpeedArt = true;
            contadorFramesSpeedArt = 0;
            ShowFeedback("REC SPEEDART [ON]", Color.red);
            StartCoroutine(RotinaCapturaSpeedArt());
        } else {
            StopCoroutine("RotinaCapturaSpeedArt"); 
            StartCoroutine(FinalizarSpeedArtComTema()); 
        }
    }

    IEnumerator RotinaCapturaSpeedArt() {
        while (gravandoSpeedArt) {
            yield return new WaitForEndOfFrame(); 
            
            Texture2D ss = ScreenCapture.CaptureScreenshotAsTexture();
            NativeGallery.SaveImageToGallery(ss, "Lorna SpeedArt", $"frame_{contadorFramesSpeedArt:D5}.png");
            Destroy(ss); 

            contadorFramesSpeedArt++;
            yield return new WaitForSeconds(velocidadeSpeedArt); 
        }
    }

    IEnumerator FinalizarSpeedArtComTema() {
        ShowFeedback("RENDERIZANDO OUTRO...", Color.magenta);
        
        AlternarModoPianoSolo(true);
        if (audioMusicaFundo) audioMusicaFundo.Stop();
        
        int[] melodiaLorna = { 69, 72, 76, 81, 76, 72, 69, 64, 69 }; 
        
        for(int i = 0; i < melodiaLorna.Length; i++) {
            ProcessarNotaOn(melodiaLorna[i], 0.9f, false);
            
            for(int f = 0; f < 10; f++) {
                yield return new WaitForEndOfFrame();
                Texture2D ss = ScreenCapture.CaptureScreenshotAsTexture();
                NativeGallery.SaveImageToGallery(ss, "Lorna SpeedArt", $"frame_{contadorFramesSpeedArt:D5}.png");
                Destroy(ss);
                contadorFramesSpeedArt++;
            }
            ProcessarNotaOff(melodiaLorna[i]);
        }

        for(int f = 0; f < 10; f++) {
            yield return new WaitForEndOfFrame();
            Texture2D ss = ScreenCapture.CaptureScreenshotAsTexture();
            NativeGallery.SaveImageToGallery(ss, "Lorna SpeedArt", $"frame_{contadorFramesSpeedArt:D5}.png");
            Destroy(ss);
            contadorFramesSpeedArt++;
        }

        AlternarModoPianoSolo(false);
        gravandoSpeedArt = false;
        ShowFeedback("FRAMES SALVOS NA GALERIA!", Color.green);
    }
}