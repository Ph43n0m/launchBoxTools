del build.log /F /Q

cd obj

rmdir Debug /s /q
rmdir Release /s /q

cd ..

cd bin
rmdir Debug /s /q
rmdir Release /s /q