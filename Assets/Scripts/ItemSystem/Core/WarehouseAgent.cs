﻿using UnityEngine;

[DisallowMultipleComponent]
public class WarehouseAgent : MonoBehaviour
{
    public string ID
    {
        get
        {
            if (MBuilding) return MBuilding.ID;
            return string.Empty;
        }
    }

    [SerializeField]
    private Warehouse warehouse;
    public Warehouse MWarehouse
    {
        get
        {
            return warehouse;
        }
    }

    public Building MBuilding { get; private set; }

    private void Awake()
    {
        if (!MBuilding) MBuilding = GetComponent<Building>();
        if (MBuilding)
        {
            MBuilding.CustumDestroy(delegate
            {
                ConfirmHandler.Instance.NewConfirm(string.Format("{0}{1}\n内的东西不会保留，确定拆除吗？", MBuilding.name, ((Vector2)MBuilding.transform.position).ToString()),
                    BuildingManager.Instance.ConfirmDestroy,
                    delegate
                    {
                        if (MBuilding.IsBuilt && BuildingManager.Instance.ToDestroy == MBuilding)
                         {
                            BuildingManager.Instance.CannotDestroy();
                         }
                    });
            });
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player" && isActiveAndEnabled)
        {
            WarehouseManager.Instance.CanStore(this);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "Player" && !WarehouseManager.Instance.IsUIOpen && isActiveAndEnabled)
        {
            WarehouseManager.Instance.CanStore(this);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Player" && WarehouseManager.Instance.MWarehouse == MWarehouse && isActiveAndEnabled)
        {
            WarehouseManager.Instance.CannotStore();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && isActiveAndEnabled)
        {
            WarehouseManager.Instance.CanStore(this);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Player" && !WarehouseManager.Instance.IsUIOpen && isActiveAndEnabled)
        {
            WarehouseManager.Instance.CanStore(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player" && WarehouseManager.Instance.MWarehouse == MWarehouse && isActiveAndEnabled)
        {
            WarehouseManager.Instance.CannotStore();
        }
    }
}
