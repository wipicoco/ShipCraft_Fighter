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
    private GameObject bullet;

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
                var dir = planetUnderGravity.transform.position - transform.position;
                float targetRotAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                rig.SetRotation(targetRotAngle + 90);

                //Player input move across planet
                if (Input.GetAxis("Horizontal") != 0)
                    rig.AddForce(Input.GetAxis("Horizontal") * transform.right * moveSpeed);

                //Jump (Can do double/triple/... jumps)
                jumpCD -= Time.deltaTime;
                if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) && jumps < allowedJumps && jumpCD <= 0)
                {
                    rig.velocity = rig.velocity * transform.right;
                    rig.AddForce(jumpForce * transform.up);
                    jumps += 1;
                    jumpCD = _JUMPCD;
                }
                if (Input.GetKeyDown(KeyCode.S))
                {
                    rig.velocity = rig.velocity * transform.right;
                }

                //RigBod Friction is whack, manually dampen player horizontal movement
                if ((rig.velocity * transform.up).magnitude < .1f && jumps == 0)
                    rig.AddForce(-rig.velocity.normalized * (moveSpeed / 4));
            }
        }
    }

    void OnTriggerEnter2D(Collider2D c)
    {
        //Debug.Log(c.tag + ", " + myBullets.Count);
        if (c.tag == "Bullet" && !c.name.EndsWith(playerName) && !rocketBoarded) //Bullet names are tied to player name
        {
            GameObject[] spawns = GameObject.FindGameObjectsWithTag("Respawn");
            int randSpawn = Random.Range(0, spawns.Length);
            transform.position = spawns[randSpawn].transform.position;
        }
    }

    private void OnCollisionEnter2D(Collision2D c)
    {
        if (c.gameObject.tag == "Planet" && Vector2.Distance(c.GetContact(0).point, c.transform.position) < Vector2.Distance(c.transform.position, transform.position)) //Contact point is closer to planet than player, aka touched planet with feet
        {
            jumps = 0;
        }
    }

    [Command]
    void CmdShootBullet()
    {
        GameObject b = Instantiate(bullet, new Vector3(transform.position.x, transform.position.y, 0), transform.rotation);
        b.name += playerName;
        b.GetComponent<Rigidbody2D>().velocity = transform.right * bulletSpeed;
        NetworkServer.Spawn(b);
        Destroy(b, 2.0f);
    }
}
