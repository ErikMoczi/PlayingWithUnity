using System;
using UnityEngine;

public class Clock : MonoBehaviour
{
    private const float DegreesPerHour = 30f;
    private const float DegreesPerMinute = 6f;
    private const float DegreesPerSecond = 6f;

    public bool Continuous;
    public Transform HoursTransform;
    public Transform MinutesTransform;
    public Transform SecondsTransform;

    private void Update()
    {
        if (Continuous)
            UpdateContinuous();
        else
            UpdateDiscrete();
    }

    private void UpdateContinuous()
    {
        var dateTime = DateTime.Now.TimeOfDay;
        HoursTransform.localRotation = Quaternion.Euler(0f, (float) dateTime.TotalHours * DegreesPerHour, 0f);
        MinutesTransform.localRotation = Quaternion.Euler(0f, (float) dateTime.TotalMinutes * DegreesPerMinute, 0f);
        SecondsTransform.localRotation = Quaternion.Euler(0f, (float) dateTime.TotalSeconds * DegreesPerSecond, 0f);
    }


    private void UpdateDiscrete()
    {
        var dateTime = DateTime.Now;
        HoursTransform.localRotation = Quaternion.Euler(0f, dateTime.Hour * DegreesPerHour, 0f);
        MinutesTransform.localRotation = Quaternion.Euler(0f, dateTime.Minute * DegreesPerMinute, 0f);
        SecondsTransform.localRotation = Quaternion.Euler(0f, dateTime.Second * DegreesPerSecond, 0f);
    }
}