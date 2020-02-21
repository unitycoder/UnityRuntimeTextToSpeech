using UnityEngine;
using UnityLibrary;

public class TestSpeech : MonoBehaviour
{
    public string sayAtStart = "Welcome!";

    // Start is called before the first frame update
    void Start()
    {
        // TEST speech
        Speech.instance.Say(sayAtStart, TTSCallback);

    }

    // Update is called once per frame
    void Update()
    {
        // test pressing any keys to say that character
        if (Input.anyKeyDown)
        {
            Speech.instance.Say(Input.inputString, TTSCallback);
        }
    }

    void TTSCallback(string message, AudioClip audio) {
        AudioSource source = GetComponent<AudioSource>();
        if(source == null) {
            source = gameObject.AddComponent<AudioSource>();
        }

        source.clip = audio;
        source.Play();
    }
}
