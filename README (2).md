# Multimodal Interaction Assignment: Speech Modality

## Counter-Strike: Global Offensive Voice Assistant

### Installation steps

* Clone the repository.

* Place **gamestate_integration_quickstartguide.cfg** and **autoexec.cfg** in your ```steamapps\common\Counter-Strike Global Offensive\csgo\cfg``` folder.

* Make sure you have **Microsoft.Speech** and **mmisharp.dll** added to the Project References in Visual Studio.

* Make sure you have the [CSGSI](https://github.com/rakijah/CSGSI), [InputSimulator](https://www.nuget.org/packages/InputSimulator/) and [Newtonsoft.json](https://www.nuget.org/packages/Newtonsoft.Json/) NuGet packages installed.

* Make sure you have the following *dll's* in your ```AppGui/AppGui/bin/Debug folder```:

  * **Microsoft.Speech.dll**
  * **mmisharp.dll**
  * **CSGSI.dll**
  * **WindowsInput.dll**
  * **Newtonsoft.Json.dll**

* Run **mmiframeworkV2.jar** with ```java -jar mmiframeworkV2.jar```.


### To run

1. Open **AppGUI** and **SpeechModality** Visual Studio projects, and execute each one of them.

2. Run **mmiframeworkV2.jar** with ```java -jar mmiframeworkV2.jar```.

3. Open **Counter-Stike:Global Offensive** in borderless fullscreen ***or*** say: ***"Abre o Counter-Strike"*** to the microphone.
