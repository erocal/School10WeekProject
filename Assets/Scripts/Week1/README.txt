我通過加入[SerializeField] private float rotationSpeed，來讓我們可以在inspector去調整速度
通過課堂上教授的方式，使用rotationSpeed * Time.deltaTime可以進行角度的累加，使用transform內建的Rotate方法，就可以進行旋轉
並且我加入了Debug.Log($"Rotation: {transform.eulerAngles}");進行旋轉角度的紀錄