using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using System;
using System.Text.RegularExpressions;
using System.IO;

public class script : MonoBehaviour
{

    AudioSource audio;
    Animator animator;
    string serverUrl = "http://localhost:59125/process?INPUT_TYPE=TEXT&AUDIO=WAVE_FILE&OUTPUT_TYPE=AUDIO&LOCALE=fr&INPUT_TEXT=%22",
            configFile = @"C:\Users\Chine\Desktop\calculator\configuration.txt",
            aucuneReponseTrouvee = "Hmmmm, je ne connais pas la réponse à ceci";
    private DictationRecognizer m_DictationRecognizer;//haylee_cb
    string texte;
    void Start()
    {
        audio = gameObject.AddComponent<AudioSource>();
        animator = GameObject.Find("KamaraManHoodieAIGeneric").GetComponent<Animator>();
        m_DictationRecognizer = new DictationRecognizer();
        m_DictationRecognizer.InitialSilenceTimeoutSeconds = float.MaxValue;
        m_DictationRecognizer.DictationResult += (text, confidence) =>
        {
            Debug.LogFormat("Dictation result: {0}", text);
            if (audio.isPlaying == false) query(text);
        };

        m_DictationRecognizer.DictationHypothesis += (text) =>
        {
            Debug.LogFormat("Dictation hypothesis: {0}", text);
        };

        m_DictationRecognizer.DictationComplete += (completionCause) =>
        {
            Debug.Log(completionCause);
            if (completionCause != DictationCompletionCause.Complete)
            {
                Debug.LogErrorFormat("Dictation completed unsuccessfully: {0}.", completionCause);
            }
            Start();
        };

        m_DictationRecognizer.DictationError += (error, hresult) =>
        {
            Debug.LogErrorFormat("Dictation error: {0}; HResult = {1}.", error, hresult);
        };
        m_DictationRecognizer.Start();
    }
    string findParole(string textReconnu)
    {
        string[] lines = System.IO.File.ReadAllLines(configFile),
                 motsCles;
        string result = aucuneReponseTrouvee;
        for (int i = 0; i < lines.Length;)
        {
            motsCles = lines[i].Split(',');
            bool allPresents = true;
            foreach (string motCle in motsCles)
                if (!textReconnu.Contains(motCle))
                    allPresents = false;
            if (allPresents)
                return lines[i + 1];
            i += 2;
        }
        return aucuneReponseTrouvee;
    }
    public void query(string text)
    {
        //Debug.Log("audio play with text :"+"http://localhost:59125/process?INPUT_TYPE=TEXT&AUDIO=WAVE_FILE&OUTPUT_TYPE=AUDIO&LOCALE=fr&INPUT_TEXT=%22"+text+"%22");

        WWW audioLoader = new WWW("http://localhost:5000/?text=" +
            System.Web.HttpUtility.UrlEncode(findParole(text)));
        while (!audioLoader.isDone)
        { }
        audio.clip = audioLoader.GetAudioClip(false, false, AudioType.WAV);
        audio.Play();
    }
    void OnApplicationQuit()
    {
        m_DictationRecognizer.Stop();
    }
    void FixedUpdate()
    {
        if(audio != null)
            animator.SetBool("New Bool", audio.isPlaying);
    }
}
