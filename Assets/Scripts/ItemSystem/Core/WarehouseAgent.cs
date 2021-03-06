﻿using UnityEngine;

[DisallowMultipleComponent]
public class WarehouseAgent : Building
{
    [SerializeField]
    private Warehouse warehouse = new Warehouse(50);
    public Warehouse MWarehouse => warehouse;

    private void Awake()
    {
        onButtonClick.AddListener(delegate
        {
            WarehouseManager.Instance.Init(MWarehouse);
            WarehouseManager.Instance.OpenWindow();
        });
    }

    public override void AskDestroy()
    {
        ConfirmManager.Instance.New(string.Format("{0}{1}\n内的东西不会保留，确定拆除吗？", name, ((Vector2)transform.position).ToString()),
            delegate { BuildingManager.Instance.DestroyBuilding(this); });
    }

    /*protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
        if (collision.CompareTag("Player") && IsBuilt)
        {
            WarehouseManager.Instance.CanStore(this);
        }
    }

    protected override void OnTriggerStay2D(Collider2D collision)
    {
        base.OnTriggerStay2D(collision);
        if (collision.CompareTag("Player") && !WarehouseManager.Instance.IsUIOpen && IsBuilt)
        {
            WarehouseManager.Instance.CanStore(this);
        }
    }

    protected override void OnTriggerExit2D(Collider2D collision)
    {
        base.OnTriggerExit2D(collision);
        if (collision.CompareTag("Player") && WarehouseManager.Instance.MWarehouse == MWarehouse && IsBuilt)
        {
            WarehouseManager.Instance.CannotStore();
        }
    }*/

    private void OnDestroy()
    {
        if (WarehouseManager.Instance)
            if (WarehouseManager.Instance.MWarehouse == MWarehouse && isActiveAndEnabled && IsBuilt)
            {
                WarehouseManager.Instance.CannotStore();
            }
    }

    /*protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);
        if (other.tag == "Player" && isActiveAndEnabled)
        {
            WarehouseManager.Instance.CanStore(this);
        }
    }

    protected override void OnTriggerStay(Collider other)
    {
        base.OnTriggerStay(other);
        if (other.tag == "Player" && !WarehouseManager.Instance.IsUIOpen && isActiveAndEnabled)
        {
            WarehouseManager.Instance.CanStore(this);
        }
    }

    protected override void OnTriggerExit(Collider other)
    {
        base.OnTriggerExit(other);
        if (other.tag == "Player" && WarehouseManager.Instance.MWarehouse == MWarehouse && isActiveAndEnabled)
        {
            WarehouseManager.Instance.CannotStore();
        }
    }*/
}
