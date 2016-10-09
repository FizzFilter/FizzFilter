using UnityEngine;
using System.Collections;

public class sizePrinter : MonoBehaviour {

    public GameObject input;
	// Use this for initialization
	void Start () {
        print(input.transform.lossyScale);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
