# Manual test matrix

Run this matrix on a Windows 11 endpoint. The standard offline engine must work without a Microsoft account, app registration, administrator consent, or a translation subscription.

## Pre-flight

- [ ] Debug and Release x64 builds complete with zero errors.
- [ ] First launch shows welcome, primary-language, and engine-selection screens.
- [ ] The welcome screen offers Español, English, and Português (Brasil) as setup languages.
- [ ] Changing the setup language immediately localizes all three onboarding screens without changing the selected primary translation language.
- [ ] The setup-language selection remains after restarting the application.
- [ ] The engine screen shows detected RAM, logical CPU cores, free disk, and a compatibility result.
- [ ] Settings can switch between Offline standard, TranslateGemma 4B, TranslateGemma 12B, TranslateGemma 27B, and optional Microsoft 365 Copilot.
- [ ] Closing Settings leaves the tray icon and hotkeys active.
- [ ] Exiting from the tray removes both global hotkeys and the tray icon.
- [ ] A deliberate hotkey conflict produces a friendly notification.
- [ ] Light, dark, and system themes remain readable.
- [ ] Overlay stays inside each monitor's working area at multiple DPI scales.

## Engine preparation

### Offline standard

- [ ] Initial setup downloads the Firefox/Bergamot model pack and displays progress.
- [ ] A completely fresh `%LOCALAPPDATA%\Bridge` installation creates the nested model directory before touching marker files.
- [ ] After the pack is ready, disconnect the network and restart the app.
- [ ] English, Spanish, and Brazilian Portuguese translations continue to work offline.
- [ ] Spanish ↔ Brazilian Portuguese translation works through the local English pivot.

### TranslateGemma 4B, 12B, and 27B

- [ ] Without Ollama installed, the app explains the prerequisite and offers the official download link.
- [ ] The Gemma terms must be accepted before the model download starts.
- [ ] Model download progress is displayed and the selected model is reported as ready afterward.
- [ ] Translation works with the network disconnected while the local Ollama service is running.
- [ ] The 12B option warns on a PC below 24 GB RAM, 8 logical cores, or 12 GB free disk.
- [ ] The 27B option warns on a PC below 32 GB RAM, 12 logical cores, or 24 GB free disk.

### Microsoft 365 Copilot (optional)

- [ ] Copilot setup is shown only when that engine is selected.
- [ ] Missing Client ID produces a configuration message without affecting offline engines.
- [ ] WAM sign-in uses a work/school account and shows the connected username.
- [ ] Missing Copilot license or admin consent produces a friendly error without raw Graph JSON.

## Application matrix

Repeat the complete flow for each target with the standard offline engine, then smoke-test any other installed engine:

| Target | Capture | Overlay | Copy | Reverse language | Replace | Never sends |
|---|---:|---:|---:|---:|---:|---:|
| Notepad | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| Chrome text field | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| Microsoft Edge text field | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| ServiceNow in Chrome/Edge | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| Microsoft Teams desktop | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| Microsoft Teams web | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| WhatsApp Web | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| Outlook desktop/web | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |

For each application:

1. Select text.
2. Press `Ctrl + Shift + T`.
3. Confirm the selected text was captured and the overlay identifies the active engine.
4. Confirm the detected and target languages.
5. Copy the translation.
6. Write a response in the primary language and select it.
7. Press `Ctrl + Shift + Enter`.
8. Confirm the target is English before any manual choice, or the last target language chosen in the overlay.
9. Choose Replace.
10. Confirm only the selection changed and no message was sent.

After choosing a target language in the overlay, repeat both shortcuts and confirm they use that language. Restart Bridge and confirm the same language is still selected for both shortcuts. Choose another target language and confirm the shared preference updates.

Also hold `Ctrl + Shift` briefly after pressing each hotkey and confirm capture begins only after the keys are released. Repeat both translation actions from the tray menu and confirm focus returns to the original application before Copy is sent.

### Automatic selection while the overlay is open

- [ ] Open the overlay with `Ctrl + Shift + T`, then drag-select different text in the source app. Confirm the overlay updates without pressing the shortcut again and keeps its target language.
- [ ] Open the overlay with `Ctrl + Shift + Enter` and repeat with a double-click, Shift + arrow keys, and Ctrl + A. Confirm each completed selection updates the result in response mode.
- [ ] A normal click or typing with Shift does not start a translation.
- [ ] The source app keeps focus after an automatic translation; Replace changes only the latest selection.
- [ ] Close the overlay, select more text, and confirm no automatic translation starts.
## Acceptance scenario

Primary language: Spanish.

1. Select `Meu computador não está sincronizando a senha depois da alteração.`
2. Press `Ctrl + Shift + T`.
3. Expect Portuguese → Spanish and a translation equivalent to `Mi computadora no está sincronizando la contraseña después del cambio.`
4. Select `Reiniciá la computadora y probá nuevamente conectado a la VPN.`
5. Press `Ctrl + Shift + Enter` and confirm English is selected initially.
6. Choose Brazilian Portuguese in the overlay and expect Spanish → Portuguese with a translation equivalent to `Reinicie o computador e tente novamente conectado à VPN.`
7. Choose Replace and confirm the Spanish selection becomes Portuguese without sending.
8. Select another Spanish response and press `Ctrl + Shift + Enter`; confirm Brazilian Portuguese is used immediately.
9. Restart Bridge, press `Ctrl + Shift + T` on another selection, and confirm Brazilian Portuguese remains the selected target.

## Technical terminology

Translate:

```text
Please open Company Portal, sync the device and then run gpupdate /force. If the problem continues, send me the output of dsregcmd /status.
```

Confirm `Company Portal`, `gpupdate /force`, and `dsregcmd /status` are preserved exactly.

## Failure, privacy, and security cases

- [ ] No selection → `Select some text and try again.`
- [ ] More than 8,000 characters → bounded-input warning and no provider call.
- [ ] First model download interrupted → friendly error and a successful retry.
- [ ] Corrupted Bergamot model download → hash validation rejects it.
- [ ] Ollama stopped after setup → friendly local-service message.
- [ ] No Copilot Client ID → configuration message and no authentication attempt.
- [ ] Copilot network disconnected → reachability message.
- [ ] Copilot `429` with `Retry-After` → bounded retry, then friendly busy message.
- [ ] Escape closes the overlay.
- [ ] Rapid repeated hotkeys produce only one active request.
- [ ] Existing text clipboard content is restored after capture and Replace.
- [ ] Rich clipboard content is restored where OLE supports it.
- [ ] Replace failure leaves the translation on the clipboard.
- [ ] Diagnostics contain no selected text, translated text, tokens, or provider bodies.
- [ ] Elevated target application is rejected safely rather than requesting app elevation.
