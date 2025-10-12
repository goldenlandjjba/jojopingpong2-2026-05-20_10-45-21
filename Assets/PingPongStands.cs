using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

public class PingPongStands : MonoBehaviour
{
    [Header("UI")]
    public GameObject painelPedido;
    public Button botaoAceitar;
    public Button botaoRecusar;

    public Button botaoJogador1;
    public Button botaoVoltarAoZero; // 🌀 botão especial de GER
    public Text textoPontuacao;
    public GameObject popupvencedor;

    [Header("Video Player")]
    public VideoPlayer videoPlayer;

    [Header("Clipes Comuns")]
    public VideoClip introDesafio;
    public VideoClip recusaDesafio;
    public VideoClip inicioPartida;

    [Header("Clipes Jogador 1 (GER)")]
    public VideoClip[] jogador1Ataques;
    public VideoClip jogador1Perdeu;
    public VideoClip retornoAoZero; // vídeo especial de GER

    [Header("Clipes Jogador 2 (King Crimson)")]
    public VideoClip[] jogador2Ataques;
    public VideoClip jogador2Perdeu;
    public VideoClip apagarTempo; // vídeo do King Crimson apagando tempo

    [Header("Configuração do Jogo")]
    public float tempoReacaoIA = 2f;
    public float tempoReacaoJogador1 = 2f; // tempo normal para clicar
    public float tempoMaximoVoltarAoZero = 2f; // ⏱ tempo limite para ativar GER
    public int pontosParaVencer = 5;

    private bool jogoAtivo = false;
    private bool vezDoJogador1 = true;
    private bool aguardandoInput = false;
    private bool esperandoVoltarAoZero = false;

    private int pontosJogador1 = 0;
    private int pontosJogador2 = 0;

    void Start()
    {
        painelPedido.SetActive(true);
        botaoJogador1.gameObject.SetActive(false);
        botaoVoltarAoZero.gameObject.SetActive(false);

        botaoAceitar.onClick.AddListener(AceitarDesafio);
        botaoRecusar.onClick.AddListener(RecusarDesafio);
        botaoJogador1.onClick.AddListener(Jogador1Atacar);
        botaoVoltarAoZero.onClick.AddListener(VoltarAoZero);

        AtualizarPontuacao();

        if (introDesafio != null)
        {
            videoPlayer.clip = introDesafio;
            videoPlayer.Play();
        }
    }

    void AceitarDesafio()
    {
        painelPedido.SetActive(false);

        if (inicioPartida != null)
        {
            videoPlayer.clip = inicioPartida;
            videoPlayer.Play();
            videoPlayer.loopPointReached += IniciarPingPong;
        }
        else
        {
            IniciarPingPong(videoPlayer);
        }
    }

    void RecusarDesafio()
    {
        painelPedido.SetActive(false);
        jogoAtivo = false;

        if (recusaDesafio != null)
        {
            videoPlayer.clip = recusaDesafio;
            videoPlayer.Play();
        }
    }

    void IniciarPingPong(VideoPlayer vp)
    {
        videoPlayer.loopPointReached -= IniciarPingPong;

        jogoAtivo = true;
        vezDoJogador1 = true;
        aguardandoInput = true;

        botaoJogador1.gameObject.SetActive(true);
        AtualizarBotoes();

        StartCoroutine(VerificarTempoJogador1());

        Debug.Log("🎬 Ping Pong com Stands começou!");
    }

    void Jogador1Atacar()
    {
        if (!vezDoJogador1 || !aguardandoInput || jogador1Ataques.Length == 0)
            return;

        vezDoJogador1 = false;
        aguardandoInput = false;

        VideoClip ataque = jogador1Ataques[Random.Range(0, jogador1Ataques.Length)];
        ReproduzirAcao(ataque, Jogador2IA);
    }

    void Jogador2IA()
    {
        if (!jogoAtivo || jogador2Ataques.Length == 0) return;

        // Decisão aleatória: King Crimson apaga o tempo?
        bool apagarTempoAgora = Random.value < 0.3f; // 30% de chance
        if (apagarTempoAgora)
            StartCoroutine(ExecutarKingCrimson());
        else
        {
            // Ataque normal
            VideoClip ataque = jogador2Ataques[Random.Range(0, jogador2Ataques.Length)];
            ReproduzirAcao(ataque, LiberarInputJogador1);
        }
    }

    IEnumerator ExecutarKingCrimson()
    {
        yield return new WaitForSeconds(tempoReacaoIA);

        // 👹 King Crimson apaga o tempo
        ReproduzirAcao(apagarTempo, () =>
        {
            esperandoVoltarAoZero = true;
            botaoVoltarAoZero.gameObject.SetActive(true);
            StartCoroutine(VerificarVoltarAoZero());
        });
    }

    IEnumerator VerificarVoltarAoZero()
    {
        float tempoInicio = Time.time;

        while (esperandoVoltarAoZero)
        {
            if (Time.time - tempoInicio > tempoMaximoVoltarAoZero)
            {
                esperandoVoltarAoZero = false;
                botaoVoltarAoZero.gameObject.SetActive(false);

                ReproduzirAcao(jogador1Perdeu, () =>
                {
                    pontosJogador2++;
                    AtualizarPontuacao();
                    VerificarVencedor();
                    LiberarInputJogador1();
                });
            }
            yield return null;
        }
    }

    void VoltarAoZero()
    {
        if (!esperandoVoltarAoZero) return;

        esperandoVoltarAoZero = false;
        botaoVoltarAoZero.gameObject.SetActive(false);

        ReproduzirAcao(retornoAoZero, () =>
        {
            LiberarInputJogador1();
        });
    }

    IEnumerator VerificarTempoJogador1()
    {
        float tempoInicio = Time.time;

        while (vezDoJogador1 && aguardandoInput)
        {
            if (Time.time - tempoInicio > tempoReacaoJogador1)
            {
                aguardandoInput = false;
                vezDoJogador1 = false;

                ReproduzirAcao(jogador1Perdeu, () =>
                {
                    pontosJogador2++;
                    AtualizarPontuacao();
                    VerificarVencedor();
                    LiberarInputJogador1();
                });
                yield break;
            }
            yield return null;
        }
    }

    void ReproduzirAcao(VideoClip clip, System.Action callback)
    {
        videoPlayer.Stop();
        videoPlayer.clip = clip;
        videoPlayer.Play();

        VideoPlayer.EventHandler onVideoEnd = null;
        onVideoEnd = (vp) =>
        {
            videoPlayer.loopPointReached -= onVideoEnd;
            callback?.Invoke();
        };

        videoPlayer.loopPointReached += onVideoEnd;
    }

    void LiberarInputJogador1()
    {
        vezDoJogador1 = true;
        aguardandoInput = true;
        AtualizarBotoes();

        StartCoroutine(VerificarTempoJogador1());
    }

    void AtualizarBotoes()
    {
        botaoJogador1.interactable = (vezDoJogador1 && aguardandoInput);
    }

    void AtualizarPontuacao()
    {
        if (textoPontuacao != null)
            textoPontuacao.text = $"GER (Jogador 1): {pontosJogador1}  |  KC (Jogador 2): {pontosJogador2}";
    }

    void VerificarVencedor()
    {
        if (pontosJogador1 >= pontosParaVencer)
        {
            Debug.Log("🏆 Giorno (GER) venceu!");
            jogoAtivo = false;
            popupvencedor.SetActive(true);
        }
        else if (pontosJogador2 >= pontosParaVencer)
        {
            Debug.Log("🏆 Kakyoin (King Crimson) venceu!");
            jogoAtivo = false;
        }
    }
}
