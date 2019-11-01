using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestObject : MonoBehaviour {
  // Start is called before the first frame update

  public GameObject gazesphere_dev;
  void Start () {

  }

  // Update is called once per frame
  void Update () {
    if (Input.GetKeyDown (KeyCode.Z)) {
      Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
      if (Physics.Raycast (ray, out var hit)) {
        Debug.Log ("hit : " + hit.point.x + " " + hit.point.y + " " + hit.point.z);
        Debug.DrawRay (ray.origin, ray.direction * 30f, Color.red,5);
        // Debug.Log ("origin : " + ray.origin + " , dir : " + ray.direction);
        // Debug.Log ("hit.tag : " + hit.transform.tag);
        gazesphere_dev.transform.position = new Vector3 (hit.point.x, hit.point.y, hit.point.z);
      }
    }

  }
}