using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractSymbol : MonoBehaviour
{

    public enum SymbolState
    {
        Interact,
        Locked,
        Trap,
        TrapSet,
        TrapActive

    }
    public SymbolState symbolState;
    public Material interactSymbol;
    public Material lockedSymbol;
    public Material trapSymbol;
    public Material trapSetSymbol;
    public Material trapActiveSymbol;
    public bool b_isActive = false;
    public bool b_canBeActive = true; // This is for clauses when we players can't interact with objects such as unactivated traps for players.

    public void SetSymbol()
    {
        switch (symbolState)
        {
            case SymbolState.Interact:
                GetComponent<Renderer>().material = interactSymbol;
                break;
            case SymbolState.Locked:
                GetComponent<Renderer>().material = lockedSymbol;
                break;
            case SymbolState.Trap:
                GetComponent<Renderer>().material = trapSymbol;
                break;
            case SymbolState.TrapSet:
                GetComponent<Renderer>().material = trapSetSymbol;
                break;
            case SymbolState.TrapActive:
                GetComponent<Renderer>().material = trapActiveSymbol;
                break;
            default:
                break;
        }

    }

    public void Activate()
    {
        if (b_isActive)
        {
            if (!b_canBeActive) return;
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        b_isActive = false;
        Activate();
    }
}
