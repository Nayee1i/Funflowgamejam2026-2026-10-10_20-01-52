using System;
using UnityEngine;

// Громкость и настройки каждого звука. Ассет лежит в Resources/Audio/SoundLibrary
// и правится в Inspector; изменения в Play Mode сохраняются (это ассет, а не сцена).
[CreateAssetMenu(menuName = "Bounce Club/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [Serializable]
    public class Sound
    {
        public string name;
        [Tooltip("Если пусто, берётся файл Resources/Audio/<name>")]
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Случайный разброс высоты, чтобы частый звук не надоедал")]
        [Range(0f, .3f)] public float pitchVariation;
        [Tooltip("Не повторять звук чаще, чем раз в столько секунд")]
        public float cooldown = .03f;
    }

    public Sound[] sounds = Array.Empty<Sound>();

    public Sound Find(string soundName)
    {
        foreach (var sound in sounds)
            if (sound != null && sound.name == soundName) return sound;
        return null;
    }
}
