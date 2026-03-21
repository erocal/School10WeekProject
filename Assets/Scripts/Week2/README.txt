通過colider與rigibody加入重力後，只需要在移動方法內加入向上跳的力量即可(Vector3.up * jumpForce * Time.deltaTime)
然後簡易地使用jumpTimer控制跳躍間隔