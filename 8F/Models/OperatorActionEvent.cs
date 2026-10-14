using System;

namespace _8F.Models
{
    /// <summary>
    /// Model representing an operator action event (click, key press, or typed word) inside 8F windows.
    /// </summary>
    public class OperatorActionEvent
    {
        public long Id { get; set; }
        public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;
        public string ActionType { get; set; } = string.Empty; // 'Click', 'KeyDown', 'WordTyped'
        public string? MouseButton { get; set; }               // 'Left', 'Right', 'Middle', or null
        public string? ControlName { get; set; }               // Clicked control's name/text
        public string? KeyName { get; set; }                   // e.g. 'F5', 'Enter', 'B', or typed word
        public string? WindowTitle { get; set; }               // Active 8F window title
        public string? OperatorName { get; set; }              // From DeviceCOM.part?.CheckedBy
        public Guid SessionId { get; set; }                    // Unique ID per app run session
        public int? ScreenX { get; set; }                      // Click X coordinate
        public int? ScreenY { get; set; }                      // Click Y coordinate
    }
}
