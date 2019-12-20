using UnityEngine;
using UnityLibrary;

public class TestSpeech : MonoBehaviour
{
    public string sayAtStart = "Welcome!";

    // Start is called before the first frame update
    void Start()
    {
        // TEST speech
        Speech.instance.Say(sayAtStart);
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
