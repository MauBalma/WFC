using System.Collections;
using System.Collections.Generic;
using Balma.WFC;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public Button buttonGenerate;
    public TMP_Text textGenerate;
    public TMP_Text textGenerationTime;
    public WFCGrid wfcGrid;
    
    // Start is called before the first frame update
    void Start()
    {
        buttonGenerate.onClick.AddListener(wfcGrid.Generate);
        wfcGrid.OnStartGeneration += WfcGridOnOnStartGeneration;
        wfcGrid.OnEndGeneration += WfcGridOnOnEndGeneration;
        
        textGenerate.text = "GENERATE";
        textGenerationTime.text = $"";
    }

    private void WfcGridOnOnStartGeneration()
    {
        buttonGenerate.enabled = false;
        textGenerate.text = "WAITING";
        textGenerationTime.text = "";
    }
    
    private void WfcGridOnOnEndGeneration()
    {
        buttonGenerate.enabled = true;
        textGenerate.text = "GENERATE";
        textGenerationTime.text = $"Took: {wfcGrid.generationTime * 1000:0} ms";
    }
}
