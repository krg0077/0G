using System.Collections;
using UnityEngine;

namespace _0G
{
    public interface IMove
    {
        IEnumerator MoveTo(Vector3 targetPosition);
    }
}