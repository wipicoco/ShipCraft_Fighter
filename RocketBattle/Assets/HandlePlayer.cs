using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class HandlePlayer : NetworkBehaviour
{
    private string playerName;
    private HandleRocket rocketBoarded;
    private GameObject[] planets;

    [SerializeField]
    private float moveSpeed = .1f;
    [SerializeField]
    private float jumpForce = 10f;
    private short jumps = 0;
    private short allowedJumps = 2;
    private const float _JUMPCD = 0.1f;
    private float jumpCD = _JUMPCD;
    [SerializeField]
    private float bulletSpeed = 10;
    [SerializeField]
    private float maximumPlayerSpeed = 5;
    [SerializeField]
    private GameObject bullet;
    [SerializeField]
    private GameObject gun;
    private GameObject spawnedGun;
    private float gunAngle = 0;
    private bool isRight = true;
    private float playerZenith;

    private Rigidbody2D rig;
    private Rigidbody2D planetUnderGravity;

    private float health = 100;

    void Start()
    {
        GameObject menuCam = GameObject.Find("Main Camera");
        if (menuCam != null)
            menuCam.SetActive(false);

        playerName = Random.Range(0, 1000000).ToString();

        rig = transform.GetComponent<Rigidbody2D>();

        float minDist = 100000000;
        planets = GameObject.FindGameObjectsWithTag("Planet");
        for (int i = 0; i < planets.Length; i++)
        {
            float newDist = Vector2.Distance(transform.position, planets[i].transform.position);
            if (newDist < minDist)
            {
                minDist = newDist;
                planetUnderGravity = planets[i].GetComponent<Rigidbody2D>();
            }
        }

        //Spawn a gun for the player!
        SpawnGun();

        if (this.isLocalPlayer)
        {
            transform.Find("Camera").gameObject.SetActive(true);
        }


    }

    void Update()
    {
        if (this.isLocalPlayer && !rocketBoarded)
        {
            //Shoot bullet
            if (Input.GetMouseButtonDown(0))
                this.CmdShootBullet();

            CalculateGunAngle();

            if (planetUnderGravity != null)
            {
                //Gravity
                float f = (planetUnderGravity.mass * rig.mass) / Mathf.Pow(Vector2.Distance(transform.position, planetUnderGravity.transform.position), 2);
                rig.AddForce(f * (planetUnderGravity.transform.position - transform.position).normalized);

                //Always stand upright on planet
                var dir = planetUnderGravity.transform.position - this.transform.position;
                playerZenith = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                rig.SetRotation(playerZenith + 90);

                //Player input move across planet
                if (Input.GetAxis("Horizontal") != 0)
                {
                    rig.AddForce(Input.GetAxis("Horizontal") * new Vector2(Mathf.Abs(transform.right.x), Mathf.Abs(transform.right.y)) * moveSpeed);
                }

                //Decsellerate when not moving
                if (Input.GetAxis("Horizontal") == 0)
                    rig.AddForce(-rig.velocity.normalized * new Vector2(Mathf.Abs(transform.right.x), Mathf.Abs(transform.right.y)) * 2);

                //Jump (Can do double/triple/... jumps)
                jumpCD -= Time.deltaTime;
                if (Input.GetKeyDown(KeyCode.Space) && jumps < allowedJumps && jumpCD <= 0)
                {
                    rig.velocity = rig.velocity * new Vector2(Mathf.Abs(transform.right.x), Mathf.Abs(transform.right.y));
                    rig.AddForce(jumpForce * transform.up);
                    jumps += 1;
                    jumpCD = _JUMPCD;
                }

                //Slow the player once it reaches a certain speed
                if (rig.velocity.magnitude > maximumPlayerSpeed)
                {
                    rig.AddForce(Input.GetAxis("Horizontal") * new Vector2(Mathf.Abs(transform.right.x), Mathf.Abs(transform.right.y)) * -moveSpeed);
                }

            }
            if (Input.GetAxis("Horizontal") < 0)
            {
                isRight = false;
            }
            else if (Input.GetAxis("Horizontal") > 0)
            {
                isRight = true;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        //Debug.Log(c.tag + ", " + myBullets.Count);
        if (collider.tag == "Bullet" && !collider.name.EndsWith(playerName) && !rocketBoarded) //Bullet names are tied to player name
        {
            GameObject[] spawns = GameObject.FindGameObjectsWithTag("Respawn");
            int randSpawn = Random.Range(0, spawns.Length);
            transform.position = spawns[randSpawn].transform.position;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Planet" && Vector2.Distance(collision.GetContact(0).point, collision.transform.position) < Vector2.Distance(collision.transform.position, transform.position)) //Contact point is closer to planet than player, aka touched planet with feet
        {
            jumps = 0;
        }
    }

    [Command]
    void CmdShootBullet()
    {
        GameObject bulletSpawn = Instantiate(bullet, new Vector3(transform.position.x, transform.position.y, 0), Quaternion.Euler(transform.rotation.x, transform.rotation.y, gunAngle));
        bulletSpawn.name += playerName;
        bulletSpawn.transform.parent = transform;
        bulletSpawn.GetComponent<Rigidbody2D>().velocity = new Vector2(Mathf.Cos(Mathf.Deg2Rad * (gunAngle + playerZenith + 90)), Mathf.Sin(Mathf.Deg2Rad * (gunAngle + playerZenith + 90))) * bulletSpeed;
        NetworkServer.Spawn(bulletSpawn);
        Destroy(bulletSpawn, 2.0f);
    }

    void SpawnGun()
    {
        spawnedGun = Instantiate(gun, new Vector3(transform.position.x, transform.position.y, 0), transform.rotation);
        spawnedGun.name += playerName;
        spawnedGun.transform.parent = transform;
        NetworkServer.Spawn(spawnedGun);

    }

    void CalculateGunAngle()
    {

        if (Input.GetAxis("Horizontal") != 0)
        {
            if ((gunAngle < -90f) && isRight)
            {
                gunAngle = gunAngle - (2 * (gunAngle - -90f));
            }
            else if ((gunAngle > 90f) && isRight)
            {
                gunAngle = gunAngle - (2 * (gunAngle - 90f));
            }
            else if ((gunAngle < 90f) && (gunAngle > 0f) && !isRight)
            {
                gunAngle = gunAngle + (2 * (90f - gunAngle));
            }
            else if ((gunAngle > -90f) && (gunAngle < 0f) && !isRight)
            {
                gunAngle = gunAngle + (2 * (-90f - gunAngle));
            }
            else if ((gunAngle == 0) && !isRight)
            {
                gunAngle = -180;
            }
            else if ((gunAngle == -180) && isRight)
            {
                gunAngle = 0;
            }
        }

        /*
         * code for smooth aiming
        if (Input.GetKey(KeyCode.W))
        {
            if (gunAngle < -90f || gunAngle > 90f)
            {
                gunAngle -= .5f;
            }
            else
            {
                gunAngle += .5f;
            }
        }
        if (Input.GetKey(KeyCode.S))
        {
            if (gunAngle < -90f || gunAngle > 90f)
            {
                gunAngle += .5f;
            }
            else
            {
                gunAngle -= .5f;
            }
        }
        */

        //code for simple aiming (pierce preferred)
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (gunAngle != 90f && gunAngle != -90f)
            {
                if (gunAngle < -90f || gunAngle > 90f)
                {
                    gunAngle -= 45f;
                }
                else
                {
                    gunAngle += 45f;
                }
            }
            else if (gunAngle == -90f)
            {
                if (isRight)
                {
                    gunAngle += 45f;
                }
                else
                {
                    gunAngle -= 45;
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (gunAngle != -90f && gunAngle != 90f)
            {
                if (gunAngle < -90f || gunAngle > 90f)
                {
                    gunAngle += 45f;
                }
                else
                {
                    gunAngle -= 45f;
                }
            }
            else if (gunAngle == 90f)
            {
                if (isRight)
                {
                    gunAngle -= 45f;
                }
                else
                {
                    gunAngle += 45;

                }
            }
        }
        spawnedGun.transform.localRotation = Quaternion.Euler(0, 0, gunAngle);
    }
    
}
