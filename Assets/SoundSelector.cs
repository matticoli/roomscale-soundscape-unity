using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundSelector : MonoBehaviour
{
    public AudioClip[] sounds;
    public AudioClip defaultSound;
    
    // Start is called before the first frame update
    void Start()
    {
        Dropdown dropdown = GameObject.Find("Dropdown").GetComponent<Dropdown>();
        AudioClip aud = sounds[dropdown.value];
        if(!aud)
        {
            aud = defaultSound;
        }
        AudioSource asrc = GetComponent<AudioSource>();
        asrc.clip = aud;
        asrc.loop = true;
        asrc.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
