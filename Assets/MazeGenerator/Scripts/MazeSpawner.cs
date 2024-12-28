using UnityEngine;
using System.Collections;
using System;

//<summary>
//Game object, that creates maze and instantiates it in scene
//</summary>
public class MazeSpawner : MonoBehaviour {
	public enum MazeGenerationAlgorithm{
		PureRecursive,
		RecursiveTree,
		RandomTree,
		OldestTree,
		RecursiveDivision,
	}

	public MazeGenerationAlgorithm Algorithm = MazeGenerationAlgorithm.PureRecursive;
	public bool FullRandom = false;
	public int RandomSeed = 12345;
	public GameObject Floor = null;
	public GameObject Wall = null;
	public GameObject Pillar = null;
	public int Rows = 1;
	public int Columns = 1;
	public float CellWidth = 5;
	public float CellHeight = 5;
	public bool AddGaps = true;
	public GameObject GoalPrefab = null;

	private BasicMazeGenerator mMazeGenerator = null;
	private int GoalCount = 0;
    private Vector3 GoalLocation = Vector3.zero;
    public char[,] MazeMatrix { get; private set; }
    void Start () {
		if (!FullRandom) {
            UnityEngine.Random.InitState(RandomSeed);
        }
		switch (Algorithm) {
		case MazeGenerationAlgorithm.PureRecursive:
			mMazeGenerator = new RecursiveMazeGenerator (Rows, Columns);
			break;
		case MazeGenerationAlgorithm.RecursiveTree:
			mMazeGenerator = new RecursiveTreeMazeGenerator (Rows, Columns);
			break;
		case MazeGenerationAlgorithm.RandomTree:
			mMazeGenerator = new RandomTreeMazeGenerator (Rows, Columns);
			break;
		case MazeGenerationAlgorithm.OldestTree:
			mMazeGenerator = new OldestTreeMazeGenerator (Rows, Columns);
			break;
		case MazeGenerationAlgorithm.RecursiveDivision:
			mMazeGenerator = new DivisionMazeGenerator (Rows, Columns);
			break;
		}
		mMazeGenerator.GenerateMaze ();
		MazeMatrix = GenerateEmptyMatrix(Rows);
		for (int row = 0; row < Rows; row++)
		{
			for (int column = 0; column < Columns; column++)
			{
				float x = column * (CellWidth + (AddGaps ? .2f : 0));
				float z = row * (CellHeight + (AddGaps ? .2f : 0));
				MazeCell cell = mMazeGenerator.GetMazeCell(row, column);
				GameObject tmp;
				tmp = Instantiate(Floor, new Vector3(x, 0, z), Quaternion.Euler(0, 0, 0)) as GameObject;
				tmp.transform.parent = transform;
				if (cell.WallRight)
				{
					tmp = Instantiate(Wall, new Vector3(x + CellWidth / 2, 0, z) + Wall.transform.position, Quaternion.Euler(0, 90, 0)) as GameObject;// right
					tmp.transform.parent = transform;
					MazeMatrix[2 * row + 1, 2 * column + 2] = 'X';
				}
				if (cell.WallFront)
				{
					tmp = Instantiate(Wall, new Vector3(x, 0, z + CellHeight / 2) + Wall.transform.position, Quaternion.Euler(0, 0, 0)) as GameObject;// front
					tmp.transform.parent = transform;
                    MazeMatrix[2 * row + 2, 2 * column + 1] = 'X';
				}
				if (cell.WallLeft)
				{
					tmp = Instantiate(Wall, new Vector3(x - CellWidth / 2, 0, z) + Wall.transform.position, Quaternion.Euler(0, 270, 0)) as GameObject;// left
					tmp.transform.parent = transform;
                    MazeMatrix[2 * row + 1, 2 * column] = 'X';
				}
				if (cell.WallBack)
				{
					tmp = Instantiate(Wall, new Vector3(x, 0, z - CellHeight / 2) + Wall.transform.position, Quaternion.Euler(0, 180, 0)) as GameObject;// back
					tmp.transform.parent = transform;
                    MazeMatrix[2 * row, 2 * column + 1] = 'X';
				}
				if (cell.IsGoal && GoalPrefab != null && GoalCount == 0)
				{
					//GoalCount++;
					GoalLocation = new Vector3(x, 1, z);
				}
			}
		}
		
        if (GoalLocation != Vector3.zero)
        {
            GameObject tmp = Instantiate(GoalPrefab, GoalLocation, Quaternion.Euler(0, 0, 0)) as GameObject;
            tmp.transform.parent = transform;

            float GX = GoalLocation.x;
            float GZ = GoalLocation.z;
            int column = Mathf.FloorToInt(GX / (CellWidth + (AddGaps ? 0.2f : 0)));
            int row = Mathf.FloorToInt(GZ / (CellHeight + (AddGaps ? 0.2f : 0)));
			//Debug.Log($"Row: {row}, Column: {column}");
            MazeMatrix[2 * row + 1, 2 * column + 1] = 'G';
        }

        FlipMatrixHorizontally(MazeMatrix);
        MazeMatrix[2 * (Rows-1) + 1, 1] = 'B';
        //PrintMatrix(MazeMatrix);

        if (Pillar != null){
			for (int row = 0; row < Rows+1; row++) {
				for (int column = 0; column < Columns+1; column++) {
					float x = column*(CellWidth+(AddGaps?.2f:0));
					float z = row*(CellHeight+(AddGaps?.2f:0));
					GameObject tmp = Instantiate(Pillar,new Vector3(x-CellWidth/2,0,z-CellHeight/2),Quaternion.identity) as GameObject;
					tmp.transform.parent = transform;
				}
			}
		}
	}

	public char[,] GenerateEmptyMatrix(int dimension)
	{
		char[,] matrix = new char[2 * dimension + 1, 2 * dimension + 1];
		for (int i = 0; i < 2*dimension+1; i++)
		{
			for (int j = 0; j < 2*dimension+1; j++)
			{
				if (i %  2 == 0)
				{
					if (j % 2 == 0)
					{
						matrix[i,j] = '#';
					} else
					{
						matrix[i, j] = '-';
					}
				} else
				{
                    if (j % 2 == 0)
                    {
                        matrix[i, j] = '-';
                    }
                    else
                    {
                        matrix[i, j] = 'O';
                    }
                }
			}
		}
		return matrix;
	}

    public void PrintMatrix(char[,] Matrix)
    {
        // Get dimensions of the matrix
        int Rows = Matrix.GetLength(0);
        int Columns = Matrix.GetLength(1);

        string matrixString = "Printing the matrix:\n"; 

        for (int i = 0; i < Rows; i++)
        {
            string row = "";
            for (int j = 0; j < Columns; j++)
            {
                row += Matrix[i, j] + " "; 
            }
            matrixString += row + "\n"; 
        }

        Debug.Log(matrixString); 
    }

    public void FlipMatrixHorizontally(char[,] Matrix)
    {
        int Rows = Matrix.GetLength(0);   
        int Columns = Matrix.GetLength(1);

        for (int i = 0; i < Rows / 2; i++)
        {
            // Swap rows i and (Rows - 1 - i)
            for (int j = 0; j < Columns; j++)
            {
                char temp = Matrix[i, j];
                Matrix[i, j] = Matrix[Rows - 1 - i, j];
                Matrix[Rows - 1 - i, j] = temp;
            }
        }
    }
}
