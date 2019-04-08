Follow these steps when creating a new setup project.

1. Make sure you have the latest WiX toolset installed. http://wixtoolset.org/
2. Add your main output project to the setup references.
3. In the project properties:
   a: Change the output name under the Installer tab.
   b: Open the pre-build event and change the project name to the referenced project name.
4. Open the Common.wxi file and modify the values as needed.
5. If EventLog is not needed, open Features.wxs and remove "EventLogSetup".
6. Build the project to see if it works.

Versionnumber is placed in Common.wxi.

When adding new libraries (3rd party) to a project make sure to also add them to the main project output.
If you don't, then the auto generation will not find them and therefore not include them.
Also, remember to set copy local to true.