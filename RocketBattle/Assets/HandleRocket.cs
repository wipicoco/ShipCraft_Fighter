using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class HandleRocket : NetworkBehaviour
{
    private HandlePlayer playerBoarded;
    private string shipName;

    private GameObject[] planets;

    [SerializeField]
    private float rotationSpeed = 100.0f;
    [SerializeField]
    private float thrustForce = .1f;

    [SerializeField]
    private float bulletSpeed = 50;
    [SerializeField]
    private GameObject bullet;

    private Rigidbody2D rig;
    private Rigidbody2D planetUnderGravity;
    private bool landed = true;

    private float health = 100;

    const float bounds = 20;

    void Start()
    {
        shipName = Random.Range(0, 1000000).ToString();

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
    }

    void Update()
    {
        if (this.isLocalPlayer)
        {
            if (playerBoarded != null)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                    landed = !landed;

                if (Input.GetMouseButtonDown(0))
                    this.CmdShootBullet();
            }

            if (landed && planetUnderGravity != null)
            {
                //Always face up relative to planet
                float f = (planetUnderGravity.mass * rig.mass) / Mathf.Pow(Vector2.Distance(transform.position, planetUnderGravity.transform.position), 2);
                rig.AddForce(f * (planetUnderGravity.transform.position - transform.position).normalized);

                var dir = planetUnderGravity.transform.position - transform.position;
                float targetRotAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                rig.SetRotation(targetRotAngle + 90);
            }
            else
            {
                //Rotate input
                rig.AddTorque(-Input.GetAxis("Horizontal") * rotationSpeed * Time.deltaTime);

                //Thrust input 
                rig.AddForce(transform.up * thrustForce * Input.GetAxis("Vertical"));

                //Brake input
                rig.AddForce(-rig.velocity.normalized * thrustForce * (Input.GetKey(KeyCode.C) ? 1 : 0));
            }
        }

        if (transform.position.x > bounds)
        {
            transform.position = new Vector3(-bounds, transform.position.y, transform.position.z);
        }
        else if (transform.position.x < -bounds)
        {
            transform.position = new Vector3(bounds, transform.position.y, transform.position.z);
        }
        if (transform.position.y > bounds)
        {
            transform.position = new Vector3(transform.position.x,-bounds, transform.position.z);
        }
        else if (transform.position.y < -bounds)
        {
            transform.position = new Vector3(transform.position.x, bounds, transform.position.z);
        }
    }
    void OnTriggerEnter2D(Collider2D c)
    {
        //Debug.Log(c.tag + ", " + myBullets.Count);
        if (c.tag.StartsWith("Bullet") && !c.tag.EndsWith(shipName))
        {
            GameObject[] spawns = GameObject.FindGameObjectsWithTag("Respawn");
            int randSpawn = Random.Range(0, spawns.Length);
            transform.position = spawns[randSpawn].transform.position;
        }
    }

    [Command]
    public void CmdBoard(HandlePlayer p)
    {
        playerBoarded = p;
        if (this.isLocalPlayer)
        {
            transform.Find("Camera").gameObject.SetActive(true);
        }
    }

    [Command]
    void CmdShootBullet()
    {
        GameObject b = Instantiate(bullet, new Vector3(transform.position.x, transform.position.y, 0), transform.rotation);
        b.tag += shipName;
        b.GetComponent<Rigidbody2D>().velocity = transform.up * bulletSpeed;
        NetworkServer.Spawn(b);
        Destroy(b, 2.0f);
    }
}
