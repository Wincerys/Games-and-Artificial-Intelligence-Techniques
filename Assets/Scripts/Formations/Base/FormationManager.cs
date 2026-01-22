// ===== FormationManager.cs =====
using UnityEngine;
using System.Collections.Generic;

public class FormationManager : MonoBehaviour
{
    [Header("Formation Settings")]
    public FormationSettings settings;
    
    [Header("Available Formations")]
    public FormationType formationType = FormationType.Random;
    
    private Dictionary<FormationType, BaseFormation> formations;
    
    public enum FormationType
    {
        Random,
        Fortress,
        Wings,
        Spearhead,
        Castle,
        Scattered,
        Wall,
        Diamond,
        Chevron
    }

    private void Awake()
    {
        // Don't initialize formations here - wait for settings to be properly set
    }

    public void Initialize()
    {
        InitializeFormations();
    }

    private void InitializeFormations()
    {
        formations = new Dictionary<FormationType, BaseFormation>
        {
            { FormationType.Fortress, new FortressFormation() },
            { FormationType.Wings, new WingsFormation() },
            { FormationType.Spearhead, new SpearheadFormation() },
            { FormationType.Castle, new CastleFormation() },
            { FormationType.Scattered, new ScatteredFormation() },
            { FormationType.Wall, new WallFormation() },
            { FormationType.Diamond, new DiamondFormation() },
            { FormationType.Chevron, new ChevronFormation() }
        };

        // Initialize all formations with settings
        foreach (var formation in formations.Values)
        {
            formation.Initialize(settings);
        }
    }

    public FormationResult GenerateFormation(Transform parentTransform)
    {
        // Clear existing invaders
        ClearExistingInvaders(parentTransform);
        
        // Choose formation type
        FormationType chosenType = GetFormationType();
        
        Debug.Log($"Generating {chosenType} formation");
        
        if (formations.ContainsKey(chosenType))
        {
            return formations[chosenType].Generate(parentTransform);
        }
        else
        {
            return new FormationResult 
            { 
                success = false, 
                errorMessage = $"Formation type {chosenType} not found!" 
            };
        }
    }

    private FormationType GetFormationType()
    {
        if (formationType == FormationType.Random)
        {
            FormationType[] availableTypes = {
                FormationType.Fortress,
                FormationType.Wings,
                FormationType.Spearhead,
                FormationType.Castle,
                FormationType.Scattered,
                FormationType.Wall,
                FormationType.Diamond,
                FormationType.Chevron
            };
            
            return availableTypes[Random.Range(0, availableTypes.Length)];
        }
        else
        {
            return formationType;
        }
    }

    private void ClearExistingInvaders(Transform parentTransform)
    {
        for (int c = parentTransform.childCount - 1; c >= 0; c--)
        {
            Destroy(parentTransform.GetChild(c).gameObject);
        }
    }

    public void UpdateSettings(FormationSettings newSettings)
    {
        settings = newSettings;
        foreach (var formation in formations.Values)
        {
            formation.Initialize(settings);
        }
    }
}