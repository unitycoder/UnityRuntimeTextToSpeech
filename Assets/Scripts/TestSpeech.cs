using UnityEngine;
using UnityLibrary;

public class TestSpeech : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // TEST speech
        Speech.instance.Say("Welcome!");
    }

    // Update is called once per frame
    void Update()
    {
        // test pressing any keys to say that character
        if (Input.anyKeyDown)
        {
            Speech.instance.Say(Input.inputString);
        }
    }
}
