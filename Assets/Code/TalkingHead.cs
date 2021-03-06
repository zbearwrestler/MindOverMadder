﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TalkingHead : MonoBehaviour
{
    [Header("Head Propertys")]
    public int HeadID;
    public int LinesDialogue = 1;

    [Header("Prefabs")]
    public GameObject AngryPrefab;
    public GameObject NeutralPrefab;
    public GameObject PassAggPrefab;

    [Header("Spawning")]
    public Transform[] SpawnPosition;
    public Transform[] CenterPosition;
    public float FastestSpawnRate = 0.5f;
    public float SlowestSpawnRate = 1.5f;
    public float DirectionSpread = 0.1f;

    [Header("Projectile Interactions")]
    public float Collide_Angry_Aggro = 6; //++
    public float Collide_Angry_Comm = 0; //0

    public float Collide_PassAgg_Aggro = 3; //+
    public float Collide_PassAgg_Comm = -3; //+

    public float Collide_Neutral_Aggro = 0; //0
    public float Collide_Neutral_Comm = 1.5f; //+

    public float Destroy_Neutral_Aggro = 6; //++
    public float Destroy_Neutral_Comm = -6; //--

    public float Collide_Positive_Aggro = -3; //-
    public float Collide_Positive_Comm = 3; //+

    public float IncrementMultiplier = 1.0f;



    [Header("Other")]
    public bool IsStarter;

    public TalkingHead target;

    public float Aggressiveness
    {
        get
        {
            return mAggressiveness;
        }
        set
        {
            mAggressiveness = value;
            if (mAggressiveness <= 0) { //cap and check for game win
                TalkingHeadManager.Instance.CheckForGameWin();
                mAggressiveness = 0;
            }
            if (mAggressiveness > 100)//cap and game lose
            {
                Aggressiveness = 100;
                TalkingHeadManager.Instance.EndGame(HeadID, GameScore.EndResult.Anger);
                Debug.Log("Lose!!!!!");
                GoPostal();
            }
            if (mAnimator)
            {
                mAnimator.SetFloat("AngerLevel", Aggressiveness);
            }
        }
    }

    public float Communicativeness
    {
        get
        {
            return mCommunicativeness;
        }
        set
        {
            mCommunicativeness = value;
            if (mCommunicativeness >= 100) { //cap and check for game win
                TalkingHeadManager.Instance.CheckForGameWin();
                mCommunicativeness = 100;
            }
            if (mCommunicativeness < 0) //cap and game lose
            {
                Communicativeness = 0;
                Debug.Log("Lose!!!!!");
                TalkingHeadManager.Instance.EndGame(HeadID, GameScore.EndResult.NotTalking);
                GoPostal();
            }
        }
    }

    //Private Variables
    private Vector2[] mSpawnDirections = new Vector2[3];
    private Animator mAnimator;

    private float mAggressiveness = 50f;
    private float mCommunicativeness = 50f;
    private List<Coroutine> replyCoroutines;
    private int convoIndex;

    private bool m_IsRanting = false;

    //--------------------------------------------------
    //Unity Functions
    void Awake()
    {
        Physics2D.IgnoreLayerCollision(8, 8, true);
        Init();
        if (IsStarter)
        {
            StartCoroutine(WaitAndReply(1));
        }
    }

    private void Start()
    {
        convoIndex = 0;
    }

    void Update()
    {
        //mInsultSpawnTimer += Time.deltaTime * SpawnSpeed;
        //if (mInsultSpawnTimer >= 10)
        //{
        //    mInsultSpawnTimer = 0;
        //    SpawnTextProjectile();
        //}

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TextProjectile textProj = collision.gameObject.GetComponent<TextProjectile>();
        if (textProj != null)
        {
            if (textProj.SpawnedBy != HeadID)
            {

                //run effects of what kind of projectile it is
                switch (textProj.ProjectileType)
                {
                    case TextProjectile.Type.Angry:
                        //Aggressiveness += 2*mIncrement;
                        Aggressiveness += (Collide_Angry_Aggro*IncrementMultiplier);
                        Communicativeness += (Collide_Angry_Comm * IncrementMultiplier);
                        //AudioManager.Play("ThroatClear");
                        break;
                    case TextProjectile.Type.PassAggressive:
                        //Aggressiveness += mIncrement;
                        //Communicativeness -= mIncrement;
                        Aggressiveness += (Collide_PassAgg_Aggro * IncrementMultiplier);
                        Communicativeness += (Collide_PassAgg_Comm * IncrementMultiplier);
                        //AudioManager.Play("ThroatClear");
                        break;
                    case TextProjectile.Type.Neutral:
                        //Communicativeness += mIncrement/2f;
                        Aggressiveness += (Collide_Neutral_Aggro * IncrementMultiplier);
                        Communicativeness += (Collide_Neutral_Comm * IncrementMultiplier);
                        //AudioManager.Play("ThroatClear");
                        break;
                    case TextProjectile.Type.Positive:
                        //Communicativeness += mIncrement;
                        //Aggressiveness -= mIncrement;
                        Aggressiveness += (Collide_Positive_Aggro * IncrementMultiplier);
                        Communicativeness += (Collide_Positive_Comm * IncrementMultiplier);
                        break;
                    default:
                        break;
                }
                Destroy(collision.gameObject);

            }
        }
    }



    //--------------------------------------------------
    //Custom Functions
    private void Init()
    {
        for (int i = 0; i < 3; ++i)
        {
            mSpawnDirections[i] = (SpawnPosition[i].position - CenterPosition[i].position).normalized;
        }
        mAnimator = GetComponent<Animator>();
    }

    private void SpawnTextProjectile()
    {
        //calculate odds
        float totalOdds = Communicativeness + 100f;
        float randomResult = Random.Range(0, totalOdds);

        GameObject prefabToSpawn = PassAggPrefab;
        if (randomResult < Communicativeness) { prefabToSpawn = NeutralPrefab; }
        else if (randomResult < Communicativeness + Aggressiveness) { prefabToSpawn = AngryPrefab; }

        int spawnLane = Random.Range(0, 3);

        //set direction
        Vector2 dir = AddSpreadToVector(mSpawnDirections[spawnLane],DirectionSpread);

        //spawn
        GameObject spawnedObject = GameObject.Instantiate(prefabToSpawn, SpawnPosition[spawnLane].position, Quaternion.identity);
        spawnedObject.GetComponent<TextProjectile>().Initialize(dir, HeadID, convoIndex);

        //increment spawn counter and loop it
        convoIndex++;
        if (convoIndex > LinesDialogue) { convoIndex = 0; }

        //Trigger animation
        if (mAnimator)
        {
            mAnimator.SetTrigger("Talk");
        }

        //play sound effect
        AudioManager.Play((HeadID == 1) ? "JibberishHead1Negative" : "JibberishHead0Negative");
    }

    public void TriggerWaitToReply()
    {
        float time = 1f;
        //calculate time

        time = Mathf.Lerp(SlowestSpawnRate, FastestSpawnRate, (Aggressiveness + (Communicativeness/2))/150f);
        //Debug.Log(gameObject.name + "Anger: "  + Aggressiveness + ", Talky: " + Communicativeness + " => " + time);
        


        StartCoroutine(WaitAndReply(time));
    }

    public void NotifyWasInterrupted()
    {
        //someone shot down your neutral statement. What meanies!
        //Debug.Log("Shot down!");
        Communicativeness += (Destroy_Neutral_Comm * IncrementMultiplier);
        Aggressiveness += (Destroy_Neutral_Aggro * IncrementMultiplier);
    }

    public void StopShooting()
    {
        foreach (Coroutine coro in replyCoroutines)
        {
            StopCoroutine(coro);
        }
        replyCoroutines.Clear();
    }

    private IEnumerator WaitAndReply(float time)
    {
        //wait
        yield return new WaitForSeconds(time);
        //spawn own projectile
        SpawnTextProjectile();
        //trigger other head to wait and reply
        target.TriggerWaitToReply();
        //remove this coroutine from list

    }


    private void GoPostal()
    {
        if (!m_IsRanting)
        {
            m_IsRanting = true;
            Aggressiveness = 100;
            Communicativeness = 0;
            StartCoroutine(Rant(3, 0.1f));
        }
    }

    IEnumerator Rant(float rantTime, float rate)
    {
        float timer = 0;
        while (timer < rantTime)
        {
            yield return new WaitForSeconds(rate);
            SpawnTextProjectile();
        }
        m_IsRanting = false;
    }

    private Vector2 AddSpreadToVector(Vector2 vect, float spread)
    {
        Vector2 newVect = new Vector2(vect.x + Random.Range(-spread, spread), vect.y + (Random.Range(-spread, spread)));
        newVect.Normalize();
        //Debug.Log(vect + "=>" + newVect);
        return newVect;
    }



}
