using System;
using UnityEngine;

public class FoxController : MonoBehaviour
{
    public int foxIdx = -1;
    
    private static readonly int FoxSpeed = Animator.StringToHash("FoxSpeed");
    private const float MoveSpeed = 5f;
    private const float JumpForce = 10f;
    private const float MaxHealth = 500f;

    private Rigidbody2D _rb;
    private Animator _animator;
    private float _health = MaxHealth;
    public bool isControllable;
    public bool isDead;
    private float _foxWidth;

    // store value in the variable since when running the game at over 660 FPS
    // and putting logic in Update() cause like the frame is skipped
    private float _moveX;
    private bool _jump;
    private int _jumpCount;

    private GameObject _weapon;
    private GameController _gameController;
    private WeaponController _weaponController;
    private GameObject _healthBar;
    private GameObject[] _healthBars;
    private GameObject _foxSprite;

    private void Awake()
    {
        this._rb = GetComponent<Rigidbody2D>();
        this._animator = GetComponent<Animator>();

        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            throw new Exception("Main camera not found");
        }

        // get child weapon game object's WeaponController component
        this._weapon = transform.Find("FoxWeapon").gameObject;
        if (this._weapon == null)
        {
            throw new Exception("FoxWeapon object not found");
        }

        this._weaponController = this._weapon.GetComponent<WeaponController>();

        var gameControllerGameObject = GameObject.Find("GameController");
        if (gameControllerGameObject == null)
        {
            throw new Exception("GameController object not found");
        }

        this._gameController = gameControllerGameObject.GetComponent<GameController>();
        this._foxSprite = transform.Find("FoxSprite").gameObject;
        this._foxWidth = this._foxSprite.GetComponent<SpriteRenderer>().bounds.size.x;

        this.InitHealthBar();
    }

    
    public void ResetFoxForTurnSwitch()
    {
        this.isControllable = false;
        this._weaponController.SwitchWeapon(WeaponType.None);
        this._moveX = 0;
        this._jump = false;
        this._jumpCount = 0;
        this._animator.SetFloat(FoxSpeed, 0);
    }
    
    public void UpdateHealthBarSpriteForPlayer2()
    {
        var frameSprite = Resources.Load<Sprite>("Frames/60px/Frame_Border_Orange_60px");
        var healthBarFillSprite = Resources.Load<Sprite>("Frames/60px/Frame_Square_Honey_60px");

        var healthBarFill = this._healthBar.transform.Find("HealthBarFill");
        if (healthBarFill == null)
        {
            throw new Exception("HealthBarFill object not found");
        }

        foreach (Transform child in healthBarFill)
        {
            var spriteRenderer = child.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = healthBarFillSprite;
        }

        var healthBarFrame = this._healthBar.transform.Find("HealthBarFrame");
        if (healthBarFrame == null)
        {
            throw new Exception("HealthBarFrame object not found");
        }

        var frameSpriteRenderer = healthBarFrame.GetComponent<SpriteRenderer>();
        frameSpriteRenderer.sprite = frameSprite;
    }

    private void Start()
    {
        this.UpdateHealthBarUI();
    }

    private void InitHealthBar()
    {
        this._healthBar = this.transform.Find("HealthBar").gameObject;
        var healthBarFill = this._healthBar.transform.Find("HealthBarFill");
        if (healthBarFill == null)
        {
            throw new Exception("HealthBarFill object not found");
        }

        this._healthBars = new GameObject[10];
        foreach (Transform child in healthBarFill)
        {
            var cleaned = child.name.Replace("HealthBarFill (", "");
            var index = cleaned.Substring(0, cleaned.Length - 1);
            this._healthBars[int.Parse(index)] = child.gameObject;
            this._healthBars[int.Parse(index)].SetActive(false);
        }
    }

    private void Update()
    {
        if (!this.isControllable) return;
        
        // move
        this._moveX = Input.GetAxis("Horizontal");
        // jump
        if (Input.GetButtonDown("Jump")) this._jump = true;
        // switch weapon
        this.HandleWeaponSwitch();
    }

    private void FixedUpdate()
    {
        this.MoveFox(this._moveX);
        if (this._jump && this._jumpCount < 2) this.JumpFox();

        // check if the fox goes outside of camera
        var minX = this._gameController.worldMinX + (this._foxWidth / 2);
        var maxX = this._gameController.worldMaxX - (this._foxWidth / 2);

        var rangeX = Mathf.Clamp(transform.position.x, minX, maxX);
        this._rb.position = new Vector2(rangeX, this._rb.position.y);
    }

    private void HandleWeaponSwitch()
    {
        var weaponType = this._weaponController.GetWeaponType();

        if (Input.GetKeyDown(KeyCode.W) && weaponType != WeaponType.Grenade)
        {
            this._weaponController.SwitchWeapon(WeaponType.Grenade);
        }
        else if (Input.GetKeyDown(KeyCode.E) && weaponType != WeaponType.Rpg)
        {
            this._weaponController.SwitchWeapon(WeaponType.Rpg);
        }
        else if (Input.GetKeyDown(KeyCode.R) && weaponType != WeaponType.None)
        {
            this._weaponController.SwitchWeapon(WeaponType.None);
        }
    }

    public void ApplyDamage(float damage)
    {
        this._health -= damage;
        if (this._health <= 0)
        {
            // make inactive
            this.gameObject.SetActive(false);
            this.isDead = true;
        }

        this.UpdateHealthBarUI();
    }

    private void MoveFox(float moveX)
    {
        // flip the fox sprite based on the direction
        if (moveX != 0)
        {
            var currentFoxDirection = moveX > 0 ? 1 : -1;
            this._foxSprite.transform.localScale = new Vector3(currentFoxDirection, 1, 1);
        }
        
        // move the fox horizontally
        var targetSpeed = moveX * MoveSpeed;
        var speedDiff = targetSpeed - _rb.velocity.x;
        var movement = speedDiff * 10f;
        this._rb.AddForce(new Vector2(movement, 0), ForceMode2D.Force);

        var foxSpeed = moveX == 0 ? (this._jumpCount == 0 ? 0 : 2) : (this._jumpCount == 0 ? 1 : 2);
        this._animator.SetFloat(FoxSpeed, foxSpeed);
    }

    private void JumpFox()
    {
        // only apply half force on double jump
        var force = JumpForce * (this._jumpCount > 0 ? 0.25f : 1f);
        this._rb.AddForce(new Vector2(0, force), ForceMode2D.Impulse);

        this._animator.SetFloat(FoxSpeed, 2);
        this._jumpCount++;
        this._jump = false;
    }

    // The Fox has Box2D collider which acts as ground sensor
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Ground")) return;
        this._jumpCount = 0;
        this._animator.SetFloat(FoxSpeed, 0);
    }

    private void UpdateHealthBarUI()
    {
        var fillCount = (int)(this._health / (MaxHealth / this._healthBars.Length));
        for (var i = 0; i < this._healthBars.Length; i++)
        {
            this._healthBars[i].SetActive(i < fillCount);
        }
    }
}