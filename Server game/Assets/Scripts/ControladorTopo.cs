using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Networking.Transport.Samples;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class ControladorTopo : MonoBehaviour
{
    public GameObject topo;

    //Spawn parent per organitzar els topos
    public Transform spawnParent;

    public ServerBehaviour sb;
    private Transform posTopo;
    private Coroutine rutinaComerFlor;

    //Posicions ocupades amb topos
    private List<Vector2> posicionesOcupadas = new List<Vector2>();

    //Topos que hi ha
    private List<GameObject> Topos = new List<GameObject>(Enumerable.Repeat<GameObject>(null, 100));

    void Start()
    {
        posTopo = gameObject.transform;

        InvokeRepeating("SpawnearTopo", 2f, 10f);
        InvokeRepeating("SpawnearTopo", 4f, 10f);
        InvokeRepeating("SpawnearTopo", 6f, 10f);//ES creen tres topos al principi

        InvokeRepeating("actualizarPosTopos", 1f,1f);
    }

    private void SpawnearTopo()//Spawn de topos, fins que no trobi posicio buida no instancia
    {
        int posX = 0;
        int posY = 0;
        Vector2 nuevaPosicion;

        do
        {
            posX = Random.Range(-1, 2) * 3;
            posY = Random.Range(-1, 2) * 3;
            nuevaPosicion = new Vector2(posX, posY);
        }
        while (posicionesOcupadas.Contains(nuevaPosicion));


        posTopo.position = nuevaPosicion;
        posicionesOcupadas.Add(nuevaPosicion);
        GameObject topoActual = Instantiate(topo, posTopo.position, Quaternion.identity, spawnParent);
        int clauTopo = Random.Range(0, 100);
        topoActual.GetComponent<topo>().assignarClau(clauTopo);
        Topos[clauTopo] = topoActual;

    }

    public IEnumerator Destruir(GameObject objeto, double tiempoDestruccion)//Destruiex topos o flors, es guarda una clau, per a fer mes facil acces i borrar. 
    {

        float tiempoD = (float)tiempoDestruccion;
        yield return new WaitForSeconds(tiempoD);

        if (objeto != null)
        {
            if (objeto.transform.tag == "Topo")
            {
                int indice = objeto.GetComponent<topo>().clauTopoPersonal();
                if (indice >= 0 && indice < Topos.Count)
                {
                    Topos[indice] = null;
                }
            }
            else
            {
                sb.EnviarFlorMenjada(objeto);
                sb.EsborrarFlor(objeto);
            }

            posicionesOcupadas.Remove(objeto.transform.position);
            Destroy(objeto);

        }
    }

    public void comerFlor(GameObject Flor, int clauTopo)
    {
        rutinaComerFlor = StartCoroutine(Destruir(Flor, 4));
    }

    public void pararTopo(GameObject topoAct)
    {
        posicionesOcupadas.Remove(topoAct.transform.position);
        Destroy(topoAct);
        StopCoroutine(rutinaComerFlor);
    }

    public void actualizarPosTopos()
    {
        sb.EnviarPosicionsTalps(posicionesOcupadas);
    }

}
