using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class TitleSceneManager : MonoBehaviour
{
    public static void LoadScene()
    {
        SceneManager.LoadScene("TitleScene");
    }

    [SerializeField] private TitleSceneUI ui;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
