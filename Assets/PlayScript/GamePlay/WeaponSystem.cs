using MGAssets.AircraftPhysics;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSystem : MonoBehaviour
{
    public AircrafSimpleHUD infoGetter; //속도 높이 가져오는 instance
    
    #region weaponCounts variables
    [SerializeField]private int gunCount;
    [SerializeField] private int missileCount;
    //[SerializeField] private int specialWeaponCount;
    #endregion

    

    #region weapon prefabs
    public GameObject bulletPrefab; // 총알 프리팹
    public GameObject missilePrefab; // 미사일 프리팹
    public GameObject specialWeaponPrefab; // 특수 무기 프리팹

    #endregion

    #region gunVariables
    public float gunFireRate = 0.02f;
    public bool isGunFiring = false;
    private float fireCooldown;

    public Transform gunPoint; // 발사 위치
    #endregion

    #region missileVariables
    public Transform leftMissileTransform;
    public Transform rightMissileTransform;
    #endregion

    public int weaponSelection = 0; // 0 : stdm, 1 : sp

    public Transform playerTransform;

    public Transform currentTargetTransform;

    public TargettingSystem targettingSystem;

    public float aircraftSpeed; //기체 현재 속도

    void Start()
    {
        gunCount = 1600;
        missileCount = 125;
        //specialWeaponCount = 16;

        gunCountUIUpdate();
        stdmCountUIUpdate();
        specialWeaponCountUIUpdate();
    }


    [Space]
    #region STDM instances

    public float missileCoolDownTime;

    public float rightMissileCoolDown;
    public float leftMissileCoolDown;

    #endregion

    #region weaponUI instances
    [SerializeField] RectTransform weaponPointer; // 무기 ui 포인터
    [SerializeField] Text gunCountText; // 기총 잔량
    [SerializeField] Text missileCountText; // 기본미사일 잔량
    [SerializeField] Text specialWeaponCountText; // 특수무기 잔량
    #endregion


    void Update()
    {
        #region Weapon Change and Fire

        if (Input.GetMouseButtonDown(0))
        {
            switch (weaponSelection)
            {
                case 0:
                    FireMissile();
                    break;
                case 1:
                    FireSpecialWeapon();
                    break;
                  
            }
        }

        // 무기 전환 (우클릭)
        if (Input.GetMouseButtonDown(1))
        {
            
            
            if (weaponSelection == 0)
            {
                weaponSelection = 1;
            }
            else if(weaponSelection == 1)
            {
                weaponSelection = 0;
            }
            weaponPointerUpdate(); //무기 포인터 업데이트
            //Beep(); //무기 전환 소리
        }

        #endregion

        #region gunfire updates

        if (Input.GetKey(KeyCode.H)) // H 키를 누르고 있는 동안
        {
            isGunFiring = true; // 총 발사 상태를 true로 설정
        }

        // 총기 연속 발사 처리
        if (isGunFiring && fireCooldown <= 0f)
        {
            FireGun();
            fireCooldown = gunFireRate;
        }

        // 쿨다운 타이머
        if (fireCooldown > 0f)
        {
            fireCooldown -= Time.deltaTime;
        }

        if (Input.GetKeyUp(KeyCode.H))
        {
            isGunFiring = false;
        }

        #endregion

        #region STDM updates

        STDMCoolDown(ref rightMissileCoolDown);
        STDMCoolDown(ref leftMissileCoolDown);

        aircraftSpeed = infoGetter.getSpeed();

        #endregion

        


    }

    void FireGun()
    {
        Debug.Log("gunfireTriggered");
        if (bulletPrefab != null && gunPoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, gunPoint.position, gunPoint.rotation);

            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.bulletSpeed = 1200f; // 원하는 속도로 설정
            }

            gunCount--;
            gunCountUIUpdate(); //잔탄 업데이트
            Debug.Log("Gun fired");
        }
    }


    

    #region stdmCodes
    void FireMissile()
    {
        if (missileCount <= 0)
        {
            return; // 잔탄 0.
        }
        if (leftMissileCoolDown > 0 && rightMissileCoolDown > 0)
        {
            return; // 재장전 안됨.
        }

        Vector3 missilePosition;

        if(missileCount % 2 == 1) // 남은 미사일 수가 홀수
        {
            missilePosition = rightMissileTransform.position;
            rightMissileCoolDown = missileCoolDownTime;
        }
        else // 짝수
        {
            missilePosition = leftMissileTransform.position;
            leftMissileCoolDown = missileCoolDownTime;
        }

        GameObject stdm = Instantiate(missilePrefab, missilePosition, playerTransform.rotation); //미사일 생성
        STDM missileScript = stdm.GetComponent<STDM>();

       
        currentTargetTransform = targettingSystem.currentTarget;
        if(targettingSystem.IsInCone(currentTargetTransform))
        {
            missileScript.Launch(currentTargetTransform, infoGetter.getSpeed() / 10 ); ////////확인!!!!!
        }
        else
        {
            missileScript.Launch(null, infoGetter.getSpeed() / 10 + 20);
        }
        
        missileCount--;
        stdmCountUIUpdate();
    }

    void STDMCoolDown(ref float cooldown)
    {
        if (cooldown > 0)
        {
            cooldown -= Time.deltaTime;
            if (cooldown < 0) cooldown = 0;
        }
        else return;
    }

    #endregion

    void FireSpecialWeapon()
    {
        
    }

    #region weaponUI update funcs
    void weaponPointerUpdate()
    {
        if (weaponPointer != null)
        {
            if(weaponSelection == 0)
            {
                weaponPointer.anchoredPosition = new Vector3(-330, 440, 0);
            }
            else if(weaponSelection == 1)
            {
                weaponPointer.anchoredPosition = new Vector3(-330, 380, 0);
            }
        }
    }

    void gunCountUIUpdate()
    {
        gunCountText.text = gunCount.ToString();
    }

    void stdmCountUIUpdate()
    {
        missileCountText.text = missileCount.ToString();
    }

    void specialWeaponCountUIUpdate()
    {

    }
    #endregion
}
