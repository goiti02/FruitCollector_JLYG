using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D), typeof(Animator))]
public sealed class Chest : MonoBehaviour, IInteractable
{
    public static readonly int ANIMATOR_OPENED_HASH = Animator.StringToHash("Opened");

    [Header("Identificación")]
    [SerializeField] private string chestId = "chest_01";

    [Header("Configuración del Inventario")]
    [Tooltip("Límite total de huecos en el cofre")]
    public int maxSlots = 10;
    public List<InventorySlot> inventory = new List<InventorySlot>();

    private EInteractionState InteractionState;
    private Collider2D triggerCollider;
    private Animator animator;

    public string ChestId => chestId;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;
        animator = GetComponent<Animator>();
    }

    public void Interact(IInteractor interactor)
    {
        if (interactor == null) return;

        Debug.Log($"El cofre '{chestId}' ha interactuado con {interactor.Transform.name}");

        // Si está cerrado, lo abrimos y hacemos el intercambio. Si está abierto, lo cerramos.
        if (!animator.GetBool(ANIMATOR_OPENED_HASH))
        {
            Open(interactor);
        }
        else
        {
            Close();
        }
    }

    private void Open(IInteractor interactor)
    {
        InteractionState = EInteractionState.INTERACTING;
        animator.SetBool(ANIMATOR_OPENED_HASH, true);

        // Buscar el inventario del jugador para hacer el intercambio
        PlayerInventory playerInv = interactor.Transform.GetComponent<PlayerInventory>();
        if (playerInv != null)
        {
            IntercambiarFrutas(playerInv);
        }
    }

    private void Close()
    {
        InteractionState = EInteractionState.FINISHED;
        animator.SetBool(ANIMATOR_OPENED_HASH, false);
    }

    // --- ESTE ES EL MÉTODO QUE FALTABA PARA CUMPLIR CON LA INTERFAZ ---
    public EInteractionState GetInteractionState()
    {
        return InteractionState;
    }

    private void IntercambiarFrutas(PlayerInventory playerInv)
    {
        Debug.Log("Abriendo menú de intercambio (Lógica interna ejecutada)...");

        // TODO: Aquí debes programar cómo quieres que sea el intercambio real.
        // Como ejemplo muy básico de traspaso automático del jugador al cofre:
        /*
        foreach(var slot in playerInv.inventory)
        {
             // Añadirías 'slot' a este cofre de manera similar a AddFruit
        }
        playerInv.inventory.Clear(); // Vaciar jugador
        */
    }
}

//using System.Collections.Generic;
//using UnityEngine;

//[DisallowMultipleComponent]
//[RequireComponent(typeof(Collider2D), typeof(Animator))]
//public sealed class Chest : MonoBehaviour, IInteractable
//{
//    public static readonly int ANIMATOR_OPENED_HASH = Animator.StringToHash("Opened");

//    [SerializeField] private string chestId = "chest_01";

//    [Header("Configuración del Inventario")]
//    [Tooltip("Límite total de huecos (slots) en el cofre")]
//    public int maxSlots = 15;
//    [Tooltip("Cantidad máxima de frutas por cada grupo (stack) por defecto")]
//    public int defaultMaxStack = 5;

//    [Header("Datos dinámicos")]
//    public List<InventorySlot> inventory = new List<InventorySlot>();

//    private EInteractionState InteractionState;
//    private Collider2D triggerCollider;
//    private Animator animator;

//    public string ChestId => chestId;

//    private void Awake()
//    {
//        triggerCollider = GetComponent<Collider2D>();
//        triggerCollider.isTrigger = true;

//        animator = GetComponent<Animator>();
//    }

//    public void Interact(IInteractor interactor)
//    {
//        if (interactor == null) return;

//        Debug.Log($"Chest '{chestId}' interacted by {interactor.Transform.name}");

//        if (!animator.GetBool(ANIMATOR_OPENED_HASH)) Open(interactor);
//        else Close();
//    }

//    // Modificamos Open para recibir al interactor y acceder a su inventario
//    private void Open(IInteractor interactor)
//    {
//        InteractionState = EInteractionState.INTERACTING;
//        animator.SetBool(ANIMATOR_OPENED_HASH, true);

//        // 1. Obtenemos el inventario del jugador
//        PlayerInventory playerInv = interactor.Transform.GetComponent<PlayerInventory>();

//        if (playerInv != null)
//        {
//            // TODO: Aquí deberás llamar a tu UIManager o GameManager para mostrar la UI visual
//            // Ejemplo conceptual: UIManager.Instance.ShowChestUI(playerInv, this);
//            Debug.Log($"[Cofre] Abriendo menú visual. Jugador tiene {playerInv.inventory.Count} tipos de objetos.");
//        }
//    }

//    private void Close()
//    {
//        InteractionState = EInteractionState.FINISHED;
//        animator.SetBool(ANIMATOR_OPENED_HASH, false);

//        // TODO: Aquí deberás ocultar la UI visual
//        // Ejemplo conceptual: UIManager.Instance.HideChestUI();
//        Debug.Log("[Cofre] Cerrando menú visual.");
//    }

//    public EInteractionState GetInteractionState()
//    {
//        return InteractionState;
//    }

//    // --- MÉTODOS DE TRANSFERENCIA DE DATOS ---

//    // Método para meter frutas al cofre
//    public bool AddFruit(string id, string name, int amount)
//    {
//        foreach (var slot in inventory)
//        {
//            if (slot.fruitId == id && slot.amount + amount <= slot.maxStack)
//            {
//                slot.amount += amount;
//                return true;
//            }
//        }

//        if (inventory.Count < maxSlots)
//        {
//            InventorySlot newSlot = new InventorySlot(id, name, defaultMaxStack);
//            newSlot.amount = amount; // Sobrescribimos el 1 inicial del constructor
//            inventory.Add(newSlot);
//            return true;
//        }

//        Debug.LogWarning("[Cofre] ¡Cofre lleno!");
//        return false;
//    }

//    // Método para sacar frutas del cofre
//    public bool RemoveFruit(string id, int amountToRemove)
//    {
//        for (int i = 0; i < inventory.Count; i++)
//        {
//            if (inventory[i].fruitId == id)
//            {
//                if (inventory[i].amount >= amountToRemove)
//                {
//                    inventory[i].amount -= amountToRemove;

//                    // Si el hueco se queda vacío, lo borramos de la lista
//                    if (inventory[i].amount <= 0)
//                    {
//                        inventory.RemoveAt(i);
//                    }
//                    return true;
//                }
//            }
//        }
//        return false;
//    }
//}

////using UnityEngine;

////[DisallowMultipleComponent]
////[RequireComponent(typeof(Collider2D), typeof(Animator))]
////public sealed class Chest : MonoBehaviour, IInteractable
////{
////    public static readonly int ANIMATOR_OPENED_HASH = Animator.StringToHash("Opened");

////    [SerializeField] private string chestId = "chest_01";

////    private EInteractionState InteractionState;
////    private Collider2D triggerCollider;
////    private Animator animator;

////    public string ChestId => chestId;


////    private void Awake()
////    {
////        triggerCollider = GetComponent<Collider2D>();
////        triggerCollider.isTrigger = true;

////        animator = GetComponent<Animator>();
////    }


////    public void Interact(IInteractor interactor)
////    {
////        if (interactor == null) return;

////        Debug.Log($"Chest '{chestId}' interacted by {interactor.Transform.name}");

////        if (!animator.GetBool(ANIMATOR_OPENED_HASH)) Open();
////        else Close();
////    }


////    private void Open()
////    {
////        InteractionState = EInteractionState.INTERACTING;
////        animator.SetBool(ANIMATOR_OPENED_HASH, true);

////        // TODO: Show and apply store logic.


////    }


////    private void Close()
////    {
////        InteractionState = EInteractionState.FINISHED;
////        animator.SetBool(ANIMATOR_OPENED_HASH, false);
////    }


////    public EInteractionState GetInteractionState()
////    {
////        return InteractionState;
////    }
////}