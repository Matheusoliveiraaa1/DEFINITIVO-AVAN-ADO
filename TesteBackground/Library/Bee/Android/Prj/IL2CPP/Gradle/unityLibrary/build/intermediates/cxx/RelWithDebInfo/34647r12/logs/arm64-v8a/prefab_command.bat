@echo off
"C:\\Program Files\\Unity\\Hub\\Editor\\6000.3.14f1\\Editor\\Data\\PlaybackEngines\\AndroidPlayer\\OpenJDK\\bin\\java" ^
  --class-path ^
  "C:\\Users\\mathe\\.gradle\\caches\\modules-2\\files-2.1\\com.google.prefab\\cli\\2.1.0\\aa32fec809c44fa531f01dcfb739b5b3304d3050\\cli-2.1.0-all.jar" ^
  com.google.prefab.cli.AppKt ^
  --build-system ^
  cmake ^
  --platform ^
  android ^
  --abi ^
  arm64-v8a ^
  --os-version ^
  24 ^
  --stl ^
  c++_shared ^
  --ndk-version ^
  27 ^
  --output ^
  "C:\\Users\\mathe\\AppData\\Local\\Temp\\agp-prefab-staging17308793576996080770\\staged-cli-output" ^
  "C:\\Users\\mathe\\.gradle\\caches\\8.13\\transforms\\bbd31d9de896f3b2d29af762e8b4b63c\\transformed\\jetified-games-activity-4.4.0\\prefab" ^
  "C:\\Users\\mathe\\.gradle\\caches\\8.13\\transforms\\6553fed145da0193876e5b502bd35441\\transformed\\jetified-games-frame-pacing-2.1.2\\prefab"
