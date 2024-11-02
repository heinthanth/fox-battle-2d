using UnityEngine;

public class WeaponAmmoSfxController : MonoBehaviour
{
    public WeaponType weaponType;
    private float _startTime;
    
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
        }
    }

    private float GetFxDuration()
    {
        return this.weaponType switch
        {
            WeaponType.None => 0,
            WeaponType.Grenade => 10f,
            WeaponType.Rpg => 5f,
            _ => 0
        };
    }
}
