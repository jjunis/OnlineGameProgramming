using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;
using UnityEngine.SceneManagement;

public class UserLogin : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [SerializeField] InputField NickNameInput;
    [SerializeField] Text checkText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        database = FirebaseDatabase.GetInstance(
            "https://onlinegameprogramming-7f17f-default-rtdb.asia-southeast1.firebasedatabase.app/"
        );

        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();
    }

    public void OnClickLogin()
    {
        string nickName = NickNameInput.text.Trim();

        if (string.IsNullOrEmpty(nickName))
        {
            checkText.text = "닉네임을 입력하세요.";
            return;
        }

        reference.Child("UserInfo").OrderByChild("NickName").EqualTo(nickName).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    if (checkText != null)
                        checkText.text = "Firebase 읽기 오류";
                });
            }

            DataSnapshot snapshot = task.Result;

            if (!snapshot.HasChildren)
            {
                dispatcher.Enqueue(() =>
                {
                    checkText.text = "존재 하지 않는 닉네임 입니다. ";
                });
                return;
            }

            foreach (DataSnapshot userSnapShot in snapshot.Children)
            {
                string userKey = userSnapShot.Key;

                dispatcher.Enqueue(() =>
                {
                    PlayerPrefs.SetString("UserKey", userKey);
                    PlayerPrefs.SetString("UserNickName", nickName);
                    PlayerPrefs.Save();

                    GameManager.Instance.SetUserData(userKey, nickName);

                    if (checkText != null)
                        checkText.text = "로그인 성공";

                    //Scene 이동처리
                    SceneManager.LoadScene("InventoryScene");
                });

                break;
            }
        });
    }
    // Update is called once per frame
    void Update()
    {

    }
}