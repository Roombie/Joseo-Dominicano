using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace APROMASTER
{
    [CreateAssetMenu(fileName = "AudioSfx", menuName = "ScriptableObjects/AudioSfx")]
    public class AudioSfx : ScriptableObject
    {
        [System.Serializable]
        public struct AudioParametersStruct
        {
            public AudioClip[] AudioClips;
            [Header("Properties")]
            public AudioMixerGroup MixerGroup;
            public enum AudioMode { Normal, Delayed, OneShot }
            public AudioMode mode;
            [Range(0, 1)] public float Volume;
            [Range(-3, 3)] public float Pitch;
            public bool Loop;
            [Header("Phasing Settings")]
            [Min(0)] public float StartDelay;
            [Min(0)] public float FadeIn;
            [Min(0)] public float FadeOut;
            [Header("3D settings")]
            public bool UseDistanceFalloff;
            // [Range(0, 1)] public float SpatialBlend;
            [Min(0)] public float MinDistance;
            public float MaxDistance;
        }
        public AudioParametersStruct AudioParameters;
        public bool isPlaying => _audioSource.isPlaying;
        private AudioSource _audioSource;
        private AudioSFXFadePlugin _fadePlugin;
        GameObject _sourceObject;

        void PlayAudioInternal(Vector3 position)
        {
            if (_sourceObject == null)
            {
                _sourceObject = new GameObject(this.name);
                _sourceObject.transform.position = position;
                _audioSource = _sourceObject.AddComponent<AudioSource>();
                _fadePlugin = _sourceObject.AddComponent<AudioSFXFadePlugin>();
                _fadePlugin.AudioSfx = this;
            }
            
            _audioSource.clip = AudioParameters.AudioClips[Random.Range(0, AudioParameters.AudioClips.Length)];
            _audioSource.outputAudioMixerGroup = AudioParameters.MixerGroup;
            _audioSource.volume = AudioParameters.Volume;
            _audioSource.pitch = AudioParameters.Pitch;
            _audioSource.loop = AudioParameters.Loop;
            _audioSource.spatialBlend = AudioParameters.UseDistanceFalloff ? 1 : 0;
            // _audioSource.spatialBlend = AudioParameters.SpatialBlend; // Maybe use this in the future
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
            _audioSource.minDistance = AudioParameters.MinDistance;
            _audioSource.maxDistance = AudioParameters.MaxDistance;
            
            switch (AudioParameters.mode)
            {
                case AudioParametersStruct.AudioMode.Normal: _audioSource.Play(); break;
                case AudioParametersStruct.AudioMode.Delayed: _audioSource.PlayDelayed(AudioParameters.StartDelay); break;
                case AudioParametersStruct.AudioMode.OneShot: _audioSource.PlayOneShot(_audioSource.clip); break;
            }
        }

        public void PlayAudio()
        {
            PlayAudioInternal(Vector3.zero);
            _fadePlugin.StartCoroutine(_fadePlugin.FadeInCoroutine(AudioParameters.FadeIn));
        }

        public void PlayAudioImmediate()
        {
            PlayAudioInternal(Vector3.zero);
        }

        public void PlayAudioOnPosition(Vector3 position)
        {
            PlayAudioInternal(position);
            _fadePlugin.StartCoroutine(_fadePlugin.FadeInCoroutine(AudioParameters.FadeIn));
        }

        public void PlayAudioImmediateOnPosition(Vector3 position)
        {
            PlayAudioInternal(position);
        }

        void StopAudioInternal(float fadeOut)
        {
            if (_audioSource == null) return;
            if (_audioSource.isPlaying)
            {
                _fadePlugin.StartCoroutine(_fadePlugin.FadeOutCoroutine(_audioSource, fadeOut));
            }
        }

        public void StopAudio()
        {
            StopAudioInternal(AudioParameters.FadeOut);
        }

        public void StopAudioImmediate()
        {
            StopAudioInternal(0);
        }

        public void SetSourceVolume(float volume)
        {
            _audioSource.volume = volume;
        }
        public float GetSourceVolume() => _audioSource.volume;
    }

    public class AudioSFXFadePlugin : MonoBehaviour
    {
        public AudioSfx AudioSfx;

        public IEnumerator FadeInCoroutine(float fadeTime)
        {
            while (!AudioSfx.isPlaying)
            {
                yield return null;
            }
            float startVolume = 0;
            float elapsedTime = 0f;

            while (elapsedTime < fadeTime)
            {
                AudioSfx.SetSourceVolume(Mathf.Lerp(startVolume, AudioSfx.AudioParameters.Volume, elapsedTime / fadeTime));
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            AudioSfx.SetSourceVolume(AudioSfx.AudioParameters.Volume);
        }

        public IEnumerator FadeOutCoroutine(AudioSource source, float fadeTime)
        {
            float startVolume = AudioSfx.GetSourceVolume();
            float elapsedTime = 0f;

            while (elapsedTime < fadeTime)
            {
                AudioSfx.SetSourceVolume(Mathf.Lerp(startVolume, 0f, elapsedTime / fadeTime));
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            source.Stop();
            // _sourceObject = null;
            // _audioSource = null;
        }
    }
}
