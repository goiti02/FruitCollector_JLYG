using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using System.Xml.Serialization; // Necesario para XML

// --- ESTRUCTURAS DE DATOS PARA SERIALIZAR ---

[System.Serializable]
public class FruitSaveData
{
    public string fruitId;
    public Vector3 position;
}

[System.Serializable]
public class ChestSaveData
{
    public string chestId;
    public List<InventorySlot> inventory;
}

[System.Serializable]
public class SaveData
{
    public Vector3 playerPosition;
    public float totalPlayTime;
    public string saveDate;

    public List<InventorySlot> playerInventory = new List<InventorySlot>();
    public List<ChestSaveData> chestInventories = new List<ChestSaveData>();
    public List<FruitSaveData> sceneFruits = new List<FruitSaveData>();
}

// --- SERVICIO DE GUARDADO ---

public enum SaveFormat { JSON, XML }

public sealed class SaveGameService : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Elige el formato de guardado de la partida")]
    public SaveFormat formatoDeGuardado = SaveFormat.JSON;

    [Header("Base de datos")]
    [Tooltip("Arrastra aquí tus 4 archivos FruitData (Apple, Banana...) para poder hacerlas reaparecer")]
    public FruitData[] fruitDatabase;

    private string savePathJSON;
    private string backupPathJSON;
    private string savePathXML;
    private string backupPathXML;

    // Variables para el mensaje temporal en pantalla (Rúbrica 4.vi y 4.vii)
    private string displayMessage = "";
    private float messageTimer = 0f;

    private void Awake()
    {
        savePathJSON = Application.persistentDataPath + "/savegame.json";
        backupPathJSON = Application.persistentDataPath + "/savegame_backup.json";

        savePathXML = Application.persistentDataPath + "/savegame.xml";
        backupPathXML = Application.persistentDataPath + "/savegame_backup.xml";
    }

    private void Start()
    {
        LoadGame();
    }

    private void Update()
    {
        // Reducir el temporizador del mensaje en pantalla
        if (messageTimer > 0) messageTimer -= Time.deltaTime;
    }

    // Dibuja el texto en la pantalla temporalmente sin necesidad de Canvas
    private void OnGUI()
    {
        if (messageTimer > 0)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.fontStyle = FontStyle.Bold;

            // Sombra para que se lea mejor sobre el juego
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(22, 22, 800, 200), displayMessage, style);

            // Texto principal
            style.normal.textColor = Color.yellow;
            GUI.Label(new Rect(20, 20, 800, 200), displayMessage, style);
        }
    }

    public void SaveGame()
    {
        SaveData data = new SaveData();
        data.totalPlayTime = Time.realtimeSinceStartup;
        data.saveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        PlayerInventory playerInv = FindFirstObjectByType<PlayerInventory>();
        if (playerInv != null)
        {
            data.playerPosition = playerInv.transform.position;
            data.playerInventory = playerInv.inventory;
        }

        Chest[] chestsInScene = FindObjectsByType<Chest>(FindObjectsSortMode.None);
        foreach (Chest chest in chestsInScene)
        {
            data.chestInventories.Add(new ChestSaveData { chestId = chest.ChestId, inventory = chest.inventory });
        }

        Fruit[] fruitsInScene = FindObjectsByType<Fruit>(FindObjectsSortMode.None);
        foreach (Fruit fruit in fruitsInScene)
        {
            IPickable pickable = fruit.GetComponent<IPickable>();
            if (pickable != null)
            {
                data.sceneFruits.Add(new FruitSaveData { fruitId = pickable.Id, position = fruit.transform.position });
            }
        }

        if (formatoDeGuardado == SaveFormat.JSON) GuardarJSON(data);
        else GuardarXML(data);
    }

    private void GuardarJSON(SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        GestionarArchivos(savePathJSON, backupPathJSON, json, "JSON");
    }

    private void GuardarXML(SaveData data)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(SaveData));
        using (StringWriter writer = new StringWriter())
        {
            serializer.Serialize(writer, data);
            GestionarArchivos(savePathXML, backupPathXML, writer.ToString(), "XML");
        }
    }

    private void GestionarArchivos(string savePath, string backupPath, string contenido, string tipoFormato)
    {
        try
        {
            if (File.Exists(savePath))
            {
                if (File.Exists(backupPath)) File.Delete(backupPath);
                File.Copy(savePath, backupPath);
                Debug.Log($"Back-up {tipoFormato} creado con éxito.");
            }
            File.WriteAllText(savePath, contenido);
            Debug.Log($"¡Partida guardada en {tipoFormato}! Ruta: {savePath}");
        }
        catch (Exception e)
        {
            Debug.LogError("Error al guardar: " + e.Message);
        }
    }

    public void LoadGame()
    {
        string path = formatoDeGuardado == SaveFormat.JSON ? savePathJSON : savePathXML;
        PlayerInventory playerInv = FindFirstObjectByType<PlayerInventory>();

        if (!File.Exists(path))
        {
            Debug.Log("No hay archivo de guardado. Iniciando partida limpia.");
            if (playerInv != null)
            {
                playerInv.transform.position = new Vector3(0, -2, 0); // Posición inicial
                playerInv.inventory.Clear();
            }
            Chest[] chests = FindObjectsByType<Chest>(FindObjectsSortMode.None);
            foreach (Chest c in chests) c.inventory.Clear();
            return;
        }

        try
        {
            SaveData data = null;

            // Leer JSON o XML según la opción elegida
            if (formatoDeGuardado == SaveFormat.JSON)
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            }
            else
            {
                XmlSerializer serializer = new XmlSerializer(typeof(SaveData));
                using (StringReader reader = new StringReader(File.ReadAllText(path)))
                {
                    data = (SaveData)serializer.Deserialize(reader);
                }
            }

            // 1. Restaurar Jugador y Cofres
            if (playerInv != null && data != null)
            {
                playerInv.transform.position = data.playerPosition;
                playerInv.inventory = data.playerInventory;
            }

            Chest[] chestsInScene = FindObjectsByType<Chest>(FindObjectsSortMode.None);
            foreach (Chest chest in chestsInScene)
            {
                ChestSaveData savedChest = data.chestInventories.Find(c => c.chestId == chest.ChestId);
                if (savedChest != null) chest.inventory = savedChest.inventory;
            }

            // 2. Restaurar Frutas en el mapa
            Fruit[] existingFruits = FindObjectsByType<Fruit>(FindObjectsSortMode.None);
            foreach (Fruit f in existingFruits) Destroy(f.gameObject); // Borramos las frutas no recogidas al reiniciar

            FruitFactory factory = FindFirstObjectByType<FruitFactory>();
            if (factory != null && fruitDatabase != null && fruitDatabase.Length > 0)
            {
                foreach (var savedFruit in data.sceneFruits)
                {
                    // Buscamos la plantilla de la fruta comparando los IDs
                    FruitData matchingData = Array.Find(fruitDatabase, f => savedFruit.fruitId.Contains(f.name));
                    if (matchingData != null)
                    {
                        factory.Create(matchingData, savedFruit.position, Quaternion.identity); // Hacemos spawn
                    }
                }
            }

            // 3. Cálculos de tiempo para imprimir mensajes
            int horasJugadas = (int)(data.totalPlayTime / 3600);
            int minutosJugados = (int)((data.totalPlayTime % 3600) / 60);
            TimeSpan tiempoAusente = DateTime.Now - DateTime.Parse(data.saveDate);

            // Rellenar mensaje y encender el temporizador
            displayMessage = $"Tiempo total de juego: {horasJugadas} horas y {minutosJugados} minutos\n" +
                             $"Hace {tiempoAusente.Days} días, {tiempoAusente.Hours} horas y {tiempoAusente.Minutes} minutos desde tu última sesión de juego";
            messageTimer = 7f; // El texto desaparecerá en 7 segundos
        }
        catch (Exception e)
        {
            Debug.LogError("Error crítico al cargar la partida: " + e.Message);
        }
    }
}

//using UnityEngine;
//using System.IO;
//using System.Collections.Generic;
//using System;

//// --- ESTRUCTURAS DE DATOS PARA SERIALIZAR ---

//[System.Serializable]
//public class FruitSaveData
//{
//    public string fruitId;
//    public Vector3 position;
//}

//[System.Serializable]
//public class ChestSaveData
//{
//    public string chestId;
//    public List<InventorySlot> inventory;
//}

//[System.Serializable]
//public class SaveData
//{
//    public Vector3 playerPosition;
//    public float totalPlayTime;
//    public string saveDate;

//    public List<InventorySlot> playerInventory;
//    public List<ChestSaveData> chestInventories = new List<ChestSaveData>();
//    public List<FruitSaveData> sceneFruits = new List<FruitSaveData>();
//}

//// --- SERVICIO DE GUARDADO ---

//public sealed class SaveGameService : MonoBehaviour
//{
//    private string saveFilePath;
//    private string backupFilePath;

//    private void Awake()
//    {
//        // Definimos las rutas donde se guardarán los archivos JSON en tu ordenador
//        saveFilePath = Application.persistentDataPath + "/savegame.json";
//        backupFilePath = Application.persistentDataPath + "/savegame_backup.json";
//    }

//    public void SaveGame()
//    {
//        Debug.Log("Iniciando el guardado JSON...");

//        // 1. Instanciamos nuestro "molde" de datos
//        SaveData data = new SaveData();

//        // 2. Metadatos de la partida
//        data.totalPlayTime = Time.realtimeSinceStartup; // Tiempo desde que se abrió el juego
//        data.saveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

//        // 3. Datos del Jugador y su inventario (ACTUALIZADO PARA UNITY 6+)
//        PlayerInventory playerInv = FindFirstObjectByType<PlayerInventory>();
//        if (playerInv != null)
//        {
//            data.playerPosition = playerInv.transform.position;
//            data.playerInventory = playerInv.inventory;
//        }

//        // 4. Datos de TODOS los cofres en la escena (ACTUALIZADO PARA UNITY 6+)
//        Chest[] chestsInScene = FindObjectsByType<Chest>(FindObjectsSortMode.None);
//        foreach (Chest chest in chestsInScene)
//        {
//            ChestSaveData chestData = new ChestSaveData
//            {
//                chestId = chest.ChestId,
//                inventory = chest.inventory
//            };
//            data.chestInventories.Add(chestData);
//        }

//        // 5. Datos de todas las frutas tiradas en la escena (ACTUALIZADO PARA UNITY 6+)
//        Fruit[] fruitsInScene = FindObjectsByType<Fruit>(FindObjectsSortMode.None);
//        foreach (Fruit fruit in fruitsInScene)
//        {
//            IPickable pickableData = fruit.GetComponent<IPickable>();
//            if (pickableData != null)
//            {
//                FruitSaveData fruitData = new FruitSaveData
//                {
//                    fruitId = pickableData.Id,
//                    position = fruit.transform.position
//                };
//                data.sceneFruits.Add(fruitData);
//            }
//        }

//        // 6. Convertimos el objeto SaveData a formato JSON (el parámetro 'true' lo hace legible)
//        string json = JsonUtility.ToJson(data, true);

//        // 7. Gestión de archivos y Back-up
//        try
//        {
//            // Si ya existe un archivo de guardado previo, hacemos la copia de seguridad
//            if (File.Exists(saveFilePath))
//            {
//                if (File.Exists(backupFilePath))
//                {
//                    File.Delete(backupFilePath); // Borramos el backup muy antiguo si existe
//                }
//                File.Copy(saveFilePath, backupFilePath); // Hacemos el backup del actual
//                Debug.Log("Back-up creado con éxito.");
//            }

//            // Guardamos (o sobreescribimos) el archivo actual con los nuevos datos
//            File.WriteAllText(saveFilePath, json);
//            Debug.Log($"¡Partida guardada en formato JSON! Ruta: {saveFilePath}");
//        }
//        catch (Exception e)
//        {
//            Debug.LogError("Error al guardar la partida: " + e.Message);
//        }
//    }
//    private void Start()
//    {
//        // El enunciado pide que se cargue automáticamente al abrir la aplicación
//        LoadGame();
//    }

//    public void LoadGame()
//    {
//        Debug.Log("Iniciando la carga de datos...");

//        PlayerInventory playerInv = FindFirstObjectByType<PlayerInventory>();

//        // 1. Comprobamos si es la primera vez que se juega (NO existe el archivo)
//        if (!File.Exists(saveFilePath))
//        {
//            Debug.Log("Primera vez que se abre la aplicación. Iniciando nueva partida.");
//            if (playerInv != null)
//            {
//                playerInv.transform.position = new Vector3(0, -2, 0); // Posición inicial exigida
//                playerInv.inventory.Clear(); // Inventario vacío
//            }

//            // Vaciar cofres
//            Chest[] chests = FindObjectsByType<Chest>(FindObjectsSortMode.None);
//            foreach (Chest c in chests) c.inventory.Clear();

//            return; // Terminamos aquí porque no hay nada que cargar
//        }

//        // 2. Si el archivo existe, leemos el JSON
//        try
//        {
//            string json = File.ReadAllText(saveFilePath);
//            SaveData data = JsonUtility.FromJson<SaveData>(json);

//            // 3. Restaurar al jugador
//            if (playerInv != null)
//            {
//                playerInv.transform.position = data.playerPosition;
//                playerInv.inventory = data.playerInventory;
//            }

//            // 4. Restaurar los cofres
//            Chest[] chestsInScene = FindObjectsByType<Chest>(FindObjectsSortMode.None);
//            foreach (Chest chest in chestsInScene)
//            {
//                // Buscamos si el cofre de la escena está guardado en el JSON
//                ChestSaveData savedChest = data.chestInventories.Find(c => c.chestId == chest.ChestId);
//                if (savedChest != null)
//                {
//                    chest.inventory = savedChest.inventory;
//                }
//            }

//            // 5. Cálculos de tiempo para los mensajes del enunciado
//            float tiempoTotal = data.totalPlayTime;
//            int horasJugadas = (int)(tiempoTotal / 3600);
//            int minutosJugados = (int)((tiempoTotal % 3600) / 60);

//            DateTime ultimaSesion = DateTime.Parse(data.saveDate);
//            TimeSpan tiempoAusente = DateTime.Now - ultimaSesion;

//            Debug.Log($"Tiempo total de juego: {horasJugadas} horas y {minutosJugados} minutos");
//            Debug.Log($"Hace {tiempoAusente.Days} días, {tiempoAusente.Hours} horas y {tiempoAusente.Minutes} minutos desde tu última sesión de juego");

//            // TODO: Falta instanciar las frutas por el mapa e imprimir estos mensajes en la UI de Unity.
//        }
//        catch (Exception e)
//        {
//            Debug.LogError("Error al cargar la partida: " + e.Message);
//        }
//    }
//}

////====================
////==================
////VERSION CON WARNINGS
////====================
////==================
//////using UnityEngine;
//////using System.IO;
//////using System.Collections.Generic;
//////using System;

//////// --- ESTRUCTURAS DE DATOS PARA SERIALIZAR ---

//////[System.Serializable]
//////public class FruitSaveData
//////{
//////    public string fruitId;
//////    public Vector3 position;
//////}

//////[System.Serializable]
//////public class ChestSaveData
//////{
//////    public string chestId;
//////    public List<InventorySlot> inventory;
//////}

//////[System.Serializable]
//////public class SaveData
//////{
//////    public Vector3 playerPosition;
//////    public float totalPlayTime;
//////    public string saveDate;

//////    public List<InventorySlot> playerInventory;
//////    public List<ChestSaveData> chestInventories = new List<ChestSaveData>();
//////    public List<FruitSaveData> sceneFruits = new List<FruitSaveData>();
//////}

//////// --- SERVICIO DE GUARDADO ---

//////public sealed class SaveGameService : MonoBehaviour
//////{
//////    private string saveFilePath;
//////    private string backupFilePath;

//////    private void Awake()
//////    {
//////        // Definimos las rutas donde se guardarán los archivos JSON en tu ordenador
//////        saveFilePath = Application.persistentDataPath + "/savegame.json";
//////        backupFilePath = Application.persistentDataPath + "/savegame_backup.json";
//////    }

//////    public void SaveGame()
//////    {
//////        Debug.Log("Iniciando el guardado JSON...");

//////        // 1. Instanciamos nuestro "molde" de datos
//////        SaveData data = new SaveData();

//////        // 2. Metadatos de la partida
//////        data.totalPlayTime = Time.realtimeSinceStartup; // Tiempo desde que se abrió el juego
//////        data.saveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

//////        // 3. Datos del Jugador y su inventario
//////        PlayerInventory playerInv = FindObjectOfType<PlayerInventory>();
//////        if (playerInv != null)
//////        {
//////            data.playerPosition = playerInv.transform.position;
//////            data.playerInventory = playerInv.inventory;
//////        }

//////        // 4. Datos de TODOS los cofres en la escena (El enunciado exige soportar 2 o más)
//////        Chest[] chestsInScene = FindObjectsOfType<Chest>();
//////        foreach (Chest chest in chestsInScene)
//////        {
//////            ChestSaveData chestData = new ChestSaveData
//////            {
//////                chestId = chest.ChestId,
//////                inventory = chest.inventory
//////            };
//////            data.chestInventories.Add(chestData);
//////        }

//////        // 5. Datos de todas las frutas tiradas en la escena
//////        Fruit[] fruitsInScene = FindObjectsOfType<Fruit>();
//////        foreach (Fruit fruit in fruitsInScene)
//////        {
//////            IPickable pickableData = fruit.GetComponent<IPickable>();
//////            if (pickableData != null)
//////            {
//////                FruitSaveData fruitData = new FruitSaveData
//////                {
//////                    fruitId = pickableData.Id,
//////                    position = fruit.transform.position
//////                };
//////                data.sceneFruits.Add(fruitData);
//////            }
//////        }

//////        // 6. Convertimos el objeto SaveData a formato JSON (el parámetro 'true' lo hace legible)
//////        string json = JsonUtility.ToJson(data, true);

//////        // 7. Gestión de archivos y Back-up
//////        try
//////        {
//////            // Si ya existe un archivo de guardado previo, hacemos la copia de seguridad
//////            if (File.Exists(saveFilePath))
//////            {
//////                if (File.Exists(backupFilePath))
//////                {
//////                    File.Delete(backupFilePath); // Borramos el backup muy antiguo si existe
//////                }
//////                File.Copy(saveFilePath, backupFilePath); // Hacemos el backup del actual
//////                Debug.Log("Back-up creado con éxito.");
//////            }

//////            // Guardamos (o sobreescribimos) el archivo actual con los nuevos datos
//////            File.WriteAllText(saveFilePath, json);
//////            Debug.Log($"¡Partida guardada en formato JSON! Ruta: {saveFilePath}");
//////        }
//////        catch (Exception e)
//////        {
//////            Debug.LogError("Error al guardar la partida: " + e.Message);
//////        }
//////    }
//////}

////using UnityEngine;

////public sealed class SaveGameService : MonoBehaviour
////{
////    public void SaveGame()
////    {
////        // TODO: Implement game saving logic here.


////        Debug.Log("Saving...");
////    }
////}