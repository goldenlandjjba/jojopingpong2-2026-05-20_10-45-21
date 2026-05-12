using UnityEngine;
using MidiPlayerTK; // Puxa a biblioteca do MPTK que você acabou de baixar

public class PonteMidiMPTK : MonoBehaviour
{
    [Tooltip("Arraste o MidiStreamPlayer da sua cena aqui")]
    public MidiStreamPlayer midiStreamPlayer;

    // Traduz o nosso comando "Tocar" para o padrão MPTK
    public void TocarNota(int notaMidi, float velocidade)
    {
        if (midiStreamPlayer == null) return;
        
        MPTKEvent nota = new MPTKEvent() {
            Command = MPTKCommand.NoteOn,
            Value = notaMidi,
            Velocity = (int)(velocidade * 127f), // Converte a força física do seu dedo (0 a 1) para MIDI (0 a 127)
            Duration = -1 // -1 significa "Deixe a nota soar até eu soltar a tecla"
        };
        midiStreamPlayer.MPTK_PlayEvent(nota);
    }

    // Traduz o nosso comando "Soltar Tecla" para o padrão MPTK
    public void PararNota(int notaMidi)
    {
        if (midiStreamPlayer == null) return;

        MPTKEvent nota = new MPTKEvent() {
            Command = MPTKCommand.NoteOff,
            Value = notaMidi
        };
        midiStreamPlayer.MPTK_PlayEvent(nota);
    }

    // Traduz a pisada no Pedal de Sustain
    public void AcionarPedal(bool ativado)
    {
        if (midiStreamPlayer == null) return;

        MPTKEvent pedal = new MPTKEvent() {
            Command = MPTKCommand.ControlChange,
            Controller = MPTKController.Sustain,
            Value = ativado ? 127 : 0 // 127 afundou o pé, 0 tirou o pé
        };
        midiStreamPlayer.MPTK_PlayEvent(pedal);
    }
}