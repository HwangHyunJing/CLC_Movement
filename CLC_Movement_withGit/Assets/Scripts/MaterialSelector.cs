using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// detection zone의 작동을 알아보기 위한 표지 클래스(색 바꾸는 거)
public class MaterialSelector : MonoBehaviour
{
    [SerializeField]
    Material[] materials = default;

    [SerializeField]
    MeshRenderer meshRenderer = default;

    public void Select(int index)
    {
        // mesh Renderer와 material이 있는지 확인하고
        // 인자로 받은 index 숫자가 유효한 범위의 인덱스인 경우
        if(meshRenderer && materials != null && index >= 0 && index < materials.Length)
        {
            meshRenderer.material = materials[index];
        }
    }
}
