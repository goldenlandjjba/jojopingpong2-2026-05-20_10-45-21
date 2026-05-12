using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.SceneManagement;

public class PingPongVideoManual : MonoBehaviour
{
    [Header("UI Pedido")]
    public GameObject painelPedido;
    public Button botaoAceitar;
    public Button botaoRecusar;

    [Header("UI Jogo")]
    public Button botaoJogador1;
    public Text textoPontuacao;
    public GameObject popupvencedor;

    [Header("Video Player")]
    public VideoPlayer videoPlayer;

    [Header("Clipes")]
    public VideoClip introDesafio;
    public VideoClip recusaDesafio;
    public VideoClip inicioPartida;
    public VideoClip[] jogador1Clipes;
    public VideoClip[] jogador2Clipes;
    public VideoClip jogador1Perdeu;
    public VideoClip jogador2Perdeu;

    [Header("Dificuldade")]
    [Range(0f, 1f)] public float chanceErroIA = 0.2f;
    public float tempoReacaoIA = 2f;
    public float tempoReacaoJogador1 = 2f; // ⏱ tempo para jogador 1 reagir
    public int pontosParaVencer = 5;

    [Header("Som Especial")]
    public AudioSource audioSource;
    public AudioClip somMudaMuda;

    private bool jogoAtivo = false;
    private bool vezDoJogador1 = true;
    private bool aguardandoInput = false;

    private bool jogador1Acertou = false;

    private int pontosJogador1 = 0;
    private int pontosJogador2 = 0;

    private int cliqueCount = 0;
    private float ultimoClique = 0f;
    private float tempoMaxDuploClique = 0.5f;

    void Start()
    {
        painelPedido.SetActive(true);
        botaoJogador1.gameObject.SetActive(false);

        botaoAceitar.onClick.AddListener(AceitarDesafio);
        botaoRecusar.onClick.AddListener(RecusarDesafio);

        botaoJogador1.onClick.AddListener(ClicarBotaoJogador1);

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

        // ⏱ inicia o tempo para o jogador 1 reagir
        StartCoroutine(VerificarTempoJogador1());

        Debug.Log("🎬 Ping Pong começou!");
    }

    void ClicarBotaoJogador1()
    {
        float tempoAtual = Time.time;

        if (tempoAtual - ultimoClique > tempoMaxDuploClique)
            cliqueCount = 0;

        cliqueCount++;
        ultimoClique = tempoAtual;

        if (!vezDoJogador1 || !aguardandoInput || jogador1Clipes.Length == 0)
            return;

        jogador1Acertou = true;
        vezDoJogador1 = false;
        aguardandoInput = false;

        VideoClip clip = jogador1Clipes[Random.Range(0, jogador1Clipes.Length)];

        if (cliqueCount == 2 && somMudaMuda != null && audioSource != null)
        {
            audioSource.PlayOneShot(somMudaMuda);
            StartCoroutine(DispararAtaqueComDelay(clip, 0.2f));
        }
        else
        {
            ReproduzirAcao(clip, Jogador2IA);
        }

        cliqueCount = 0;
    }

    IEnumerator DispararAtaqueComDelay(VideoClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReproduzirAcao(clip, Jogador2IA);
    }

    void Jogador2IA()
    {
        if (!jogoAtivo || jogador2Clipes.Length == 0) return;
        StartCoroutine(ExecutarAcaoIA());
    }

    IEnumerator ExecutarAcaoIA()
    {
        yield return new WaitForSeconds(tempoReacaoIA);

        bool iaErrou = Random.value < chanceErroIA;

        if (iaErrou)
        {
            ReproduzirAcao(jogador2Perdeu, () =>
            {
                pontosJogador1++;
                AtualizarPontuacao();
                VerificarVencedor();
                LiberarInputJogador1();
            });
        }
        else
        {
            ReproduzirAcao(jogador2Clipes[Random.Range(0, jogador2Clipes.Length)], () =>
            {
                if (!jogador1Acertou)
                {
                    ReproduzirAcao(jogador1Perdeu, () =>
                    {
                        pontosJogador2++;
                        AtualizarPontuacao();
                        VerificarVencedor();
                        LiberarInputJogador1();
                    });
                }
                else
                {
                    jogador1Acertou = false;
                    LiberarInputJogador1();
                }
            });
        }
    }

    // 🔹 Se o Jogador 1 não clicar no tempo certo → perde a bola
    IEnumerator VerificarTempoJogador1()
    {
        float tempoInicio = Time.time;

        while (vezDoJogador1 && aguardandoInput)
        {
            if (Time.time - tempoInicio > tempoReacaoJogador1)
            {
                // tempo acabou → jogador 1 perdeu
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

        // reinicia timer do jogador 1
        StartCoroutine(VerificarTempoJogador1());
    }

    void AtualizarBotoes()
    {
        botaoJogador1.interactable = (vezDoJogador1 && aguardandoInput);
    }

    void AtualizarPontuacao()
    {
        if (textoPontuacao != null)
            textoPontuacao.text = $"Jogador 1: {pontosJogador1}  |  Jogador 2: {pontosJogador2}";
    }

    void VerificarVencedor()
    {
        if (pontosJogador1 >= pontosParaVencer)
        {
            Debug.Log("🏆 Jogador 1 venceu!");
            jogoAtivo = false;
            SceneManager.LoadScene("Game2");
        }
        else if (pontosJogador2 >= pontosParaVencer)
        {
            Debug.Log("🏆 Jogador 2 venceu!");
            jogoAtivo = false;
        }
    }
}
