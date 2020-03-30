using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using UnityStandardAssets.Characters.FirstPerson;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnLeftButtonClick : MonoBehaviour
{
    private CharacterController m_CharacterController;
    public FirstPersonController fpsController;

    // Start is called before the first frame update
    void Start()
    {
        m_CharacterController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClick()
    {
        fpsController.GoLeft();
    }
}
