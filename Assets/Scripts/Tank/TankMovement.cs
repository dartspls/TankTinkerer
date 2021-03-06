﻿using UnityEngine;


public class TankMovement : MonoBehaviour
{
    public int m_PlayerNumber = 1;              // Used to identify which tank belongs to which player.  This is set by this tank's manager.
	public float m_TurnSpeed = 90f;            // How fast the tank turns in degrees per second.
    public AudioSource m_MovementAudio;         // Reference to the audio source used to play engine sounds. NB: different to the shooting audio source.
    public AudioClip m_EngineIdling;            // Audio to play when the tank isn't moving.
    public AudioClip m_EngineDriving;           // Audio to play when the tank is moving.
    public float m_PitchRange = 0.2f;           // The amount by which the pitch of the engine noises can vary.
    public bool m_HasFlag;                      // Determine whether flag is in possesion
    public GameObject m_SpeedPrefab;           // Reference to the gameobject with the speedlines particle effect
	public float m_Speed;                // Tank speed with subtractions/additions 

    private string m_MovementAxisName;          // The name of the input axis for moving forward and back.
    private string m_TurnAxisName;              // The name of the input axis for turning.
    private Rigidbody m_Rigidbody;              // Reference used to move the tank.
    private float m_MovementInputValue;         // The current value of the movement input.
    private float m_TurnInputValue;             // The current value of the turn input.
    private float m_OriginalPitch;              // The pitch of the audio source at the start of the scene.
    private ParticleSystem[] m_particleSystems; // References to all the particles systems used by the Tanks
    private SerialController m_SerialController;  //Reference to the serial controller
    private string m_Controller;                  //Reference to control settings
    private float m_SpeedUpCountdown;             // Stores the time left of speed boost
	private float m_SpeedDownCountdown; 		 // Stores the time left of slow
	private ParticleSystem m_SpeedParticles;     // Reference to the particle system activated when tank is sped up
    private int m_PlayerTeamID;                 // Stores whether the player is 0 or 1 in the team 

    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();

        //Initialize the countdown timer
        m_SpeedUpCountdown = 0f;
		m_SpeedDownCountdown = 0f;

        // Instantiate the speed prefab and get a reference to the particle system on it.
        m_SpeedParticles = Instantiate(m_SpeedPrefab).GetComponent<ParticleSystem>();

        // Disable the prefab so it can be activated when it's required.
        m_SpeedPrefab.gameObject.SetActive(false);

	 }


    private void OnEnable()
    {
        // When the tank is turned on, make sure it's not kinematic.
        m_Rigidbody.isKinematic = false;

        // Also reset the input values.
        m_MovementInputValue = 0f;
        m_TurnInputValue = 0f;

        // We grab all the Particle systems child of that Tank to be able to Stop/Play them on Deactivate/Activate
        // It is needed because we move the Tank when spawning it, and if the Particle System is playing while we do that
        // it "think" it move from (0,0,0) to the spawn point, creating a huge trail of smoke
        m_particleSystems = GetComponentsInChildren<ParticleSystem>();
        for (int i = 0; i < m_particleSystems.Length; ++i)
        {
            m_particleSystems[i].Play();
        }
    }


    private void OnDisable()
    {
        // When the tank is turned off, set it to kinematic so it stops moving.
        m_Rigidbody.isKinematic = true;

        // Stop all particle system so it "reset" it's position to the actual one instead of thinking we moved when spawning
        for (int i = 0; i < m_particleSystems.Length; ++i)
        {
            m_particleSystems[i].Stop();
        }
    }


    private void Start()
    {
        // The axes names are based on player number.
        m_MovementAxisName = "Vertical" + m_PlayerNumber;
        m_TurnAxisName = "Horizontal" + m_PlayerNumber;

        // Store the original pitch of the audio source.
        m_OriginalPitch = m_MovementAudio.pitch;

        //Initialize Control Scheme
        if (m_PlayerNumber % 2 != 0)
        {
            m_Controller = GameObject.Find("GameManager").GetComponent<GameManager>().BlueControl;
            if (m_Controller != "Keyboard")
            {
                m_SerialController = GameObject.Find("SerialController1").GetComponent<SerialController>();
            }
        }
        else
        {
            m_Controller = GameObject.Find("GameManager").GetComponent<GameManager>().RedControl;
            if (m_Controller != "Keyboard")
            {
                m_SerialController = GameObject.Find("SerialController2").GetComponent<SerialController>();
            }
        }
        //Set player team id
        if (m_PlayerNumber < 3)
        {
            m_PlayerTeamID = 0;
        }
        else
        {
            m_PlayerTeamID = 1;
        }
    }


    private void Update()
    {
        if (m_Controller == "Keyboard")
        {
            // Store the value of both input axes.
            m_MovementInputValue = Input.GetAxis(m_MovementAxisName);
            m_TurnInputValue = Input.GetAxis(m_TurnAxisName);
        }
        else
        {
            // Store the value of both input axes.
            m_MovementInputValue = m_SerialController.m_MoveValues[m_PlayerTeamID];
            m_TurnInputValue = m_SerialController.m_TurnValues[m_PlayerTeamID];
        }

        if (m_SpeedUpCountdown > 0f)
        {
            m_SpeedUpCountdown -= Time.deltaTime;
            m_SpeedParticles.transform.position = transform.position;
            m_SpeedParticles.transform.rotation = transform.rotation;
            if (m_SpeedUpCountdown == 0f)
            {
                // In the event the countdown somehow becomes 0
                m_SpeedUpCountdown = -1f;
            }
        }
        else if (m_SpeedUpCountdown <0f)
        {
			m_Speed -= 4f;
            m_SpeedUpCountdown = 0f;
            // Stop the speedlines particle system
            m_SpeedParticles.Stop();
            m_SpeedParticles.gameObject.SetActive(false);
        }

		if (m_SpeedDownCountdown > 0f)
		{
			m_SpeedDownCountdown -= Time.deltaTime;
			if (m_SpeedDownCountdown == 0f)
			{
				// In the event the countdown somehow becomes 0
				m_SpeedDownCountdown = -1f;
			}
		}
		else if (m_SpeedDownCountdown <0f)
		{
			m_Speed += 6f;
			m_SpeedDownCountdown = 0f;
		}

        EngineAudio();

    }


    private void EngineAudio()
    {
        // If there is no input (the tank is stationary)...
        if (Mathf.Abs(m_MovementInputValue) < 0.1f && Mathf.Abs(m_TurnInputValue) < 0.1f)
        {
            // ... and if the audio source is currently playing the driving clip...
            if (m_MovementAudio.clip == m_EngineDriving)
            {
                // ... change the clip to idling and play it.
                m_MovementAudio.clip = m_EngineIdling;
                m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                m_MovementAudio.Play();
            }
        }
        else
        {
            // Otherwise if the tank is moving and if the idling clip is currently playing...
            if (m_MovementAudio.clip == m_EngineIdling)
            {
                // ... change the clip to driving and play.
                m_MovementAudio.clip = m_EngineDriving;
                m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                m_MovementAudio.Play();
            }
        }
    }


    private void FixedUpdate()
    {
        // Adjust the rigidbodies position and orientation in FixedUpdate.
        Move();
        Turn();
    }


    private void Move()
    {
        // Create a vector in the direction the tank is facing with a magnitude based on the input, speed and the time between frames.
		Vector3 movement = transform.forward * m_MovementInputValue * m_Speed * Time.deltaTime;

        // Apply this movement to the rigidbody's position.
        m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
    }


    private void Turn()
    {
        // Determine the number of degrees to be turned based on the input, speed and time between frames.
        float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;

        // Make this into a rotation in the y axis.
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);

        // Apply this rotation to the rigidbody's rotation.
        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
    }

	public void SpeedUp()
    {
        if (m_SpeedUpCountdown == 0f)
        {
			m_Speed += 4f;
            m_SpeedUpCountdown = 10f;
            // Enable the speed particles
            m_SpeedParticles.gameObject.SetActive(true);
            // Play the speed particles
            m_SpeedParticles.Play();
            // Set the position and rotation of the particles to the player
            m_SpeedParticles.transform.position = transform.position;
            m_SpeedParticles.transform.rotation = transform.rotation;
        }

    }

	public void SpeedDown()
	{
		if (m_SpeedDownCountdown == 0f)
		{
			m_Speed -= 6f;
			m_SpeedDownCountdown = 10f;
		}

	}

}

