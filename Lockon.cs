using System.Linq;
using UnityEngine;
using UnityStandardAssets.Cameras;

public class Lockon : BaseBehaviour
{
    private GameObject lockonTarget;
    private float lockonSpeed = 0;
    private float lockonMaxDistance = 0;
    private bool lockingon = false;

    private GameObject player;
    private GameObject mainCamera;
    private FreeLookCam freeLookCam;

    /// <summary>
    /// Use this for initialization
    /// </summary>
    private void Start()
    {
        // Set the Player object.
        player = GameObject.FindGameObjectWithTag("Player");

        // Set the MainCamera object.
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");

        // Set the FreeLookCameraScript.
        freeLookCam = GameObject.Find("FreeLookCameraRig").GetComponent<FreeLookCam>();
    }

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    private void Update()
    {
        if (lockonTarget == null)
        {
            DisableLockon();
        }
    }

    /// <summary>
    /// LateUpdate is called after all Update functions have been called
    /// </summary>
    private void LateUpdate()
    {
        if (lockingon && lockonTarget != null)
        {
            // Look at lockonTarget.
            freeLookCam.Lockon(lockonTarget, lockonSpeed);
        }
    }

    /// <summary>
    /// Set the lockon speed.
    /// </summary>
    /// <param name="lockonSpeed">The lockon speed.</param>
    public void SetLockonSpeed(float lockonSpeed)
    {
        this.lockonSpeed = lockonSpeed;
    }

    /// <summary>
    /// Set the lockon max distance.
    /// </summary>
    /// <param name="lockonMaxDistance">The lockon max distance.</param>
    public void SetLockonMaxDistance(float lockonMaxDistance)
    {
        this.lockonMaxDistance = lockonMaxDistance;
    }

    /// <summary>
    /// Enable lockon mode.
    /// </summary>
    /// <param name="target">The lockon target.</param>
    public void EnableLockon(GameObject target)
    {
        // Pause the main camera.
        freeLookCam.Pause();

        // Rader spike to enemy.
        var enemy = target.GetComponent<BaseEnemy>();
        enemy.SetRadarSpike(true);

        this.lockonTarget = target;
        lockingon = true;
    }

    /// <summary>
    /// Disable lockon mode.
    /// </summary>
    public void DisableLockon()
    {
        if (lockingon)
        {
            // Resume the main camera.
            freeLookCam.Resume();

            // rset the Rader spike.
            if (this.lockonTarget != null)
            {
                var enemy = this.lockonTarget.GetComponent<BaseEnemy>();
                enemy.SetRadarSpike(false);
            }

            this.lockonTarget = null;
            lockingon = false;
        }
    }

    /// <summary>
    /// Is lockonMode.
    /// </summary>
    /// <returns>True or False.</returns>
    public bool IsLockonMode()
    {
        return !(this.lockonTarget == null);
    }

    /// <summary>
    /// Get target position.
    /// </summary>
    /// <returns>The target position.</returns>
    public Vector3 GetTargetPos()
    {
        if (lockonTarget == null)
        {
            return Vector3.zero;
        }
        else
        {
            return lockonTarget.transform.position;
        }
    }

    /// <summary>
    /// Get target from all screen.
    /// </summary>
    /// <param name="isNearestPlayer">Is nearest player.</param>
    /// <param name="isScreenCentral">Is screen central.</param>
    /// <returns>The target object.</returns>
    public GameObject GetTargetFromAllScreen(bool isNearestPlayer = false, bool isScreenCentral = false)
    {
        var enemys = GameObject.FindGameObjectsWithTag("Enemy")
            .Where(enemy => enemy.GetComponent<BaseEnemy>().IsVisible() == true)                                                    // Whether it is in the screen.
            .Where(enemy => Camera.main.WorldToScreenPoint(enemy.transform.position).z > -(mainCamera.transform.localPosition.z))   // Whether it is far from the player.
            .Where(enemy => Vector3.Distance(player.transform.position, enemy.transform.position) < lockonMaxDistance)              // Whether it is within lockable range.
            .Where(enemy => RaycastTarget(enemy) == true);                                                                          // Whether rays pass.

        // Get the target closest to the player.
        if (isNearestPlayer)
        {
            enemys = enemys.OrderBy(enemy => Vector3.Distance(player.transform.position, enemy.transform.position));
        }

        // Get the target closest to the center of the screen,
        if (isScreenCentral)
        {
            enemys = enemys.OrderBy(enemy => Vector2.Distance(new Vector2(Screen.width / 2.0f, Screen.height / 2.0f), Camera.main.WorldToScreenPoint(enemy.transform.position)));
        }

        return enemys.FirstOrDefault();
    }

    /// <summary>
    /// Get target from click.
    /// </summary>
    /// <returns>The target object.</returns>
    public GameObject GetTargetFromClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        int layerMask = (1 << LayerMask.NameToLayer("Enemy")) + (1 << LayerMask.NameToLayer("Terrain")); // Get "Enemy" and "Terrain" layers only.
        var hits = Physics.RaycastAll(ray, lockonMaxDistance, layerMask);

        foreach (var hit in hits.OrderBy(hit => hit.distance).Select((value, index) => new { value, index }))
        {
            if (hit.value.collider.CompareTag("Terrain"))
            {
                // Ignore terrain if camera is below player.
                if (player.transform.position.y > mainCamera.transform.position.y && (hit.index == 0))
                {
                    continue;
                }

                break;
            }
            else if (hit.value.collider.CompareTag("Enemy"))
            {
                return hit.value.collider.gameObject;
            }
        }

        return null;
    }

    /// <summary>
    /// Check if rays pass through target.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <returns>True or False.</returns>
    private bool RaycastTarget(GameObject target)
    {
        var heading = target.transform.position - mainCamera.transform.position;    // The camera position be the origin.
        var distance = heading.magnitude;
        var direction = heading / distance; // This is now the normalized direction.

        int layerMask = (1 << LayerMask.NameToLayer("Enemy")) + (1 << LayerMask.NameToLayer("Terrain")); // Get "Enemy" and "Terrain" layers only.
        var hits = Physics.RaycastAll(mainCamera.transform.position, direction, lockonMaxDistance, layerMask);

        foreach (var hit in hits.OrderBy(hit => hit.distance).Select((value, index) => new { value, index }))
        {
            if (hit.value.collider.CompareTag("Terrain"))
            {
                // Ignore terrain if camera is below player.
                if (player.transform.position.y > mainCamera.transform.position.y && (hit.index == 0))
                {
                    continue;
                }

                return false;
            }
            else if (hit.value.collider.CompareTag("Enemy") && hit.value.transform.position == target.transform.position)
            {
                return true;
            }
        }

        return false;
    }
}