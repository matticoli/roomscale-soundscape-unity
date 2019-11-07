using Firebase;
using Firebase.Database;
using Firebase.Storage;
using Firebase.Unity.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Converters;
using UnityEngine.Networking;

public class SharedExperienceController : MonoBehaviour
{
    public Dropdown drop;
    public GameObject prefab;
    private Dictionary<string, object> selectedSketch;

    private IEnumerator FetchySketchy()
    {
        while (true)
        {
            yield return new WaitForSeconds(5.0f);
            FirebaseDatabase.DefaultInstance
            .GetReference("sketches")
            .GetValueAsync().ContinueWith(task => {
                if (task.IsFaulted)
                {
                    // Handle the error...
                }
                else if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    Dictionary<string, object> d = (Dictionary<string, object>)snapshot.GetValue(false);
                    Debug.Log(d);
                    List<string> sketches = new List<string>(d.Keys);
                    drop.options.Clear();
                    drop.options.Add(new Dropdown.OptionData("Select a soundscape"));
                    foreach (string sketch in sketches)
                    {
                        drop.options.Add(new Dropdown.OptionData(sketch));
                    }
                    //Debug.Log(string.Join(", ", sketches.ToArray()));
                }
            });
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //CreateAudioClip("name", wav);
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://bosear-test.firebaseio.com/");
        
        DatabaseReference dbref = FirebaseDatabase.DefaultInstance.RootReference;
        StorageReference sref = FirebaseStorage.DefaultInstance.RootReference;
        sref.Child("bigbadotis@gmail.com").Child("k.wav").GetDownloadUrlAsync().ContinueWith(task =>
        {
            if(task.IsFaulted)
            {
                Debug.Log("error fetching file");
            } else
            {
                Debug.Log(""+task.Result);
                StartCoroutine(SongCoroutine(""+task.Result));
                //CreateAudioClip(""+task.Result);
            }
        });
        IEnumerator coroutine = FetchySketchy();
        StartCoroutine(coroutine);
        drop.onValueChanged.AddListener(delegate {
            DropdownValueChanged(drop);
        });
  
    }

    IEnumerator SongCoroutine(string url)
    {
        AudioSource src = gameObject.GetComponent<AudioSource>();
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
        {
            DownloadHandlerAudioClip dHA = new DownloadHandlerAudioClip(string.Empty, AudioType.WAV);
            dHA.streamAudio = true;
            www.downloadHandler = dHA;
            www.SendWebRequest();
            while (www.downloadProgress < 0.01)
            {
                Debug.Log(www.downloadProgress);
                yield return new WaitForSeconds(.1f);
            }
            if (www.isNetworkError)
            {
                Debug.Log("error");
            }
            else
            {
                src.clip = dHA.audioClip;
                //_m1SongTime = musicOne.clip.length + Time.time;
                src.loop = true;
                src.Play();
            }
        }
    }

    IEnumerable CreateAudioClip(string url)
    {
        AudioSource src = gameObject.GetComponent<AudioSource>();
        using (var www = new WWW(url))
        {
            yield return www;
            src.clip = www.GetAudioClip();
            src.loop = true;
            src.Play();
        }
    }

    void ParseFile(string name, string data)
    {
        Debug.Log("DECODING " + name + " ... " + data);
        //CreateAudioClip(name, data);
    }

    //[{"filename":"k.wav","movementSpeed":5,"position":{"x":-255.71869774667871,"y":-1.2143254215877978E-13,"z":-546.88355161693266},"volume":1}]

    [System.Serializable]
    class SoundFile
    {
        public string filename;
        public int volume;
        public Dictionary<string, double> position;
        public int movementSpeed;
    }

    void DropdownValueChanged(Dropdown change)
    {
        string op = change.options[change.value].text;
        Debug.Log("CHANGE!" + op);
        DatabaseReference sketchRef = FirebaseDatabase.DefaultInstance.GetReference("sketches").Child(op),
            filesRef = sketchRef.Child("files"),
            soundsRef = sketchRef.Child("soundObjects");
            soundsRef.GetValueAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    // Handle the error...
                }
                else if (task.IsCompleted)
                {

                    
                    Debug.Log(task.Result.GetRawJsonValue());
                    
                    var files = (List<object>)task.Result.GetValue(false);
                    try
                    {
                        // For each sound object
                        foreach(object o in files)
                        {
                            Debug.Log(o);
                        }
                    } catch(Exception e) {
                        Debug.Log("Error deserializing!");
                        Debug.Log(e.Message);
                    }
                }
            });
    }

    // Update is called once per frame
    void Update()
    {
    }
}
