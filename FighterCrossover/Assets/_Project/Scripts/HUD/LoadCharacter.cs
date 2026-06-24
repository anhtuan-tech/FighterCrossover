using UnityEngine;

public class LoadCharacter : MonoBehaviour
{
    // Đây là 2 Object rỗng đóng vai trò định vị (Spawn Point) trên Scene
    public GameObject p1;
    public GameObject p2;

    void Start()
    {
        // 1. Thay thế cho Player 1
        if (p1 != null && !string.IsNullOrEmpty(SelectionData.characterPrefabUrl1))
        {
            // Load bản vẽ từ thư mục Resources
            GameObject p1Prefab = Resources.Load<GameObject>(SelectionData.characterPrefabUrl1);

            if (p1Prefab != null)
            {
                // Khởi tạo nhân vật mới tại đúng vị trí và góc quay của object p1 rỗng
                GameObject newP1 = Instantiate(p1Prefab, p1.transform.position, p1.transform.rotation);

                // (Tùy chọn) Nếu p1 rỗng nằm trong cụm cha nào đó, đưa nhân vật mới vào cụm đó luôn
                if (p1.transform.parent != null) newP1.transform.SetParent(p1.transform.parent);
                newP1.name = "Player1";

                // Xóa Object rỗng cũ đi để tránh rác Scene
                Destroy(p1);

                // Cập nhật lại biến p1 để giữ liên kết với nhân vật mới nếu cần quản lý sau này
                p1 = newP1;
            }
            else
            {
                Debug.LogError($"Không tìm thấy Prefab P1 tại địa chỉ: {SelectionData.characterPrefabUrl1}");
            }
        }

        // 2. Thay thế cho Player 2
        if (p2 != null && !string.IsNullOrEmpty(SelectionData.characterPrefabUrl2))
        {
            GameObject p2Prefab = Resources.Load<GameObject>(SelectionData.characterPrefabUrl2);

            if (p2Prefab != null)
            {
                GameObject newP2 = Instantiate(p2Prefab, p2.transform.position, p2.transform.rotation);
                if (p2.transform.parent != null) newP2.transform.SetParent(p2.transform.parent);
                newP2.name = "Player2";

                Destroy(p2);
                p2 = newP2;
            }
            else
            {
                Debug.LogError($"Không tìm thấy Prefab P2 tại địa chỉ: {SelectionData.characterPrefabUrl2}");
            }
        }
    }
}