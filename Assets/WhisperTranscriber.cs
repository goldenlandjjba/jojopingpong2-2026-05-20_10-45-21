using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class WhisperTranscriber : MonoBehaviour
{
    private string apiKey = "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"; // coloque a chave nova aqui!
    private string url = "https://api.openai.com/v1/audio/transcriptions";

    public IEnumerator TranscreverAudio(AudioClip clip, System.Action<string> callback)
    {
        byte[] audioData = WavUtility.AudioClipToWavBytes(clip);

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", audioData, "voz.wav", "audio/wav");
        form.AddField("model", "whisper-1");

        UnityWebRequest request = UnityWebRequest.Post(url, form);
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Erro Whisper: " + request.error);
            callback?.Invoke(null);
        }
        else
        {
            string resposta = request.downloadHandler.text;
            Debug.Log("Resposta Whisper: " + resposta);

            // A resposta vem em JSON -> {"text":"..."}
            string texto = JsonUtility.FromJson<WhisperResponse>(resposta).text;
            callback?.Invoke(texto);
        }
    }

    [System.Serializable]
    public class WhisperResponse
    {
        public string text;
    }
}
