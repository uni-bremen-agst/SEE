Runs Axivion's Architecture Verification on SEE

Please use in the following way:

1. Set JAVA_HOME

    set "JAVA_HOME=C:\Program Files\Java\jdk-21.0.1"

2. open an Axivion command prompt
  - on Linux / macOS: in a shell source bauhaus-kshrc or bauhaus-cshrc
                from the installation directory
  - on Windows: open an "Axivion Command Prompt" from the Start Menu
                or open a standard command prompt window and run absvars.bat
                from the Axivion Installation Directory

3. optionally start a local dashboard server with the following commands.
   This step is only required if you are interested in seeing the Dashboard
   presentation of architecture violations.

  - on Linux / macOS:
      cd dashboard
      ./start_dashboard.sh
  - on Windows:
      cd dashboard
      start_dashboard.sh

   after this, point your browser to http://localhost:9090/axivion to
   see the Axivion Dashboard running on your local machine.

4. cd into the root directory "architecture".
   In this location, you can find the following subdirectories:
   
      rules/      - Python modules that will be executed as part of the Axivion CI
                    or after by yourself

5. from the "architecture" directory
   (please use the command "cd" to switch into the directory), you can:

   - run the analysis with the command:
        start_analysis.bat           (Windows)
        ./start_analysis.sh          (Linux/macOS)
   - inspect the project configuration in the Axivion Project Configuration GUI
        start_analysis config        (Windows)
        ./start_analysis.sh config   (Linux/macOS)

7. after the analysis of one project configuration has been executed,
   you can inspect results on the Dashboard.
   Additionally, you can open the RFG files in Gravis and run the architecture
   analysis manually. The RFG files are automatically placed into the
   the root directory of the SEE project.
