using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleMazeMover : MonoBehaviour
{
    Rigidbody2D _rb;
    // Start is called before the first frame update
    void Start()
    {
        _rb = this.GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 move = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (move != Vector2.zero) 
        {
            _rb.MovePosition(_rb.position + move * Time.deltaTime * 3);
        }
    }
}
