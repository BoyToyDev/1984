productivity_tracker_directory:
  file.directory:
    - name: "C:\\Users\\{{ grains.get('username', 'User') }}\\AppData\\Local\\Programs\\1984"
    - makedirs: True

productivity_tracker_exe:
  file.managed:
    - name: "C:\\Users\\{{ grains.get('username', 'User') }}\\AppData\\Local\\Programs\\1984\\1984.exe"
    - source: salt://1984/1984.exe
    - require:
      - file: productivity_tracker_directory

productivity_tracker_config:
  file.managed:
    - name: "C:\\Users\\{{ grains.get('username', 'User') }}\\AppData\\Local\\Programs\\1984\\1984.config.json"
    - source: salt://1984/1984.config.json
    - require:
      - file: productivity_tracker_directory

productivity_tracker_autostart:
  reg.present:
    - name: "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Run"
    - vname: "1984"
    - vdata: "\"C:\\Users\\{{ grains.get('username', 'User') }}\\AppData\\Local\\Programs\\1984\\1984.exe\""
    - vtype: REG_SZ
    - require:
      - file: productivity_tracker_exe
      - file: productivity_tracker_config
