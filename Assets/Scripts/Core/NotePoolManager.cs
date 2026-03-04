using UnityEngine;
using System.Collections.Generic;

namespace RhythmGame.Core
{
    /// <summary>
    /// Object pool with one prefab per lane.
    /// Lane 0=Up(W), 1=Left(A), 2=Down(S), 3=Right(D).
    /// Each lane's notes use their matching colored prefab.
    /// </summary>
    public class NotePoolManager : MonoBehaviour
    {
        public static NotePoolManager Instance { get; private set; }

        [Header("One prefab per lane: 0=Up, 1=Left, 2=Down, 3=Right")]
        public GameObject[] notePrefabs = new GameObject[4];

        [Header("Pool Settings")]
        public int initialPoolSizePerLane = 20;

        private Queue<NoteObject>[] pools;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            pools = new Queue<NoteObject>[4];
            for (int lane = 0; lane < 4; lane++)
            {
                pools[lane] = new Queue<NoteObject>();
                for (int i = 0; i < initialPoolSizePerLane; i++)
                    CreateNote(lane);
            }
        }

        NoteObject CreateNote(int lane)
        {
            if (notePrefabs[lane] == null)
            {
                Debug.LogError($"[NotePool] No prefab assigned for lane {lane}.");
                return null;
            }
            GameObject go = Instantiate(notePrefabs[lane], transform);
            go.SetActive(false);
            NoteObject note = go.GetComponent<NoteObject>();
            pools[lane].Enqueue(note);
            return note;
        }

        public NoteObject GetNote(int lane)
        {
            if (lane < 0 || lane >= 4) return null;

            if (pools[lane].Count == 0)
                CreateNote(lane);

            NoteObject note = pools[lane].Dequeue();
            note.gameObject.SetActive(true);
            return note;
        }


        public void ReturnNote(NoteObject note, int lane)
        {
            if (lane < 0 || lane >= 4) return;
            note.gameObject.SetActive(false);
            pools[lane].Enqueue(note);
        }
    }
}
