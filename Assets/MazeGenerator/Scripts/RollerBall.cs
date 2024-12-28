using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;

//<summary>
//Ball movement controlls and simple third-person-style camera
//</summary>
public class RollerBall : MonoBehaviour {

	public GameObject ViewCamera = null;
	public AudioClip JumpSound = null;
	public AudioClip HitSound = null;
	public AudioClip CoinSound = null;
    private MazeSpawner mazeSpawner;

    private Rigidbody mRigidBody = null;
	private AudioSource mAudioSource = null;
	//private bool mFloorTouched = false;

    private char[,] MazeMatrix;
    private Vector2Int startPos;
    private Vector2Int goalPos;
    private List<string> directions;
    private List<Vector2Int> path;

    void Start () {
		mRigidBody = GetComponent<Rigidbody> ();
		mAudioSource = GetComponent<AudioSource> ();

        mazeSpawner = FindObjectOfType<MazeSpawner>();

        StartCoroutine(WaitForMazeSpawner());
    }

    IEnumerator WaitForMazeSpawner()
    {
        // Wait until MazeMatrix is initialized
        while (mazeSpawner == null || mazeSpawner.MazeMatrix == null)
        {
            mazeSpawner = FindObjectOfType<MazeSpawner>();
            yield return null;
        }
        mazeSpawner.PrintMatrix(mazeSpawner.MazeMatrix);
        MazeMatrix = mazeSpawner.MazeMatrix;

        startPos = FindPosition('B');
		goalPos = FindPosition('G');

        Debug.Log("Starting at: " + startPos);
        Debug.Log("Goal at: " + goalPos);

        path = FindShortestPath(startPos, goalPos);
        

        if (path != null)
        {
            path.Insert(0, startPos);
            Debug.Log("Path found.");
            //foreach (Vector2Int point in path)
            //{
            //    Debug.Log("Path: " + point);
            //}
        }
        else
        {
            Debug.Log("No path found.");
            yield break;
        }

        directions = GetDirections(path);

        // Print the directions
        //foreach (string direction in directions)
        //{
        //    Debug.Log(direction);
        //}

        StartCoroutine(MoveBallAutomatically());
    }

    IEnumerator MoveBallAutomatically()
    {
        // Check if the path exists
        if (path == null || path.Count == 0)
        {
            Debug.Log("No path available.");
            yield break;
        }

        float stepDistance = 4f; // Distance for each grid step
        float moveDuration = 1f; // Time to complete each step

        // Loop through the directions and move the ball
        foreach (string direction in directions)
        {
            //Debug.Log("Moving " + direction);
            Vector3 moveDirection = Vector3.zero;
            if (direction == "up")
                moveDirection = Vector3.forward;
            else if (direction == "down")
                moveDirection = Vector3.back;
            else if (direction == "left")
                moveDirection = Vector3.left;
            else if (direction == "right")
                moveDirection = Vector3.right;
            yield return StopBall();
            //Debug.Log("Ball Stopped");
            yield return new WaitForSeconds(1);
            //Debug.Log("Ball start to move");
            yield return MoveBall(moveDirection, stepDistance, moveDuration);
        }
    }

    IEnumerator MoveBall(Vector3 direction, float distance, float duration)
    {
        if (mRigidBody == null) yield break;

        Vector3 targetVelocity = direction * (distance / duration);
        mRigidBody.velocity = targetVelocity;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        mRigidBody.velocity = Vector3.zero;
    }
    IEnumerator StopBall()
    {
        if (mRigidBody == null) yield break;

        // Gradually stop the ball by setting its velocity to zero
        mRigidBody.velocity = Vector3.zero;

        // Wait until the ball is fully stopped
        while (mRigidBody.velocity.magnitude > 0.01f)
        {
            yield return null;
        }
    }

    // OLD CONTROLS USING KEYBOARD
    //void FixedUpdate () {
	//	if (mRigidBody != null) {
	//		if (Input.GetButton ("Horizontal")) {
	//			mRigidBody.AddTorque(Vector3.back * Input.GetAxis("Horizontal")*10);
	//		}
	//		if (Input.GetButton ("Vertical")) {
	//			mRigidBody.AddTorque(Vector3.right * Input.GetAxis("Vertical")*10);
	//		}
	//		if (Input.GetButtonDown("Jump")) {
	//			if(mAudioSource != null && JumpSound != null){
	//				mAudioSource.PlayOneShot(JumpSound);
	//			}
	//			mRigidBody.AddForce(Vector3.up*200);
	//		}
	//	}
	//}

	void OnCollisionEnter(Collision coll){
		if (coll.gameObject.tag.Equals ("Floor")) {
			if (mAudioSource != null && HitSound != null && coll.relativeVelocity.y > .5f) {
				mAudioSource.PlayOneShot (HitSound, coll.relativeVelocity.magnitude);
			}
		} else {
			if (mAudioSource != null && HitSound != null && coll.relativeVelocity.magnitude > 2f) {
				mAudioSource.PlayOneShot (HitSound, coll.relativeVelocity.magnitude);
			}
		}
	}

	void OnTriggerEnter(Collider other) {
		if (other.gameObject.tag.Equals ("Coin")) {
			if(mAudioSource != null && CoinSound != null){
				mAudioSource.PlayOneShot(CoinSound);
			}
			Destroy(other.gameObject);
		}
	}

    Vector2Int FindPosition(char target)
    {
        for (int y = 0; y < MazeMatrix.GetLength(0); y++)
        {
            for (int x = 0; x < MazeMatrix.GetLength(1); x++)
            {
                if (MazeMatrix[y, x] == target)
                {
                    return new Vector2Int(y, x);
                }
            }
        }
        return new Vector2Int(-1, -1); 
    }
    List<Vector2Int> FindShortestPath(Vector2Int start, Vector2Int goal)
    {
        Vector2Int[] directions = new Vector2Int[] {
            new Vector2Int(1, 0),  // right 
            new Vector2Int(0, 1),  // down 
            new Vector2Int(-1, 0), // left 
            new Vector2Int(0, -1)  // up 
        };

        PriorityQueue<Vector2Int> openList = new PriorityQueue<Vector2Int>();
        HashSet<Vector2Int> closedList = new HashSet<Vector2Int>();

        Dictionary<Vector2Int, float> gCost = new Dictionary<Vector2Int, float>();
        Dictionary<Vector2Int, float> fCost = new Dictionary<Vector2Int, float>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        // Add the start node to the open list
        openList.Enqueue(start, 0f);
        gCost[start] = 0;
        fCost[start] = Heuristic(start, goal);

        while (openList.Count > 0)
        {
            //StringBuilder openListContents = new StringBuilder("Open list contents: ");
            //foreach (var item in openList)
            //{
            //    openListContents.Append($"[Cost: {fCost[item]}, Position: {item}] ");
            //}
            //Debug.Log(openListContents.ToString());

            Vector2Int current = openList.Dequeue();

            if (current == goal)
            {
                List<Vector2Int> path = ReconstructPath(cameFrom, current);
                path.Reverse(); 
                return path;
            }

            closedList.Add(current);

            foreach (Vector2Int direction in directions)
            {
                Vector2Int neighborValid = current + direction;
                Vector2Int neighbor = current + 2 * direction;

                if (IsValidMove(neighborValid, current) && !closedList.Contains(neighbor))
                {
                    float tentativeGCost = gCost[current] + 1;

                    if (!openList.Contains(neighbor) || tentativeGCost < gCost[neighbor])
                    {
                        gCost[neighbor] = tentativeGCost;
                        fCost[neighbor] = tentativeGCost + Heuristic(neighbor, goal);
                        cameFrom[neighbor] = current;

                        // Add the neighbor to the open list
                        if (!openList.Contains(neighbor))
                        {
                            openList.Enqueue(neighbor, fCost[neighbor]);
                        }
                        else
                        {
                            openList.UpdatePriority(neighbor, fCost[neighbor]);
                        }
                    }
                }
            }
        }

        return null; // No path found
    }

    // Heuristic: Manhattan distance
    float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    // Reconstruct the path from the cameFrom dictionary
    List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        while (cameFrom.ContainsKey(current))
        {
            path.Add(current);
            current = cameFrom[current];
        }
        return path;
    }

    // Check if a move is valid (within bounds, no walls between steps)
    bool IsValidMove(Vector2Int position, Vector2Int previous)
    {
        if (position.x < 0 || position.y < 0 || position.x >= MazeMatrix.GetLength(1) || position.y >= MazeMatrix.GetLength(0))
            return false;

        char tile = MazeMatrix[position.x, position.y];
        if (tile == 'X')
            return false;

        return true;
    }

    List<string> GetDirections(List<Vector2Int> path)
    {
        List<string> directions = new List<string>();

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector2Int current = path[i];
            Vector2Int next = path[i + 1];

            if (next.y == current.y + 2)
            {
                directions.Add("right"); 
            }
            else if (next.y == current.y - 2)
            {
                directions.Add("left"); 
            }
            else if (next.x == current.x + 2)
            {
                directions.Add("down"); 
            }
            else if (next.x == current.x - 2)
            {
                directions.Add("up");
            }
        }

        return directions;
    }
}

public class PriorityQueue<T> : IEnumerable<T>
{
    private List<(T Element, float Priority)> _elements = new List<(T, float)>();

    public int Count => _elements.Count;

    public void Enqueue(T element, float priority)
    {
        _elements.Add((element, priority));
        _elements.Sort((x, y) => x.Priority.CompareTo(y.Priority));
    }

    public T Dequeue()
    {
        if (_elements.Count == 0)
            throw new InvalidOperationException("Priority queue is empty.");

        var element = _elements[0].Element;
        _elements.RemoveAt(0);
        return element;
    }

    public bool Contains(T element)
    {
        return _elements.Exists(x => x.Element.Equals(element));
    }

    public void UpdatePriority(T element, float priority)
    {
        for (int i = 0; i < _elements.Count; i++)
        {
            if (_elements[i].Element.Equals(element))
            {
                _elements[i] = (element, priority);
                _elements.Sort((x, y) => x.Priority.CompareTo(y.Priority)); 
            }
        }
    }
    public IEnumerator<T> GetEnumerator()
    {
        foreach (var item in _elements)
        {
            yield return item.Element;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
