using System.Collections.Generic;
using UnityEngine;

// Definimos la estructura de cada hueco del inventario
[System.Serializable]
public class InventorySlot
{
    public string fruitId;
    public string displayName;
    public int amount;
    public int maxStack;

    // Constructor vacío necesario para la serialización en XML
    public InventorySlot() { }

    // Constructor
    public InventorySlot(string id, string name, int maxStackAmount)
    {
        this.fruitId = id;
        this.displayName = name;
        this.amount = 1;
        this.maxStack = maxStackAmount;
    }
}

[DisallowMultipleComponent]
public sealed class PlayerInventory : MonoBehaviour, IStorable
{
    [Header("Configuración del Inventario")]
    [Tooltip("Límite total de huecos (slots) en el inventario")]
    public int maxSlots = 10;

    [Tooltip("Cantidad máxima de frutas por cada grupo (stack) por defecto")]
    public int defaultMaxStack = 5;

    [Header("Datos dinámicos")]
    public List<InventorySlot> inventory = new List<InventorySlot>();

    public void Store(IPickable item)
    {
        // 1. Comprobamos si ya tenemos un stack de esta fruta que NO esté lleno
        foreach (var slot in inventory)
        {
            if (slot.fruitId == item.Id && slot.amount < slot.maxStack)
            {
                slot.amount++;
                Debug.Log($"[Inventario] Apilado: {item.DisplayName}. Cantidad en el stack: {slot.amount}/{slot.maxStack}");
                return;
            }
        }

        // 2. Si no hay stack disponible (o están todos llenos), creamos un nuevo hueco si hay espacio total
        if (inventory.Count < maxSlots)
        {
            // Nota: Si has añadido un "maxStack" directamente dentro de FruitData.cs, 
            // aquí podrías intentar extraerlo casteando el item. Por ahora usamos un valor por defecto.
            InventorySlot newSlot = new InventorySlot(item.Id, item.DisplayName, defaultMaxStack);
            inventory.Add(newSlot);
            Debug.Log($"[Inventario] Nuevo slot ocupado por: {item.DisplayName}. Huecos usados: {inventory.Count}/{maxSlots}");
            return;
        }

        // 3. Si llegamos aquí, el inventario está totalmente lleno
        Debug.LogWarning($"[Inventario] ¡Inventario lleno! No hay espacio para {item.DisplayName}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        if (!other.TryGetComponent<IPickable>(out var pickable))
            return;

        // El recolectable (IPickable) ejecuta su propia lógica y luego llama a nuestro método Store()
        pickable.Pick(this);
    }
}


//using UnityEngine;

//[DisallowMultipleComponent]
//public sealed class PlayerInventory : MonoBehaviour, IStorable
//{
//    // TODO: store items, stack amounts, etc.


//    public void Store(IPickable item)
//    {
//        // TODO: implement inventory rules.


//        // For now, just log.
//        Debug.Log($"Picked: {item.DisplayName} ({item.Id})");
//    }


//    private void OnTriggerEnter2D(Collider2D other)
//    {
//        if (other == null) return;

//        if (!other.TryGetComponent<IPickable>(out var pickable))
//            return;

//        // The pickable decides what happens on pick.
//        pickable.Pick(this);
//    }
//}