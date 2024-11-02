using UnityEngine;

public class WeaponAmmoFxController : MonoBehaviour
{
    public WeaponType weaponType;
    private float _startTime;
    private GameController _gameController;

    private void Awake()
    {
        this._gameController = GameObject.Find("GameController").GetComponent<GameController>();
    }

    // Start is called before the first frame update
    private void Start()
    {
        this._startTime = Time.time;
    }

    // Update is called once per frame
    // ReSharper disable Unity.PerformanceAnalysis
    private void Update()
    {
        var delayTime = this.GetFxDuration();
        var time = Time.time - this._startTime;

        if (time >= delayTime)
        {
            Destroy(gameObject);
            // switch turn
            this._gameController.SwitchTurn();
        }
    }

    private float GetFxDuration()
    {
        return this.weaponType switch
        {
            WeaponType.None => 0,
            WeaponType.Grenade => 1f,
            WeaponType.Rpg => 1f,
            _ => 0
        };
    }

    private void ApplyDamageToFox(GameObject fox)
    {
        var foxController = fox.GetComponent<FoxController>();
        var damage = this.GetDamage();
        foxController.ApplyDamage(damage);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Player2"))
        {
            this.ApplyDamageToFox(other.gameObject);
        }
    }

    private float GetDamage()
    {
        return this.weaponType switch
        {
            WeaponType.Grenade => 50,
            WeaponType.Rpg => 80,
            _ => 0
        };
    }
}