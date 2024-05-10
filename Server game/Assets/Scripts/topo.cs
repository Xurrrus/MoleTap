using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport.Samples;


public class topo : MonoBehaviour
{
    private ControladorTopo ct;
    public ServerBehaviour sb;

    private int clauTopo;
    public Coroutine rutinaDestruirTopo = null;
    public bool estaMenjant = false;

    //Llista de les flors que pot menjar el topo
    private List<Collider2D> llistaFlors = new List<Collider2D>();
    //Numero flors que veu el topo per menjar
    public int hihaFlor = 0;


    void Start()
    {
        ct = GameObject.FindWithTag("topoManager").GetComponent<ControladorTopo>();
        rutinaDestruirTopo = StartCoroutine(ct.Destruir(this.gameObject, 8));
    }


    void Update()
    {
        if (hihaFlor > 0 && !estaMenjant)
            if (llistaFlors.Count > 0)
                ct.comerFlor(llistaFlors[0].gameObject, clauTopo);
       
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {

        if (collision.gameObject.transform.tag == "Flor")
        {
            hihaFlor += 1;
            if (rutinaDestruirTopo != null)
            {
                StopCoroutine(rutinaDestruirTopo);
                rutinaDestruirTopo = null;
            }
            llistaFlors.Add(collision);
            
        }

    }


    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.transform.tag == "Tronco")
        {
            float dist = Vector2.Distance(gameObject.transform.position, collision.gameObject.transform.position);//Si es troba molt a prop, es que l'ha tocat i s'ha de borrar
            StartCoroutine(ct.Destruir(gameObject, .1));


        }
        if (collision.gameObject.transform.tag == "Flor")
        {

            if (!estaMenjant) 
            { 
                ct.comerFlor(collision.gameObject, clauTopo);
                estaMenjant = true; 
            }

        }



    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.transform.tag == "Flor")
        {
            estaMenjant = false;

            if (hihaFlor > 0) hihaFlor -= 1;
            if (rutinaDestruirTopo == null && hihaFlor == 0) rutinaDestruirTopo = StartCoroutine(ct.Destruir(this.gameObject, 8));//Tornar a engegar la rutina per destuir el topo si no te flors al voltant

            llistaFlors.RemoveAt(0);
        }

    }

    public void assignarClau(int clau)
    {
        clauTopo = clau;
    }

    public int clauTopoPersonal()
    {
        return clauTopo;
    }
}