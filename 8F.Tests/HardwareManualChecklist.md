# Hardware-in-the-Loop Manual Checklist — 8F ECT Inspection System

This checklist must be executed prior to any formal release, and whenever changes are made to hardware communication (`DeviceCOM.cs` / `DeviceCommunicationService.cs`), Auto Ellipse calibration (`AutoEllipseViewModel.cs`), or configuration persistence (`ConfigurationImporter.cs` / `InspectionLogRepository.cs`).

---

## Pre-Release Verification Checklist

- [ ] **1. Instrument Connectivity**
  - Launch application on test station with physical ECT instrument connected via Serial COM port or TCP/IP socket.
  - Verify COM/TCP connection status indicator turns green (`Connected`).

- [ ] **2. Multi-Channel Balance**
  - Click `Balance` on each active channel (Channel 1 through 4).
  - Confirm instrument returns FC 22 status response and `IsBalacenced` flag sets to `true` for all active channels.

- [ ] **3. Live Inspection Gating**
  - Run a live part inspection.
  - Confirm OK / Not OK threshold gating renders correctly on live canvas with correct color indicators (Green = Pass, Red = Defect).

- [ ] **4. Auto Ellipse Calibration Flow**
  - Open `Auto Ellipse` calibration modal.
  - Run `Balance` -> Run `Test` -> Select minimum 3 captured test runs -> Click `Make Ellipse`.
  - Confirm calculated threshold ellipse parameters (`CenterX`, `CenterY`, `Width`, `Height`, `RotationAngle`) are applied to the live channel configuration.

- [ ] **5. Configuration Profile Persistence**
  - Save current configuration profile to file and to database.
  - Reload configuration from file and via `Open from Database`.
  - Confirm all frequencies (D1–D8), gains, phases, threshold ellipses, and overlay parameters match exactly.

- [ ] **6. Configuration Export**
  - Export configuration to JSON/file.
  - Open output file and confirm JSON payload structure is valid and complete.

- [ ] **7. PDF Batch Report Generation**
  - Generate a PDF batch inspection report.
  - Open PDF report and confirm header, operator, batch details, statistics, and table data render cleanly.

- [ ] **8. Security Password Gate**
  - Trigger password-protected configuration change action.
  - Confirm access is blocked when incorrect password is provided, and granted only when correct password (`best@123`) is entered.

- [ ] **9. Config-Toggle Menu Options**
  - Toggle feature flags in `AppSettings` (`IsAutoEllipseEnable`, `isOpenDbEbable`, `IsTotalCountVisible`, `IsNotOkCountVisible`).
  - Confirm menu items are hidden when flag is `false` and visible when `true`.
