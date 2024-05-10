using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;

using Unity.Networking.Transport;
using System.Net;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using System.IO;


namespace Unity.Networking.Transport.Samples
{
    public class ServerBehaviour : MonoBehaviour
    {
        NetworkDriver m_Driver;
        NativeList<NetworkConnection> m_Connections;
        NetworkPipeline myPipeline;
        public TMP_Text tMP_Text;

        private uint NumClient = 1;
        private uint Servidor = 0;
        private double TempsEnces;
        private int playerEnviat;
        private bool Comencament = true;

        private List<int> PersonajesSeleccionados = new List<int>();
        public GameObject[] jugadors;
        public Transform[] spawnPositions;
        public Transform spawnParent;
        public GameObject[] players;
        public GameObject topo;
        public GameObject controladorTp; 
        
        private Dictionary<int, int> connetionObject = new Dictionary<int, int>();
        private Dictionary<int, Vector2> ultimasPosicionesEnviadas = new Dictionary<int, Vector2>();

        //OBJECTES (FLORS / TRONCS)
        public List<GameObject> objectesActuals;
        public List<Vector3> posObjecteNoDisponible;
        public List<Vector3> posObjectesActuals;
        public Transform objParent;

        //FLORS
        public GameObject florPrefab;

        //TRONCS
        public GameObject troncPrefab;


        //Spawn de objectes amb el temps
        private Vector3 transformTronco = new Vector3(-5, 0, 0);
        private Vector3 transformFlor = new Vector3(5, 0, 0);//Posicions

        //Joc parametres
        private int maxVictoria = 20;
        public int numFlors = 0;
        private int maxNumFlorsMenjades = 35;
        private int numeroFlorsMenjades = 0;
        private bool WinGame = false;
        public float moveThreshold = 0f;


        void Start()
        {
            m_Driver = NetworkDriver.Create();
            myPipeline = m_Driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
            m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

            var endpoint = NetworkEndpoint.AnyIpv4.WithPort(7777);
            if (m_Driver.Bind(endpoint) != 0)
            {
                Debug.LogError("Failed to bind to port 7777.");
                return;
            }
            m_Driver.Listen();

            GameObject newObjecte = Instantiate(florPrefab, transformFlor, Quaternion.identity);
            objectesActuals.Add(newObjecte);
            posObjectesActuals.Add(newObjecte.transform.position);
            comptarFlors();

            GameObject newObjecte2 = Instantiate(troncPrefab, transformTronco, Quaternion.identity);
            objectesActuals.Add(newObjecte2);
            posObjectesActuals.Add(newObjecte2.transform.position);//Spawn de la primera flor i tronc als costats

        }

        void OnDestroy()
        {
            if (m_Driver.IsCreated)
            {
                m_Driver.Dispose();
                m_Connections.Dispose();
            }
        }

        void Update()
        {
            m_Driver.ScheduleUpdate().Complete();

            // Clean up connections.
            for (int i = 0; i < m_Connections.Length; i++)
            {
                if (!m_Connections[i].IsCreated)
                {
                    m_Connections.RemoveAtSwapBack(i);
                    i--;
                }
            }

            // Accept new connections.
            NetworkConnection c;
            while ((c = m_Driver.Accept()) != default)
            {
                m_Connections.Add(c);
                Debug.Log("Accepted a connection.");
                tMP_Text.text = "Server IP: " + GetLocalIPAddress();
            }

            for (int i = 0; i < m_Connections.Length; i++)
            {
                DataStreamReader stream;
                NetworkEvent.Type cmd;
                while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream, out var receivePipeline)) != NetworkEvent.Type.Empty)
                {
                    if (cmd == NetworkEvent.Type.Data)
                    {
                        if (Comencament)//Nomes fa falta al principi, bool per a comprovar-ho
                        {
                            m_Driver.BeginSend(myPipeline, m_Connections[i], out var writer);
                            TempsEnces = Time.time;

                            if (NumClient == 1)
                            {
                                writer.WriteByte((byte)'a');
                                writer.WriteUInt(Servidor);
                                writer.WriteUInt(NumClient);
                                writer.WriteDouble(TempsEnces);
                                NumClient += 1;
                            }
                            else if (NumClient == 2)
                            {
                                writer.WriteByte((byte)'b');
                                writer.WriteUInt(Servidor);
                                writer.WriteUInt(NumClient);
                                writer.WriteUInt(NumClient - 1);
                                writer.WriteDouble(TempsEnces);
                                Comencament = false;
                            }

                            m_Driver.EndSend(writer);
                        }

                        char funcio = (char)stream.ReadByte();

                        if(funcio == 'j'){
                            //Pot agafar el character? ((int) -> idCharacter)
                            //Enviem nomes resposta si/no, el valor del seleccionat ja ho te el player

                            int value = stream.ReadInt(); //"value" es "idJugador"
                            if(value != -1){
                                if(!PersonajesSeleccionados.Contains(value)){

                                    PersonajesSeleccionados.Add(value);
                                    enviarPersonatge(1, i); //Si es pot agafar

                                    GameObject newPrefab = Instantiate(jugadors[value], spawnPositions[value].position, Quaternion.identity, spawnParent);
                                    //players.Insert(value, newPrefab);
                                    players[value] = newPrefab;
                                    connetionObject.Add(i, value);

                                    ActualitzarPosClients();
                                   
                                }
                                else enviarPersonatge(0, i); //No es pot agafar                               
                            }
                        }
                        else if(funcio == 'p'){
                            //Obtenim inputs del jugador
                            //Movem el jugador
                            //Enviem nova posicio

                            int playerID = stream.ReadInt();
                            float movimentH = stream.ReadFloat();
                            float movimentV = stream.ReadFloat();

                            if (!controladorTp.activeSelf){ 
                                controladorTp.SetActive(true); 
                                ActualitzarPosObjectes();
                                playerEnviat = playerID; 
                            }

                            if(playerID != playerEnviat){
                                ActualitzarPosObjectes();
                            }

                            MoverJugador(movimentH, movimentV, playerID);
                        }
                        else if(funcio == 'e'){
                            //Sabem que el jugador ha clicat l'espai
                            //Afegim canvis
                            //Enviem canvis als clients
                            int playerID = stream.ReadInt();
                            AgafarDeixarObj(playerID);
                        }
                        else if (funcio == 'f')
                        {
                            //Final de partida
                            SceneManager.LoadScene(0);
                        }

                        personajesDisponibles(i);
                    }
                    else if (cmd == NetworkEvent.Type.Disconnect)
                    {
                        Debug.Log("Client disconnected from the server.");
                        m_Connections[i] = default;
                        break;
                    }
                }
            }
        }

        private string GetLocalIPAddress()
        {
            string localIP = "";
            foreach (IPAddress ip in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }

        private void enviarPersonatge(int seleccionPersonaje, int c){//si ha escollit el personatge
            m_Driver.BeginSend(myPipeline, m_Connections[c], out var writer);

            writer.WriteByte((byte)'s');
            writer.WriteInt(seleccionPersonaje);
            m_Driver.EndSend(writer);
        }

        private void personajesDisponibles(int c){//personatges no escollits
            m_Driver.BeginSend(myPipeline, m_Connections[c], out var writer);

            writer.WriteByte((byte)'d');
            writer.WriteInt(PersonajesSeleccionados.Count);
            for(int i=0; i < PersonajesSeleccionados.Count; i++){
                writer.WriteInt(PersonajesSeleccionados[i]);
            }

            m_Driver.EndSend(writer);
        }
        
        
        private void MoverJugador(float movimientoHorizontal, float movimientoVertical, int playerID)
        {
            //Fent el moviment de quadricula no cal rigidBody, suma de direccions
            if(movimientoHorizontal > moveThreshold) movimientoHorizontal = 1;
            if(movimientoHorizontal < -moveThreshold) movimientoHorizontal = -1;

            if(movimientoVertical > moveThreshold) movimientoVertical = 1;
            if(movimientoVertical < -moveThreshold) movimientoVertical = -1;

            Vector3 movePosition = players[playerID].transform.position + new Vector3((int)movimientoHorizontal, (int)movimientoVertical, 0f);

            //Comporovem que esta dins el mapa
            if((movePosition.x < 5 && movePosition.x > -5 && movePosition.y < 5 && movePosition.y > -5) || (movePosition == transformFlor) || (movePosition == transformTronco)){
                players[playerID].transform.position = movePosition;            

                //Enviem nova posicio
                ActualitzarPosClients();
            }
        }


        void ActualitzarPosClients(){
            foreach (int clau in connetionObject.Keys){
                foreach (KeyValuePair<int, int> parella in connetionObject){
                    float x = players[parella.Value].transform.position.x;
                    float y = players[parella.Value].transform.position.y;
                    EnviarPosicioPersonatges(clau, parella.Value, x, y);
                }
            }
        }

        void ActualitzarPosObjectes(){
            foreach (int clau in connetionObject.Keys){
                EnviarPosicionsObjectes(clau);
            }
        }

        private void EnviarPosicioPersonatges(int connexioID, int playerID, float x, float y){
            m_Driver.BeginSend(myPipeline, m_Connections[connexioID], out var writer);

            writer.WriteByte((byte)'p');
            writer.WriteInt(playerID);
            writer.WriteFloat(x);
            writer.WriteFloat(y);

            m_Driver.EndSend(writer);
        }

        private void EnviarPosicionsObjectes(int connexioID)
        {
            m_Driver.BeginSend(myPipeline, m_Connections[connexioID], out var writer);

            writer.WriteByte((byte)'f');
            writer.WriteInt(objectesActuals.Count);
            for(int i=0; i<objectesActuals.Count; i++){
                writer.WriteFloat(objectesActuals[i].transform.position.x);
                writer.WriteFloat(objectesActuals[i].transform.position.y);
                if(objectesActuals[i].tag == "Flor") writer.WriteInt(0); // 0 == FLOR
                else writer.WriteInt(1); // 1 == TRONC
            }

            m_Driver.EndSend(writer);
        }

        public void EnviarFlorMenjada(GameObject flor)
        {
            foreach (int clau in connetionObject.Keys)
            {
                m_Driver.BeginSend(myPipeline, m_Connections[clau], out var writer);

                writer.WriteByte((byte)'g');
                writer.WriteFloat(flor.transform.position.x);
                writer.WriteFloat(flor.transform.position.y);
                m_Driver.EndSend(writer);
                
            }

        }



        public void EsborrarFlor(GameObject flor){
           
            if (objectesActuals.Contains(flor)){
                objectesActuals.Remove(flor);
                posObjectesActuals.Remove(flor.transform.position);
            }
            numeroFlorsMenjades++;
            if (numeroFlorsMenjades == maxNumFlorsMenjades) { 
                FinJoc(); 
            }
            comptarFlors();
            

        }

        public void AgafarDeixarObj(int playerID){
            AnecController anecController = players[playerID].GetComponent<AnecController>();
            bool anecTeFlor = anecController.teFlor;
            if(!anecTeFlor){

                bool trobat = false;
                int i=0;
                while(i<objectesActuals.Count && !trobat){
                    bool esPotAgafar = ((playerID==0 && objectesActuals[i].tag == "Flor") || (playerID==1 && objectesActuals[i].tag != "Flor"));
                    if(objectesActuals[i].transform.position == players[playerID].transform.position && esPotAgafar) trobat = true;
                    else i++;
                }

                if(trobat){
                    //Pot Agafar Objecte
                    AgafarObjecte(anecController, playerID);               
                }
            }
            else{
                if(!posObjectesActuals.Contains(players[playerID].transform.position)){
                    //Pot deixar flor
                    DeixarObjecte(anecController, playerID);
                }
            }
        }

        void AgafarObjecte(AnecController anecController, int playerID){
            
            //Si agafem un tronc o una flor es crida el metode perque n'aparegui un de nou
            if(transformFlor == players[playerID].transform.position)
            {
                StartCoroutine(spawnFlores(3f));
            }
            else if (transformTronco == players[playerID].transform.position)
            {
                StartCoroutine(spawnTroncos(4f));
            }

            posObjectesActuals.Remove(players[playerID].transform.position);
            bool trobat = false;
            int i=0;
            while(i<objectesActuals.Count && !trobat){
                if(objectesActuals[i].transform.position == players[playerID].transform.position) trobat = true;
                else i++;
            }

            if(trobat){
                Destroy(objectesActuals[i]);
                objectesActuals.Remove(objectesActuals[i]);
            }

            Vector3 newPos = new Vector3(players[playerID].transform.position.x, players[playerID].transform.position.y, 10f);
            players[playerID].transform.GetChild(0).gameObject.SetActive(true);
            anecController.teFlor = true;

            comptarFlors();

            ActualitzarPosObjectes();
            EnviarFlorAgafada(1, playerID);
        }

        void DeixarObjecte(AnecController anecController, int playerID){

            GameObject prefab;
            if(playerID == 0) prefab = florPrefab;
            else prefab = troncPrefab;

           
            GameObject newObjecte = Instantiate(prefab, players[playerID].transform.position, Quaternion.identity, objParent);
            objectesActuals.Add(newObjecte);
            posObjectesActuals.Add(newObjecte.transform.position);
            players[playerID].transform.GetChild(0).gameObject.SetActive(false);
            anecController.teFlor = false;

            if (playerID == 1) StartCoroutine(destruirTronc(newObjecte, playerID));
            else if (playerID == 0) comptarFlors();
            ActualitzarPosObjectes();
            EnviarFlorAgafada(0, playerID);
        }

        void EnviarFlorAgafada(int agafada, int playerID){
            foreach (int clau in connetionObject.Keys){
                m_Driver.BeginSend(myPipeline, m_Connections[clau], out var writer);

                writer.WriteByte((byte)'c');
                writer.WriteInt(playerID);
                writer.WriteInt(agafada); // 1 = Si, 0 = No

                m_Driver.EndSend(writer);
            }
        }
       

        IEnumerator spawnTroncos(float temps)
        {

            yield return new WaitForSeconds(temps);         
            GameObject newObjecte = Instantiate(troncPrefab, transformTronco, Quaternion.identity);
            objectesActuals.Add(newObjecte);
            posObjectesActuals.Add(newObjecte.transform.position);

            ActualitzarPosObjectes();
        }
        IEnumerator spawnFlores(float temps)
        {
            yield return new WaitForSeconds(temps);
            if (!posObjectesActuals.Contains(transformFlor)) //si la llista no conte la posicio, es a dir, si hi ha una flor no n'instancis una altre
            { 
                GameObject newObjecte = Instantiate(florPrefab, transformFlor, Quaternion.identity);
                objectesActuals.Add(newObjecte);
                posObjectesActuals.Add(newObjecte.transform.position);
                comptarFlors();
                ActualitzarPosObjectes();

            }
            
            
        }

        IEnumerator destruirTronc(GameObject tronc,int playerID)
        {
            
            posObjectesActuals.Remove(players[playerID].transform.position);
            bool trobat = false;
            int i = 0;
            while (i < objectesActuals.Count && !trobat)
            {
                if (objectesActuals[i].transform.position == players[playerID].transform.position) trobat = true;
                else i++;
            }

            yield return new WaitForSeconds(1f);
            if (trobat)
            {
                Destroy(objectesActuals[i]);
                objectesActuals.Remove(objectesActuals[i]);
            }

            ActualitzarPosObjectes();
        }
        void EnviarNumeroFlors()//Numero de flors menjades i numero de flors posades actualment
        {
            foreach (int clau in connetionObject.Keys)
            {
                foreach (KeyValuePair<int, int> parella in connetionObject)
                {
                    m_Driver.BeginSend(myPipeline, m_Connections[clau], out var writer);
                    writer.WriteByte((byte)'l');
                    writer.WriteInt(numFlors);
                    writer.WriteInt(numeroFlorsMenjades);
                    m_Driver.EndSend(writer);


                }
            }
        }

        private void comptarFlors()
        {
            int florsActuals = 0;
            for(int i = 0; i < objectesActuals.Count; i++)
            {
                if (objectesActuals[i].transform.tag == "Flor") florsActuals++;
            }

            numFlors = florsActuals;
            if (numFlors == maxVictoria)
            {
                WinGame = true;
                FinJoc();//Final de joc
            }

            EnviarNumeroFlors();
        }

        void FinJoc()
        {
            foreach (int clau in connetionObject.Keys)
            {
                foreach (KeyValuePair<int, int> parella in connetionObject) 
                {
                    m_Driver.BeginSend(myPipeline, m_Connections[clau], out var writer);
                    writer.WriteByte((byte)'e');
                    if (WinGame) writer.WriteInt(0); // 0 == Win
                    else writer.WriteInt(1); // 1 == Lose
                    m_Driver.EndSend(writer);
                }
            }

            StartCoroutine(destruirServidor());
        }

        IEnumerator destruirServidor()
        {
            yield return new WaitForSeconds(4f);
            SceneManager.LoadScene(0);

            Destroy(this.gameObject);
        }

        public void EnviarPosicionsTalps( List<Vector2> toposActuals)
        {
            foreach (int clau in connetionObject.Keys)
            {
                m_Driver.BeginSend(myPipeline, m_Connections[clau], out var writer);

                writer.WriteByte((byte)'x');
                writer.WriteInt(toposActuals.Count);
                for (int i = 0; i < toposActuals.Count; i++)
                {
                    writer.WriteInt((int)toposActuals[i].x);
                    writer.WriteInt((int)toposActuals[i].y);
                }

                m_Driver.EndSend(writer);
            }
            
        }

    }
}