SonarScanner.MSBuild.exe begin /k:"MonoMazer" /d:sonar.host.url="http://localhost:9000" /d:sonar.login="f64d856942fad2c9f6e46897cb07687f35ccbc08"

MsBuild.exe /t:Rebuild


SonarScanner.MSBuild.exe end /d:sonar.login="f64d856942fad2c9f6e46897cb07687f35ccbc08"
