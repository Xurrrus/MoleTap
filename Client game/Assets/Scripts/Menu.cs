using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport.Samples;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Networking.Transport;

public class Menu : MonoBehaviour
{
    public GameObject canvasMenuWin;
    public GameObject canvasMenuLose;
    public GameObject gameManagerPrefab;
    private void Start()
    {

       if (ValoresGenerales.finGame == 0) canvasMenuWin.SetActive(true);
       else if (ValoresGenerales.finGame == 1) canvasMenuLose.SetActive(true);
    }

    public void Restart()
    {
        GameObject manager = GameObject.FindGameObjectWithTag("Manager");
        ClientBehaviour cb = manager.GetComponent<ClientBehaviour>();

        cb.EnviarFinJoc();
        StartCoroutine(destruirCliente(manager));

      
    }

    public void Quit()
    {
        Application.Quit();
    }

    IEnumerator destruirCliente(GameObject manager) 
    {    
        yield return new WaitForSeconds(2f);
        Destroy(manager);
        SceneManager.LoadScene(0);
        Instantiate(gameManagerPrefab);

    }
}
