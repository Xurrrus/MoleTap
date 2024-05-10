using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport.Samples;
using UnityEngine;

public class SelectCharacter : MonoBehaviour
{
    public void callSelectCharacter(int c)//seleccio de personatge.
    {
        ClientBehaviour obj = GameObject.FindGameObjectWithTag("Manager").GetComponent<ClientBehaviour>(); 
        
        ValoresGenerales.idJugadorSelecionat = c;
        if(c == 0) ValoresGenerales.JugadorSeleccionado = "Anec";
        else if(c == 1) ValoresGenerales.JugadorSeleccionado = "Castor";

        obj.sendSelectCharacter(c); 
    }
}
