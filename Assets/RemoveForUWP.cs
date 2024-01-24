using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if WINDOWS_UWP
public class RemoveForUWP : MonoBehaviour
{
    private void Awake()
    {
        gameObject.SetActive(false);
    }
}
#endif