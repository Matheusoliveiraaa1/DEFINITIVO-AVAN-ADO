using UnityEngine;
using TMPro;
using System.Text;

public class MobileDebugConsole : MonoBehaviour
{
    public static MobileDebugConsole Instance;

    public TextMeshProUGUI text;
    public int maxLines = 25;

    private StringBuilder buffer = new StringBuilder();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Clear();
        Log("🟢 Mobile Debug iniciado");
    }

    public void Log(string msg)
    {
        buffer.AppendLine(msg);

        // limita quantidade de linhas
        var lines = buffer.ToString().Split('\n');
        if (lines.Length > maxLines)
        {
            buffer.Clear();
            for (int i = lines.Length - maxLines; i < lines.Length; i++)
                buffer.AppendLine(lines[i]);
        }

        text.text = buffer.ToString();
    }

    public void Clear()
    {
        buffer.Clear();
        text.text = "";
    }
}
