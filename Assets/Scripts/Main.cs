using UnityEngine;
using UnityEngine.UI;

using System.Collections;
using System.Text.RegularExpressions;

// temporary class for testing purposes
// should be removed once jwt-token is received from server
public class JwtPayload
{
    public string uid;
    public string expired_at;
}


public class Main : MonoBehaviour
{
    public ServerMessagesController _controller;
    public Coroutine _checkInputRoutine;

    public Text _input;
    public Text _output;
    public Button _connectBtn;

    public void Start()
    {
        _checkInputRoutine = StartCoroutine(CheckInput());
        _controller.ServerEvent += ServerEventHandler;
    }

    private IEnumerator CheckInput()
    {
        while(true)
        {
            Regex regex = new Regex("^[0-9]{1,50}$");
            Match match = regex.Match(_input.text);
            _connectBtn.enabled = match.Success;
            yield return null;
        }        
    }

    public void ConnectBtnClickHandler()
    {
        StopCoroutine(_checkInputRoutine);

        //For testing
        var jwtPayload = new JwtPayload();
        jwtPayload.uid = _input.text;
        jwtPayload.expired_at = "123";
        var secretKey = "test_secret";
        string token = JWT.JsonWebToken.Encode(jwtPayload, secretKey, JWT.JwtHashAlgorithm.HS256);
       _controller.Connect("localhost", 8000, token);

        _connectBtn.gameObject.SetActive(false);       
    }

    private void ServerEventHandler(object sender, ServerEventArgs e)
    {
        //_output.text += "e.Data.Type: " + e.Data.Type.ToString() + "\n";
        Debug.Log("e.Data.Type: " + e.Data.Type.ToString() + "\n");
    }

    void OnApplicationQuit()
    {
        _controller.Disconnect();
    }
}
