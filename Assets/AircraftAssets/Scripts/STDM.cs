using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class STDM : MonoBehaviour
{
    public Transform target; // 추적할 타겟
    public float turningForce; // 회전 속도

    public float maxSpeed; // 최대 속도
    public float accelAmount; // 가속량
    public float lifetime; // 미사일의 수명
    public float speed; // 현재 속도

    public GameObject enemyHitEffect; //적기 명중시 폭파효과
    [SerializeField] Rigidbody rb;
    [SerializeField] CapsuleCollider mslCollider;

    public void Launch(Transform target, float launchSpeed)
    {
        // 타겟이 존재할 때만 할당
        if (target != null)
        {
            this.target = target;
        }

        // 발사 속도를 설정
        speed = launchSpeed;
    }

    void LookAtTarget()
    {
        // 타겟이 존재할 때만 추적
        if (target != null)
        {
            Quaternion lookRotation = Quaternion.LookRotation(target.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, turningForce * Time.deltaTime);
        }
    }

    void Start()
    {
        // 수명이 끝나면 미사일을 제거
        Destroy(gameObject, lifetime);
        rb = GetComponent<Rigidbody>();
        mslCollider = GetComponent<CapsuleCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        // 속도가 maxSpeed를 넘지 않도록 가속
        if (speed < maxSpeed)
        {
            speed += accelAmount * Time.deltaTime;
        }

        // 타겟이 없으면 직진만 수행
        if (target == null)
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }
        else
        {
            // 타겟이 있으면 추적
            LookAtTarget();
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }
    }

    void OnCollisionEnter(Collision collision) //땅이든, 적이든... 파괴.
    {

        if (collision.gameObject.CompareTag("Enemy"))
        {
            // 적기에 부딪혔을 때 효과 생성
            Instantiate(enemyHitEffect, transform.position, Quaternion.identity);

            Debug.Log("missilehittoenemy");
        }
        // 충돌한 오브젝트의 태그가 "Ground"일 경우
        //else if (collision.gameObject.CompareTag("Ground"))
        {
            // 땅에 닿았을 때 효과 생성
            //Instantiate(groundHitEffect, transform.position, Quaternion.identity);
        }

        // 총알 파괴
        Destroy(gameObject);
    }
}
