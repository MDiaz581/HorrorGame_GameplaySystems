using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ConsoleUpdate : MonoBehaviour
{

    public TMP_Text consoleText;

    public void OnEnable()
    {
        SteamConnector.updateConsole += UpdateConsoleText;
    }

    void OnDisable()
    {
        SteamConnector.updateConsole -= UpdateConsoleText;
    }

    private void UpdateConsoleText(string text)
    {
        consoleText.text = text;
    }
}
