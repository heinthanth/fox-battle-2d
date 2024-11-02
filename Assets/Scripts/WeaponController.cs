using System;
using JetBrains.Annotations;
using UnityEngine;

public enum WeaponType
{
    None,
    Grenade,
    Rpg,
}

public class WeaponController : MonoBehaviour
{
    private const float WeaponForceMultiplier = 0.25f;
    private const float MaxWeaponChargePower = 100f; // Maximum weapon charge level
    private const float WeaponChargeRate = 40f; // Rate at which the weapon charges
    private const float WeaponMaxChargeHoldTime = 3f; // Time before charge resets
    private Weapon _weapon;
    public WeaponType weaponType;
    private GameObject _weaponChargeBar;
    private GameObject[] _weaponChargeBars;
    private Camera _camera;
    private GameObject _ammoSpawnPoint;
    private GameController _gameController;
    private FoxController _foxController;
    private SpriteRenderer _weaponSpriteRenderer;
    
    // key press flags ...
    private bool _acceptKeyPress = true;
    private float _weaponChargePower; // Current weapon charge level
    private bool _isWeaponCharging; // Track charging state
    private float _weaponChargeHoldTime; // Track how long charge is held

    public WeaponType GetWeaponType()
    {
        return this.weaponType;
    }

    public void SwitchWeapon(WeaponType type)
    {
        this.weaponType = type;
        this._weapon = new Weapon(this.weaponType);

        // update weapon sprite
        var spritePath = this._weapon.GetSpritePath();
        if (spritePath != null)
        {
            this._weaponSpriteRenderer.sprite = Resources.Load<Sprite>(spritePath);
        }

        // update weapon scale
        var weaponScale = this._weapon.GetWeaponScale();
        transform.localScale = new Vector3(weaponScale, weaponScale, 1);
        if (weaponType == WeaponType.None)
        {
            this.RestoreMouseCursor();
        }
        else
        {
            this.SetupMouseCursor();
        }
    }

    private void ToggleWeaponChargeBar(bool isActive)
    {
        // set children of weapon charge bar to active
        foreach (Transform child in this._weaponChargeBar.transform)
        {
            child.gameObject.SetActive(isActive);
        }
    }

    private void Awake()
    {
        this.SwitchWeapon(WeaponType.None);
        this._weaponChargeBar = this.transform.root.Find("WeaponChargeBar").gameObject;
        this._ammoSpawnPoint = this.transform.root.Find("WeaponAmmoSpawnPoint").gameObject;
        var weaponChargeBarFill = this._weaponChargeBar.transform.Find("WeaponChargeBarFill");
        if (weaponChargeBarFill == null)
        {
            throw new Exception("WeaponChargeBarFill object not found");
        }

        this._weaponChargeBars = new GameObject[10];
        foreach (Transform child in weaponChargeBarFill)
        {
            var cleaned = child.name.Replace("WeaponChargeBarFill (", "");
            var index = cleaned.Substring(0, cleaned.Length - 1);
            this._weaponChargeBars[int.Parse(index)] = child.gameObject;
            this._weaponChargeBars[int.Parse(index)].SetActive(false);
        }

        this._camera = Camera.main;
        if (this._camera == null)
        {
            throw new Exception("Main camera not found");
        }

        this._foxController = this.transform.root.GetComponentInChildren<FoxController>();
        if (this._foxController == null)
        {
            throw new Exception("FoxController object not found");
        }
        
        this._weaponSpriteRenderer = this.GetComponent<SpriteRenderer>();
        
        var gameControllerGameObject = GameObject.Find("GameController");
        if (gameControllerGameObject == null)
        {
            throw new Exception("GameController object not found");
        }
        
        this._gameController = gameControllerGameObject.GetComponent<GameController>();
    }

    private void Update()
    {
        if (this._foxController.isControllable && this.weaponType != WeaponType.None && Input.GetKeyUp(KeyCode.F))
        {
            this.HandleWeaponFire();
            this._acceptKeyPress = true;
        }
    }

    private void FixedUpdate()
    {
        if (this.weaponType != WeaponType.None && Input.GetKey(KeyCode.F) && this._acceptKeyPress)
        {
            this._isWeaponCharging = true;
            this.HandleWeaponCharge();
        }

        // update weapon Z rotation
        this.UpdateWeaponZRotation();

        // update weapon charge bar
        this.UpdateWeaponChargeBarUI();
    }

    private float GetMouseAngle()
    {
        var mousePosition = this._camera.ScreenToWorldPoint(Input.mousePosition);
        var direction = mousePosition - transform.position;
        var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return angle;
    }

    private void UpdateWeaponZRotation()
    {
        transform.rotation = Quaternion.Euler(0, 0, this.GetMouseAngle());
    }

    private void HandleWeaponCharge()
    {
        if (this._weaponChargePower < MaxWeaponChargePower)
        {
            this._weaponChargePower += WeaponChargeRate * Time.deltaTime;
            this._weaponChargePower = Mathf.Clamp(this._weaponChargePower, 0, MaxWeaponChargePower);
        }

        // can't compare floats directly, use Mathf.Approximately
        var isMaxCharge = Mathf.Approximately(this._weaponChargePower, MaxWeaponChargePower);

        // start the timer once the weapon is fully charged
        if (isMaxCharge && this._weaponChargeHoldTime == 0f) this._weaponChargeHoldTime += Time.time;

        // Reset charge if hold time exceeds limit
        var chargeHoldDuration = Time.time - this._weaponChargeHoldTime;
        if (isMaxCharge && chargeHoldDuration >= WeaponMaxChargeHoldTime)
        {
            this._acceptKeyPress = false;
            this.ResetWeaponCharge();
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void HandleWeaponFire()
    {
        if (this._weaponChargePower > 0)
        {
            var weaponAmmon = Instantiate(this._weapon.WeaponAmmo, this._ammoSpawnPoint.transform.position,
                this._ammoSpawnPoint.transform.rotation);
            var weaponAmmoRb = weaponAmmon.GetComponent<Rigidbody2D>();
            var launchAngle = this.GetMouseAngle();
            var launchDirection = new Vector2(Mathf.Cos(Mathf.Deg2Rad * launchAngle),
                Mathf.Sin(Mathf.Deg2Rad * launchAngle));

            // Apply the launch force to the weapon ammo
            // shotgun force is not affected by charge power
            var launchForce = this._weaponChargePower * WeaponForceMultiplier;
            weaponAmmoRb.AddForce(launchDirection * launchForce, ForceMode2D.Impulse);
            
            // make the fox uncontrollable
            this._foxController.isControllable = false;
            this._gameController.weaponFired = true;
        }

        this.ResetWeaponCharge();
    }

    private void ResetWeaponCharge()
    {
        this._isWeaponCharging = false;
        this._weaponChargePower = 0f;
        this._weaponChargeHoldTime = 0f;
    }

    private void UpdateWeaponChargeBarUI()
    {
        var fillCount = (int)(this._weaponChargePower / (MaxWeaponChargePower / this._weaponChargeBars.Length));
        if (fillCount > 0)
        {
            this.ToggleWeaponChargeBar(true);
        }
        else if (fillCount == 0 && this._isWeaponCharging)
        {
            this.ToggleWeaponChargeBar(true);
        }
        else
        {
            this.ToggleWeaponChargeBar(false);
            return;
        }

        for (var i = 0; i < this._weaponChargeBars.Length; i++)
        {
            this._weaponChargeBars[i].SetActive(i < fillCount);
        }
    }

    private void SetupMouseCursor()
    {
        var cursorTexture =
            Resources.Load<Texture2D>("Joysticks/Joystick_Fire_Jelly");
        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
    }

    private void RestoreMouseCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}

internal class Weapon
{
    private readonly WeaponType _type;
    public readonly GameObject WeaponAmmo;

    public Weapon(WeaponType type)
    {
        this._type = type;
        this.WeaponAmmo = type switch
        {
            WeaponType.Grenade => Resources.Load<GameObject>("Prefabs/WeaponAmmo/Grenade"),
            WeaponType.Rpg => Resources.Load<GameObject>("Prefabs/WeaponAmmo/RpgRocket"),
            _ => null
        };
    }

    [CanBeNull]
    public string GetSpritePath()
    {
        return this._type switch
        {
            WeaponType.Grenade => "Weapons/grenade",
            WeaponType.Rpg => "Weapons/RPG",
            _ => null
        };
    }

    public float GetWeaponScale()
    {
        return this._type switch
        {
            WeaponType.Grenade => 1,
            WeaponType.Rpg => 0.85f,
            _ => 0
        };
    }
}