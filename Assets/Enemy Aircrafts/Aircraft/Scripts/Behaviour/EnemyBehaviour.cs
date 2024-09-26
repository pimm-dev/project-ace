using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyAI : MonoBehaviour
{
    
    [SerializeField] Transform player; // 플레이어의 Transform
    [SerializeField] TargettingSystem targetingSystem;

    [Header("movingAI instances")]
    #region
    [SerializeField] float maxSpeed;
    [SerializeField] float minSpeed;
    [SerializeField] float defaultSpeed;

    float speed;

    [SerializeField] float speedLerpAmount;
    [SerializeField] float turningForce;
    [SerializeField] float turningTime;

    [SerializeField] List<Transform> initialWaypoints;
    Queue<Transform> waypointQueue;

    Vector3 currentWaypoint;

    float prevWaypointDistance;
    float waypointDistance;
    bool isComingClose;

    float prevRotY;
    float currRotY;
    float rotateAmount;
    float zRotateValue;

    // Z Rotate Values
    [SerializeField]
    float zRotateMaxThreshold = 0.5f;
    [SerializeField]
    float zRotateAmount = 90;

    [SerializeField]
    float newWaypointDistance;
    [SerializeField]
    float waypointMinHeight;
    [SerializeField]
    float waypointMaxHeight;

    [SerializeField]
    GameObject waypointObject;

    void ChangeWaypoint()
    {
        if (waypointQueue.Count == 0)
        {
            CreateWaypoint();
        }
        else
        {
            currentWaypoint = waypointQueue.Dequeue().position;
        }

        waypointDistance = Vector3.Distance(transform.position, currentWaypoint);
        prevWaypointDistance = waypointDistance;
        isComingClose = false;
    }

    void CreateWaypoint()
    {
        float distance = Random.Range(newWaypointDistance * 0.7f, newWaypointDistance);
        float height = Random.Range(waypointMinHeight, waypointMaxHeight);
        float angle = Random.Range(0, 360);
        Vector3 directionVector = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad));
        Vector3 waypointPosition = transform.position + directionVector * distance;

        RaycastHit hit;
        Physics.Raycast(waypointPosition, Vector3.down, out hit);

        if (hit.distance != 0)
        {
            waypointPosition.y += height - hit.distance;
        }
        // New waypoint is below ground
        else
        {
            Physics.Raycast(waypointPosition, Vector3.up, out hit);
            waypointPosition.y += height + hit.distance;
        }

        Instantiate(waypointObject, waypointPosition, Quaternion.identity);

        currentWaypoint = waypointPosition;
    }

    void CheckWaypoint()
    {
        if (currentWaypoint == null) return;
        waypointDistance = Vector3.Distance(transform.position, currentWaypoint);

        if (waypointDistance >= prevWaypointDistance) // Aircraft is going farther from the waypoint
        {
            if (isComingClose == true)
            {
                ChangeWaypoint();
            }
        }
        else
        {
            isComingClose = true;
        }

        prevWaypointDistance = waypointDistance;
    }

    void Rotate()
    {
        if (currentWaypoint == null)
            return;

        Vector3 targetDir = currentWaypoint - transform.position;
        Quaternion lookRotation = Quaternion.LookRotation(targetDir);

        float delta = Quaternion.Angle(transform.rotation, lookRotation);
        if (delta > 0f)
        {
            float lerpAmount = Mathf.SmoothDampAngle(delta, 0.0f, ref rotateAmount, turningTime);
            lerpAmount = 1.0f - (lerpAmount / delta);

            Vector3 eulerAngle = lookRotation.eulerAngles;
            eulerAngle.z += zRotateValue * zRotateAmount;
            lookRotation = Quaternion.Euler(eulerAngle);

            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, lerpAmount);
        }
    }

    void ZAxisRotate()
    {
        currRotY = transform.eulerAngles.y;
        float diff = prevRotY - currRotY;

        if (diff > 180) diff -= 360;
        if (diff < -180) diff += 360;

        prevRotY = transform.eulerAngles.y;
        zRotateValue = Mathf.Lerp(zRotateValue, Mathf.Clamp(diff / zRotateMaxThreshold, -1, 1), turningForce * Time.deltaTime);
    }

    void Move()
    {
        transform.Translate(new Vector3(0, 0, speed) * Time.deltaTime);
    }




    #endregion


    [Header("EnemyInfo")]
    #region EnemyInfo
    public string aircraftName;
    [SerializeField] int aircraftHP = 100;
    [SerializeField] public int aircraftScore = 240;
    #endregion
    
    [Space]

    [Header("UI References")]
    #region reactive UI references and variables
    [SerializeField] Camera mainCamera;
    [SerializeField] RectTransform lockOnUIRectTransform;
    [SerializeField] Image lockOnUIImage;

    [SerializeField] Color lockedOnColor = Color.red;
    [SerializeField] Color normalColor = Color.green;
    [SerializeField] Color transparentColor = new Color(1f,0f,0f,0f); // 타게팅 상태일 때의 색상

    [SerializeField] GameObject aircraftInfoUIobject; // 적기 정보 UI 세트
    [SerializeField] Text distanceText;
    [SerializeField] Text aircraftNameText;

    [SerializeField] float minDistance = 10f;
    [SerializeField] float maxDistance = 650f;

    [SerializeField] Vector2 minSize = new Vector2(10f, 10f); // UI의 최소 크기
    [SerializeField] Vector2 maxSize = new Vector2(100f, 100f); // UI의 최대 크기

    [SerializeField] bool isFlickering = false;
    [SerializeField] bool isMinimapFlickering = false;

    [SerializeField] GameObject lockOnSquare;
    [SerializeField] Color sibal;

    [SerializeField] TagController tagController;
    [SerializeField] GameManagement gameManagement;

    [SerializeField] SpriteRenderer minimapSprite;

    #endregion

    [Header("enemyState")]
    #region states
    public bool isTargeted = false; // 타겟으로 지정되었는지 여부
    public bool isLockedOn = false;
    #endregion


    [Header("Sound Sources")]
    #region Audio Sources
    [SerializeField] AudioSource lockOnSound;
    #endregion

    void Start()
    { 
        mainCamera = Camera.main;   

        distanceText.color = normalColor;
        aircraftNameText.text = aircraftName;
        aircraftNameText.color = normalColor;

        speed = defaultSpeed;
        turningTime = 1 / turningForce;

        waypointQueue = new Queue<Transform>();
        foreach (Transform t in initialWaypoints)
        {
            waypointQueue.Enqueue(t);
        }
        ChangeWaypoint();

    }

    void Update() //자체 비행 로직.
    {

        #region UI updates

        if(lockOnUIImage != null)
        {
            // 적 기체의 월드 좌표를 화면 좌표로 변환
            Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position);

            // 적과 플레이어 간의 거리 계산
            float distanceToTarget = Vector3.Distance(mainCamera.transform.position, transform.position);

            if (screenPos.z > 0 && screenPos.x > 0 && screenPos.x < Screen.width && screenPos.y > 0 && screenPos.y < Screen.height)
            {
                if (distanceToTarget <= maxDistance)
                {
                    lockOnUIRectTransform.gameObject.SetActive(true);

                    // 거리 기반으로 크기 조정
                    float t = Mathf.InverseLerp(minDistance, maxDistance, distanceToTarget);
                    lockOnUIRectTransform.sizeDelta = Vector2.Lerp(minSize, maxSize, t);

                    // 화면 좌표를 UI 캔버스 좌표로 변환
                    lockOnUIRectTransform.position = screenPos;

                    if (isLockedOn)
                    {
                        // 락온 상태

                        if (isFlickering)
                        {
                            StopCoroutine(FlickerEffect());
                            isFlickering = false;
                        }
                    }
                    else if (isTargeted) //록온 안됨, 타겟됨.
                    {
                        // 타게팅 상태
                        if (!isFlickering)
                        {
                            StartCoroutine(FlickerEffect());
                        }
                    }
                    else
                    {
                        // 타겟팅되지 않은 상태
                        
                            StopCoroutine(FlickerEffect());
                            isFlickering = false;
                        
                    }



                        distanceText.text = ((int)(distanceToTarget * 10)).ToString();
                }
                else
                {
                    lockOnUIRectTransform.gameObject.SetActive(false);
                }
            }
            else
            {
                // 거리 범위 밖이면 UI 숨기기
                lockOnUIRectTransform.gameObject.SetActive(false);
            }

            
        }



        #endregion

        CheckWaypoint();
        Rotate();
        ZAxisRotate();
        Move();
    }

    public void initializeInstance(Transform playerTransform, TargettingSystem targettingSystem, TagController tagController, GameManagement gm, GameObject waypointObj)
    {
        player = playerTransform;
        this.targetingSystem = targettingSystem;
        this.tagController = tagController;
        this.gameManagement = gm;
        waypointObject = waypointObj;
    }

    #region target, lock controls

    // 타겟으로 지정될 때 호출
    public void OnTargeted()
    {
        isTargeted = true;

        if (!aircraftInfoUIobject.activeSelf) //적기 정보 활성화
        {
            aircraftInfoUIobject.SetActive(true);
        }

        StartCoroutine(MinimapEffect());
        

    }

    // 타겟에서 벗어날 때 호출
    public void OnUntargeted()
    {
        isTargeted = false;

        lockOnUIImage.color = normalColor;

        if (aircraftInfoUIobject.activeSelf)
        {
            aircraftInfoUIobject.SetActive(false);
        }

        StopCoroutine(MinimapEffect());
        isMinimapFlickering = false;
    }

    public void OnLockedOn()
    {
        lockOnUIImage.color = lockedOnColor; // 적기 UI 붉은색으로.
        if (isLockedOn) return;
        StopCoroutine(FlickerEffect());
        isFlickering = false;

        isLockedOn = true;

        lockOnUIImage.color = lockedOnColor; // 적기 UI 붉은색으로.
        distanceText.color = lockedOnColor;
        aircraftNameText.color = lockedOnColor;

        if (!aircraftInfoUIobject.activeSelf) // 적기 정보 UI 활성화.
        {
            aircraftInfoUIobject.SetActive(true);
        }

        lockOnSound.Play();
        lockOnSquare.SetActive(true);
        //록온
    }

    public void OnLockedOff()
    {
        if (!isLockedOn) return;
        isLockedOn = false;

        lockOnUIImage.color = normalColor;
        distanceText.color = normalColor;
        aircraftNameText.color = normalColor;


        if (lockOnSound.isPlaying)
        {
            lockOnSound.Stop();
        }
        //록 오프
        lockOnSquare.SetActive(false);

        
    }

    #endregion

    #region coroutines(minimap, targetBox)

    private IEnumerator FlickerEffect() //타겟이지만 록온되지 않았을 때, ui가 깜빡이는 효과 구현.
    {
        isFlickering = true;

        while (isFlickering)
        {
            lockOnUIImage.color = transparentColor;
            yield return new WaitForSeconds(0.25f);
            lockOnUIImage.color = normalColor;
            yield return new WaitForSeconds(0.25f);
        }
    }

    private IEnumerator MinimapEffect() //타겟이지만 록온되지 않았을 때, ui가 깜빡이는 효과 구현.
    {
        isMinimapFlickering = true;

        while (isMinimapFlickering)
        {
            minimapSprite.color = transparentColor;
            yield return new WaitForSeconds(0.25f);
            minimapSprite.color = Color.white;
            yield return new WaitForSeconds(0.25f);
        }
    }

    #endregion

    #region info controllers

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("bullet"))
        {
            AircraftDamage(15);
            if(aircraftHP > 0)
            {
                tagController.ShowHitTag();
            }
            
            Debug.Log("gun hit(aircraft detect)");

        }
        if (collision.gameObject.CompareTag("stdm"))
        {
            AircraftDamage(70);
            if (aircraftHP > 0)
            {
                tagController.ShowHitTag();
            }
            Debug.Log("msl hit(aircraft detect)");

        }
    }

    void AircraftDamage(int damage)
    {
        aircraftHP -= damage;

        if (aircraftHP <= 0)
        {
            AircraftDestroyed();
        }
    }

    [SerializeField] GameObject explodeEffect;
    void AircraftDestroyed()
    {
        if (explodeEffect) Instantiate(explodeEffect, transform.position, Quaternion.identity, null);
        targetingSystem.RemoveTarget(gameObject.transform); //현재 타겟 리스트에서 제거
        tagController.ShowDestroyedTag(); //destroyed 태그 표출
        gameManagement.UpdateScore(aircraftScore);
        Destroy(gameObject);
    }

    #endregion

}
