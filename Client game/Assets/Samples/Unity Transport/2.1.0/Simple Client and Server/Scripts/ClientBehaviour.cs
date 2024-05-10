using UnityEngine;
using Unity.Networking.Transport;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace Unity.Networking.Transport.Samples
{
    public class ClientBehaviour : MonoBehaviour
    {
        NetworkDriver m_Driver;
        NetworkConnection m_Connection;
        NetworkPipeline myPipeline;

        //Ip text
        public InputField ip_Text;
        public InputField port_Text;
        int selectCharacter = -1;

        private List<int> PersonajesSeleccionados = new List<int>();

        private GameObject[] Botones;
        public GameObject[] jugadorsActuals;
        private Vector2 posicio;

        //Topos
        public List<GameObject> Topos = new List<GameObject>(Enumerable.Repeat<GameObject>(null, 100));
        public GameObject[] prefabs;
        public GameObject topoPrefab;
        private bool buscat = false;
        private int escenaActual = 0;

        public float tempsEntreMoviment = 0.5f;
        public bool canviInputs = true;

        //OBJECTES (FLORS I TRONCS)
        public Transform objParent;
        public List<GameObject> objActuals;

        //FLORS
        public GameObject florPrefab;
        private int numFlors;
        private int numFlorsMenjades;
        private TMP_Text canvasFlors = null;
        private TMP_Text canvasFlorsMenjades = null;

        //TRONCS
        public GameObject troncPrefab;




        void Awake()
        {
            GameObject[] objs = GameObject.FindGameObjectsWithTag("Manager");

            if (objs.Length > 1)
            {
                Destroy(this.gameObject);
            }

            DontDestroyOnLoad(this.gameObject);
            ValoresGenerales.finGame = -1;
        }

        void Start()
        {
            m_Driver = NetworkDriver.Create();
            myPipeline = m_Driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
        }

        public void Connectar()
        {
            ushort u_port = UInt16.Parse(port_Text.text);

            var endpoint = NetworkEndpoint.Parse(ip_Text.text, u_port);
            m_Connection = m_Driver.Connect(endpoint);
        }

        void OnDestroy()
        {
            m_Driver.Dispose();
        }

        void Update()
        {
            m_Driver.ScheduleUpdate().Complete();

            if (!m_Connection.IsCreated)
            {
                return;
            }

            Unity.Collections.DataStreamReader stream;
            Unity.Collections.DataStreamWriter streamWriter;
            NetworkEvent.Type cmd;

            while ((cmd = m_Connection.PopEvent(m_Driver, out stream, out var receivePipeline)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Connect)
                {
                    m_Driver.BeginSend(myPipeline, m_Connection, out streamWriter);
                    streamWriter.WriteByte((byte)'j');
                    streamWriter.WriteInt(selectCharacter);

                    if (m_Driver.EndSend(streamWriter) == (int)Error.StatusCode.NetworkSendQueueFull)
                    {
                       //En cas que doni error....
                    }
                    SceneManager.LoadScene(1);
                    
                }
                else if (cmd == NetworkEvent.Type.Data)
                {
                    char funcion = (char)stream.ReadByte();
                    if (funcion == 'a')
                    {
                        uint value = stream.ReadUInt();
                        Debug.Log($"El Nombre del servidor es: {value}.");
                        uint NombreCliente = stream.ReadUInt();
                        Debug.Log($"Mi Nombre es: {NombreCliente}.");
                        double TiempoServidor = stream.ReadDouble();
                        Debug.Log($"El tiempo que lleva encendido es: {TiempoServidor}.");
                    }
                    else if (funcion == 'b')
                    {
                        uint value = stream.ReadUInt();
                        Debug.Log($"El Nombre del servidor es: {value}.");
                        uint NombreCliente = stream.ReadUInt();
                        Debug.Log($"Mi Nombre es: {NombreCliente}.");
                        uint NombreClienteAnterior = stream.ReadUInt();
                        Debug.Log($"El anterior cliente era: {NombreClienteAnterior}.");
                        double TiempoServidor = stream.ReadDouble();
                        Debug.Log($"El tiempo que lleva encendido es: {TiempoServidor}.");
                    }
                    else if (funcion == 's')
                    { //Resposta Selecionar character
                        if (stream.ReadInt() == 1)
                        { //Si l'ha pogut agafar...                         
                            SceneManager.LoadScene(2);
                        }
                    }
                    else if (funcion == 'd')//Desactivar el personatge en cas que l'altre company l'ha agafat
                    {
                        int v = stream.ReadInt();
                        for (int i = 0; i < v; i++)
                        {
                            PersonajesSeleccionados.Add(stream.ReadInt());
                        }
                        desactivarPersonaje();
                    }
                    else if (funcion == 'p')
                    { //Llegim la posicio
                        int playerID = stream.ReadInt();
                        float x = stream.ReadFloat();
                        float y = stream.ReadFloat();

                        if (SceneManager.GetActiveScene().buildIndex == 2)
                        {
                            if (jugadorsActuals.Length > playerID)
                            {
                                if (jugadorsActuals[playerID] == null) //Si no existeix el personatge -> Instanciem
                                {
                                    GameObject newPrefab = Instantiate(prefabs[playerID], new Vector2(x, y), Quaternion.identity);
                                    jugadorsActuals[playerID] = newPrefab;
                                }
                                else
                                { //Si existeix el personatge -> Movem
                                    jugadorsActuals[playerID].transform.position = new Vector2(x, y);
                                }
                            }
                            else
                            { //Si no existeix el personatge -> Instanciem
                                GameObject newPrefab = Instantiate(prefabs[playerID], new Vector2(x, y), Quaternion.identity);
                                jugadorsActuals[playerID] = newPrefab;
                            }
                        }
                        else enviarPosicionSecundario(1, 1);
                    }
                    else if (funcion == 'f')//Inicials Flors
                    {
                        for(int i=0; i<objActuals.Count; i++){
                            Destroy(objActuals[i]);
                        }
                        objActuals.Clear();

                        int numFlors = stream.ReadInt();
                        for (int i = 0; i < numFlors; i++)
                        {
                            float x = stream.ReadFloat();
                            float y = stream.ReadFloat();
                            int idObjecte = stream.ReadInt();

                            GameObject prefab;
                            if(idObjecte == 0) prefab = florPrefab;
                            else prefab = troncPrefab;

                            GameObject newObj = Instantiate(prefab, new Vector3(x, y, 0f), Quaternion.identity);
                            objActuals.Add(newObj);
                        }
                    }
                    else if (funcion == 'g')//Borrar Flors
                    {
                        float x = stream.ReadFloat();
                        float y = stream.ReadFloat();

                        Vector3 vecFlor = new Vector3(x, y, 0f);

                        for (int i = 0; i < objActuals.Count; i++)
                        {
                            if (objActuals[i].transform.position == vecFlor) { GameObject florActual = objActuals[i];  objActuals.RemoveAt(i);  Destroy(florActual); i--; }
                        }
                    }
                    else if (funcion == 'c')//Afegir / treure flor bec anec
                    {
                        int playerID = stream.ReadInt();
                        int florABec = stream.ReadInt();
                        if(florABec == 1) jugadorsActuals[playerID].transform.GetChild(0).gameObject.SetActive(true);
                        else jugadorsActuals[playerID].transform.GetChild(0).gameObject.SetActive(false);
                    }
                    else if (funcion == 'l')//Numero de flors que hi ha en el mapa posades i numero de flors menjades per topos pasat a text pel canvas.
                    {
                        if(canvasFlors == null)canvasFlors = GameObject.FindGameObjectWithTag("numFlors").GetComponent<TMP_Text>();                      
                        if(canvasFlorsMenjades == null) canvasFlorsMenjades = GameObject.FindGameObjectWithTag("numFlorsMenjades").GetComponent<TMP_Text>();                      
                        numFlors = stream.ReadInt();
                        numFlorsMenjades = stream.ReadInt();

                        canvasFlors.text = "Nº flors: " + numFlors + "/20";
                        canvasFlorsMenjades.text = "Flors menjades " + numFlorsMenjades + "/35";
                    }
                    else if (funcion == 'e')//Fin juego
                    {
                        ValoresGenerales.finGame = stream.ReadInt();
                        enabled = false;
                        FinMenu();
                    }
                    else if (funcion == 'x')//Es borren els topos i es tornen a crear.
                    {
                        for (int i = 0; i < Topos.Count; i++)
                        {
                            Destroy(Topos[i]);
                        }
                        Topos.Clear();

                        int numTopos = stream.ReadInt();
                        for (int i = 0; i < numTopos; i++)
                        {
                            int x = stream.ReadInt();
                            int y = stream.ReadInt();

                            GameObject newObj = Instantiate(topoPrefab, new Vector3(x, y, 0f), Quaternion.identity);
                            Topos.Add(newObj);
                        }
                    }
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client got disconnected from server.");
                    m_Connection = default;
                }
            }

            //Dins de l'Update, llegim els inputs quan existeixi el nostre jugador (id nostre jugador -> ValoresGenerales.idJugadorSelecionat)
            //NO HI HA PLAYER CONTROLLER -> Sino, tots els prefabs llegirien inputs -> mes d'un moviment per click
            if(jugadorsActuals.Length > ValoresGenerales.idJugadorSelecionat && jugadorsActuals[ValoresGenerales.idJugadorSelecionat] != null){
                LlegirInputs();
            }
        }

        public void sendSelectCharacter(int c)
        {         
            selectCharacter = c;
            sendCharacter();
        }

        public void sendCharacter(){
            Unity.Collections.DataStreamWriter streamWriter;
            if (m_Connection.IsCreated)
            {
                m_Driver.BeginSend(myPipeline, m_Connection, out streamWriter);

                streamWriter.WriteByte((byte)'j');
                streamWriter.WriteInt(selectCharacter); //"selectCharacter" es el mateix valor que " ValoresGenerales.idJugadorSelecionat"

                m_Driver.EndSend(streamWriter);
            }
        }

        private void desactivarPersonaje(){
            Botones = GameObject.FindGameObjectsWithTag("botones");

            if(PersonajesSeleccionados.Count > 0){
                for(int i = 0; i < Botones.Length; i++){
                
                    if(Botones[i].name == "CharacterOne" && PersonajesSeleccionados.Contains(0)){
                        Botones[i].SetActive(false);
                    }
                    else if(Botones[i].name == "CharacterTwo" && PersonajesSeleccionados.Contains(1)){
                        Botones[i].SetActive(false);
                    }
                }
            }
        }

        public void enviarPosicionSecundario(float x, float y)
        {
            Unity.Collections.DataStreamWriter streamWriter;
            if (m_Connection.IsCreated)
            {
                m_Driver.BeginSend(myPipeline, m_Connection, out streamWriter);

                streamWriter.WriteByte((byte)'p');
                streamWriter.WriteInt(ValoresGenerales.idJugadorSelecionat);
                streamWriter.WriteFloat(x);
                streamWriter.WriteFloat(y);

                m_Driver.EndSend(streamWriter);
            }
        }

        void LlegirInputs(){
            float movimientoHorizontal = Input.GetAxis("Horizontal");
            float movimientoVertical = Input.GetAxis("Vertical");
            bool espaiPremut = Input.GetKey(KeyCode.Space);

            if(canviInputs && espaiPremut){
                canviInputs = false;
                sendEspaiInput();
                StartCoroutine(esperarSegons());
            }

            //Nomes volem desplacaments (amunt, avall, dreta, esquerra)
        
            if(canviInputs && (movimientoHorizontal != 0f || movimientoVertical != 0f)){ 
                canviInputs = false;
                sendPositionCharacter(movimientoHorizontal, movimientoVertical);
                StartCoroutine(esperarSegons()); //Deixem un temps abans de poder fer el seguent moviment
            }
        }

        IEnumerator esperarSegons(){
            yield return new WaitForSeconds(tempsEntreMoviment);
            canviInputs = true;
        }

        //Nomes enviem idPlayer i inputs al servidor
        public void sendPositionCharacter(float movHoriz, float movVertic){
            if(jugadorsActuals.Length > ValoresGenerales.idJugadorSelecionat && (jugadorsActuals[ValoresGenerales.idJugadorSelecionat] != null)){
                Unity.Collections.DataStreamWriter streamWriter;
                if (m_Connection.IsCreated)
                {
                    m_Driver.BeginSend(myPipeline, m_Connection, out streamWriter);

                    streamWriter.WriteByte((byte)'p');
                    streamWriter.WriteInt(ValoresGenerales.idJugadorSelecionat);
                    streamWriter.WriteFloat(movHoriz);
                    streamWriter.WriteFloat(movVertic);

                    m_Driver.EndSend(streamWriter);
                }
            }
        }

        public void sendEspaiInput(){
            if(jugadorsActuals.Length > ValoresGenerales.idJugadorSelecionat && (jugadorsActuals[ValoresGenerales.idJugadorSelecionat] != null)){
                Unity.Collections.DataStreamWriter streamWriter;
                if (m_Connection.IsCreated)
                {
                    m_Driver.BeginSend(myPipeline, m_Connection, out streamWriter);

                    streamWriter.WriteByte((byte)'e');
                    streamWriter.WriteInt(ValoresGenerales.idJugadorSelecionat);

                    m_Driver.EndSend(streamWriter);
                }
            }
        }
        void FinMenu()//Escena final per triar si tornar a jugar o sortir
        {
           
            SceneManager.LoadScene(3);
       
        }

        public void EnviarFinJoc()//enviar que hem acabat, confirmacio, al servidor.
        {
            Unity.Collections.DataStreamWriter streamWriter;
            if (m_Connection.IsCreated)
            {
                m_Driver.BeginSend(myPipeline, m_Connection, out streamWriter);

                streamWriter.WriteByte((byte)'f');

                m_Driver.EndSend(streamWriter);
            }

        }
    }
}