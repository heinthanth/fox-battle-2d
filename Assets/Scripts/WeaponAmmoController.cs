using System;
using UnityEngine;

public class WeaponAmmoController : MonoBehaviour
{
    public WeaponType weaponType;
    private float _startTime;
    private bool _isUsed;
    private GameObject _fxPrefab;
    private GameObject _sfxPrefab;
    private GameObject _fxSpawnPoint;
    private bool _shouldSpawnFx = true;
    private bool _shouldSpawnSfx = true;
    private GameController _gameController;

    // trick to avoid explosion on hand :P
    private bool _firstCollision = true;

    private void Awake()
    {
        this._fxSpawnPoint = this.transform.Find("WeaponFxSpawnPoint").gameObject;
        var gameControllerGameObject = GameObject.Find("GameController");
        if (gameControllerGameObject == null)
        {
            throw new Exception("GameController object not found");
        }

        this._gameController = gameControllerGameObject.GetComponent<GameController>();
    }

    private void Start()
    {
        this._fxPrefab = this.weaponType switch
        {
            WeaponType.None => null,
            WeaponType.Grenade => Resources.Load<GameObject>("Prefabs/WeaponAmmo/Explosion"),
            WeaponType.Rpg => Resources.Load<GameObject>("Prefabs/WeaponAmmo/Explosion"),
            _ => throw new ArgumentOutOfRangeException()
        };
        this._sfxPrefab = this.weaponType switch
        {
            WeaponType.None => null,
            WeaponType.Grenade => Resources.Load<GameObject>("Prefabs/WeaponAmmo/GrenadeSound"),
            WeaponType.Rpg => Resources.Load<GameObject>("Prefabs/WeaponAmmo/RpgSound"),
            _ => throw new ArgumentOutOfRangeException()
        };

        // start the timer for the grenade
        if (this.weaponType == WeaponType.Grenade)
        {
            this._isUsed = true;
            this._startTime = Time.time;
        }
    }

    private void FixedUpdate()
    {
        // if weapon is outside the screen, destroy it
        if (this.transform.position.x < this._gameController.worldMinX ||
            this.transform.position.x > this._gameController.worldMaxX)
        {
            this._isUsed = true;
        }
    }

    private void Update()
    {
        var delayTime = this.GetDelayTime();
        var time = Time.time - this._startTime;
        if (this._isUsed && this._shouldSpawnSfx)
        {
            Instantiate(this._sfxPrefab);
            this._shouldSpawnSfx = false;
        }

        if (this._isUsed && time >= delayTime)
        {
            if (this._shouldSpawnFx && this._fxSpawnPoint)
            {
                Instantiate(this._fxPrefab, this._fxSpawnPoint.transform.position, Quaternion.Euler(0, 0, 90));
                this._shouldSpawnFx = false;
            }

            Destroy(gameObject);
        }
    }

    private float GetDelayTime()
    {
        switch (this.weaponType)
        {
            case WeaponType.Grenade:
                return 5f;
            case WeaponType.Rpg:
                return 0f;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private float GetHitDamage()
    {
        switch (this.weaponType)
        {
            case WeaponType.Grenade:
                return 20;
            case WeaponType.Rpg:
                return 30;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ApplyDamageToFox(GameObject fox)
    {
        var foxController = fox.GetComponent<FoxController>();
        var damage = this.GetHitDamage();
        foxController.ApplyDamage(damage);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (this._firstCollision)
        {
            this._firstCollision = false;
            return;
        }

        this._isUsed = true;
        if (other.CompareTag("Player") || other.CompareTag("Player2"))
        {
            this.ApplyDamageToFox(other.gameObject);
        }
    }
}