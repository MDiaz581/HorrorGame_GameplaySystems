using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System;
using UnityEditor;

public class TextChatBehavior : NetworkBehaviour
{
    [SerializeField]
    private TMP_InputField inputField = null;
    [SerializeField] private GameObject chatBubblePrefab = null;
    [SerializeField] private Transform chatBox = null; // Parent object this gets set to.
    private bool isChatOpen = false; // Track if chat is open
    private int PlayerID;

    public enum CurrentMenuType
    {
        Chat,
        Contacts,
        Base
    }

    public CurrentMenuType currentMenuType;

    [SerializeField] private GameObject chatRoom;
    [SerializeField] private GameObject contactsList;
    [SerializeField] private GameObject baseMenu;

    public static event Action SendSoundToPlayers;

    //Key thing to think about this script. Everyone loads this script locally, but it has the ability to communicate through the network but only if it's called through cmd and rpc.
    //So if we maintain this work flow we can in essence allow players to give their information as they load in, but only send their specific information when they send.

    public void OnEnable()
    {
        PlayerBehavior.callTestButton += ToggleChat;
        PhoneButtons.ToggleInputField += ToggleInputField;
        PhoneButtons.MoveChatField += MoveChatbox;
        PhoneButtons.ToggleMenuLocation += ChangeMenu;
        PhoneBehavior.ToggleInputField += ToggleInputField;
    }

    public void OnDisable()
    {
        PlayerBehavior.callTestButton -= ToggleChat;
        PhoneButtons.ToggleInputField -= ToggleInputField;
        PhoneButtons.MoveChatField -= MoveChatbox;
        PhoneButtons.ToggleMenuLocation -= ChangeMenu;
        PhoneBehavior.ToggleInputField -= ToggleInputField;
    }


    [Client]
    void ToggleChat(PlayerBehavior player)
    {
        PlayerID = player.int_playerID;

        if (isChatOpen)
        {
            inputField.Select();
            inputField.ActivateInputField();
        }
        else
        {
            inputField.DeactivateInputField();
        }
    }

    [Client]
    public void Send(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) { return; }

        CmdSendMessage(inputField.text);

        inputField.text = string.Empty;

        inputField.ActivateInputField(); // Continue typing after sending.  
    }

    [Command(requiresAuthority = false)]
    private void CmdSendMessage(string message, NetworkConnectionToClient sender = null)
    {
        int senderID = sender != null ? sender.connectionId : -1; // Get sender's ID

        RpcHandleMessage($"{message}", senderID);
    }

    [ClientRpc]
    private void RpcHandleMessage(string message, int senderID)
    {
        SpawnChatBubble(message, senderID);
    }

    private void SpawnChatBubble(string message, int senderID)
    {
        // Get the RectTransform of chatBox
        RectTransform chatBoxRect = chatBox.GetComponent<RectTransform>();

        // Move chatBox's top up by 50 units
        chatBoxRect.offsetMax += new Vector2(0, 50);

        // Instantiate new chat bubble
        GameObject newBubble = Instantiate(chatBubblePrefab, chatBox);

        newBubble.GetComponent<ChatBubbleInformation>().text_playerName.text = $"{senderID}";

        newBubble.GetComponent<ChatBubbleInformation>().text_message.text = message;

        if (PlayerID == senderID) //Compare the ID of who sent it to the player's own ID. Basically if the player is ourselves spawn the bubble differently to showcase that.
        {
            newBubble.GetComponent<ChatBubbleInformation>().backgroundBubble.GetComponent<Image>().color = new Color(0.07058824f, 0.7882354f, 0.6039216f, 1f);
            newBubble.GetComponent<ChatBubbleInformation>().backgroundBubble.GetComponent<RectTransform>().anchoredPosition -= new Vector2(35, 0);
        }
        else //if we're not the player
        {
            SendSoundToPlayers?.Invoke();
        }
    }

    [Client]
    private void ToggleInputField(bool toggle)
    {
        isChatOpen = toggle;

        if (toggle)
        {
            inputField.Select();
            inputField.ActivateInputField();

        }
        else
        {
            inputField.DeactivateInputField();
        }
    }

    private void MoveChatbox(int units)
    {
        RectTransform chatBoxRect = chatBox.GetComponent<RectTransform>();

        // Get the current top value dynamically, this has to be negative as for some reason it doesn't take the negative in the top value into account.
        float dynamicTop = -chatBoxRect.offsetMax.y;

        if (dynamicTop >= -100) return; // Check the top value to see if it's low enough to necessitate movement.

        // Calculate the new bottom value, basically add the current bottom value by the units. 
        float newBottom = chatBoxRect.offsetMin.y + units;

        // Clamp between dynamic top and 1.5
        newBottom = Mathf.Clamp(newBottom, dynamicTop + 100, 1.5f);

        // Apply the clamped value
        chatBoxRect.offsetMin = new Vector2(chatBoxRect.offsetMin.x, newBottom);
    }

    private void ChangeMenu(int roomID)
    {

        switch (roomID)
        {
            case (int)CurrentMenuType.Chat:
                contactsList.SetActive(false);
                chatRoom.SetActive(true);
                break;
            case (int)CurrentMenuType.Contacts:
                chatRoom.SetActive(false);
                contactsList.SetActive(true);
                break;
            case (int)CurrentMenuType.Base:
                Debug.Log("Not Set yet");
                break;
            default:
                Debug.Log("Phone menu out of bounds");
                break;
        }



    }
}
