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
    private float maximumPlayerSpeed = 1;
    [SerializeField]
    private GameObject bullet;
    [SerializeField]
    private GameObject gun;
    private GameObject spawnedGun;
    private float gunAngle = 0;

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

            if (planetUnderGravity != null)
            {
                //Gravity
                float f = (planetUnderGravity.mass * rig.mass) / Mathf.Pow(Vector2.Distance(transform.position, planetUnderGravity.transform.position), 2);
                rig.AddForce(f * (planetUnderGravity.transform.position - transform.position).normalized);

                //Always stand upright on planet
                var dir = planetUnderGravity.transform.position - this.transform.position;
                float targetRotAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                rig.SetRotation(targetRotAngle + 90);

                //Player input move across planet
                if (Input.GetAxis("Horizontal") != 0)
                {
                    rig.AddForce(Input.GetAxis("Horizontal") * transform.right * moveSpeed);
                }

                //Decsellerate when not moving
                if (Input.GetAxis("Horizontal") == 0)
                    rig.AddForce(-rig.velocity.normalized * transform.right * 2);

                //Jump (Can do double/triple/... jumps)
                jumpCD -= Time.deltaTime;
                if (Input.GetKeyDown(KeyCode.Space) && jumps < allowedJumps && jumpCD <= 0)
                {
                    rig.velocity = rig.velocity * transform.right;
                    rig.AddForce(jumpForce * transform.up);
                    jumps += 1;
                    jumpCD = _JUMPCD;
                }
                if (Input.GetKey(KeyCode.W))
                {
                    gunAngle += 1;
                }
                if (Input.GetKey(KeyCode.S))
                {
                    gunAngle -= 1;
                }
                spawnedGun.transform.localRotation = Quaternion.Euler(0,0,gunAngle);

                //Slow the player once it reaches a certain speed
                if ((rig.velocity.magnitude - (Vector2.one.magnitude) * maximumPlayerSpeed) >= 1)
                {
                        rig.AddForce(-Vector2.one * (rig.velocity.magnitude - (Vector2.one.magnitude * maximumPlayerSpeed)) * transform.right * rig.velocity.normalized);
                }

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
        GameObject bulletSpawn = Instantiate(bullet, new Vector3(transform.position.x, transform.position.y, 0), transform.rotation);
        bulletSpawn.name += playerName;
        bulletSpawn.GetComponent<Rigidbody2D>().velocity = transform.right * bulletSpeed;
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
}
